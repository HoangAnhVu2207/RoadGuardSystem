using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyAssignmentReassignmentResult(
    Guid PreviousAssignmentId,
    Guid CurrentAssignmentId,
    Guid OutboxMessageId,
    IdempotencyOperationStatus Status);

/// <summary>
/// Persists an already-authorized reassignment and its durable audit/outbox records in one transaction.
/// Lifecycle authorization and SurveyRequest state transitions remain owned by P1-23.
/// </summary>
public sealed class SurveyAssignmentPersistenceService : ISurveyAssignmentRepository
{
    private const string ReassignedEventType = "survey_request.reassigned";
    private const string ReassignmentOperation = "SurveyRequestReassigned";
    private static readonly string[] AuditSnapshotProperties = ["operatorUserId"];
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public SurveyAssignmentPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public Task<SurveyAssignmentReassignmentFacts?> GetReassignmentFactsAsync(
        Guid surveyRequestId,
        CancellationToken cancellationToken = default)
    {
        return (
                from request in _context.SurveyRequests.AsNoTracking()
                where request.Id == surveyRequestId
                join assignment in _context.SurveyAssignments.AsNoTracking()
                    on request.Id equals assignment.SurveyRequestId
                where assignment.EndedAt == null
                select new SurveyAssignmentReassignmentFacts(
                    request.ProjectId,
                    assignment.Id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SurveyAssignmentReassignmentResult> ReassignAsync(
        Guid surveyRequestId,
        Guid projectId,
        Guid expectedPreviousAssignmentId,
        SurveyAssignment replacementAssignment,
        Guid actorUserId,
        DateTimeOffset occurredAt,
        string reassignmentReason,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        var now = occurredAt.ToUniversalTime();
        var operation = await _idempotency.ExecuteAsync(
            actorUserId,
            projectId,
            ReassignmentOperation,
            idempotencyKey,
            requestFingerprint,
            async operationCancellationToken =>
            {
                var previousAssignment = await _context.SurveyAssignments
                    .SingleOrDefaultAsync(
                        assignment => assignment.SurveyRequestId == surveyRequestId && assignment.EndedAt == null,
                        operationCancellationToken)
                    ?? throw new InvalidOperationException("The active survey assignment changed before reassignment could be persisted.");
                if (previousAssignment.Id != expectedPreviousAssignmentId)
                {
                    throw new InvalidOperationException("The active survey assignment changed before reassignment could be persisted.");
                }

                var outboxMessageId = Guid.NewGuid();
                previousAssignment.EndForReassignment(now, reassignmentReason);
                _context.SurveyAssignments.Add(replacementAssignment);
                _context.AuditLogs.Add(AuditLog.Create(
                    Guid.NewGuid(),
                    actorUserId,
                    now,
                    ReassignedEventType,
                    "SurveyRequest",
                    surveyRequestId,
                    JsonSerializer.Serialize(new { operatorUserId = previousAssignment.OperatorUserId }),
                    JsonSerializer.Serialize(new { operatorUserId = replacementAssignment.OperatorUserId }),
                    reason: null,
                    source: "p2-23.persistence",
                    correlationId: surveyRequestId,
                    snapshotAllowedPropertyNames: AuditSnapshotProperties));
                _context.OutboxMessages.Add(OutboxMessage.Create(
                    outboxMessageId,
                    ReassignedEventType,
                    now,
                    surveyRequestId,
                    JsonSerializer.Serialize(new
                    {
                        surveyRequestId,
                        previousAssignmentId = previousAssignment.Id,
                        currentAssignmentId = replacementAssignment.Id
                    })));
                return (replacementAssignment.Id, JsonSerializer.Serialize(new ReassignmentOutcome(
                    previousAssignment.Id,
                    replacementAssignment.Id,
                    outboxMessageId)));
            },
            cancellationToken);
        var outcome = JsonSerializer.Deserialize<ReassignmentOutcome>(operation.OutcomeJson)
            ?? throw new InvalidOperationException("Reassignment idempotency outcome was invalid.");
        return new SurveyAssignmentReassignmentResult(
            outcome.PreviousAssignmentId,
            outcome.CurrentAssignmentId,
            outcome.OutboxMessageId,
            operation.Status);
    }

    private sealed record ReassignmentOutcome(
        Guid PreviousAssignmentId,
        Guid CurrentAssignmentId,
        Guid OutboxMessageId);
}
