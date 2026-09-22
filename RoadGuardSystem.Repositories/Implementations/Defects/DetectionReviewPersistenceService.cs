using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Interfaces.Defects;

namespace RoadGuardSystem.Repositories.Defects;

public sealed class DetectionReviewPersistenceService : IDetectionReviewRepository
{
    private const string Operation = "DefectReviewPersisted";
    private const string EventType = "defect.reviewed";
    private const string AuditSource = "p2-32.persistence";
    private static readonly string[] AuditProperties =
        ["detection_id", "defect_id", "field_inspection_task_id", "verification_log_id"];
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public DetectionReviewPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public async Task<DetectionReviewPersistenceResult> PersistAsync(
        DetectionReviewPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            ValidateRequest(request);
            var operation = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                Operation,
                request.IdempotencyKey,
                request.RequestFingerprint,
                operationCancellationToken => PersistAggregateAsync(request, operationCancellationToken),
                cancellationToken);

            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return EmptyResult(DetectionReviewPersistenceStatus.IdempotencyConflict);
            }

            var outcome = JsonSerializer.Deserialize<PersistenceOutcome>(operation.OutcomeJson)
                ?? throw new InvalidOperationException("Detection review idempotency outcome was invalid.");
            return new(
                operation.Status == IdempotencyOperationStatus.Executed
                    ? DetectionReviewPersistenceStatus.Executed
                    : DetectionReviewPersistenceStatus.Replayed,
                outcome.DetectionId,
                outcome.DefectId,
                outcome.FieldInspectionTaskId,
                outcome.VerificationLogId,
                outcome.OutboxMessageId);
        }
        catch (ArgumentException)
        {
            return EmptyResult(DetectionReviewPersistenceStatus.InvalidInput);
        }
        catch (DbUpdateException exception) when (HasSqlError(exception, 2601) || HasSqlError(exception, 2627))
        {
            return EmptyResult(DetectionReviewPersistenceStatus.DuplicateRetainedDetection);
        }
        catch (DbUpdateException exception) when (HasSqlError(exception, 547))
        {
            return EmptyResult(DetectionReviewPersistenceStatus.InvalidReference);
        }
    }

    private async Task<(Guid OperationId, string OutcomeJson)> PersistAggregateAsync(
        DetectionReviewPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var outboxMessageId = Guid.NewGuid();
        var outcome = new PersistenceOutcome(
            request.Detection.Id,
            request.Defect.Id,
            request.FieldInspectionTask.Id,
            request.VerificationLog.Id,
            outboxMessageId);

        _context.AIDetections.Add(request.Detection);
        _context.Defects.Add(request.Defect);
        _context.FieldInspectionTasks.Add(request.FieldInspectionTask);
        _context.DefectVerificationLogs.Add(request.VerificationLog);
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(),
            request.ActorUserId,
            now,
            EventType,
            "DefectReview",
            request.Defect.Id,
            null,
            JsonSerializer.Serialize(new
            {
                detection_id = request.Detection.Id,
                defect_id = request.Defect.Id,
                field_inspection_task_id = request.FieldInspectionTask.Id,
                verification_log_id = request.VerificationLog.Id
            }),
            request.AuditReason,
            AuditSource,
            request.CorrelationId,
            AuditProperties));
        _context.OutboxMessages.Add(OutboxMessage.Create(
            outboxMessageId,
            EventType,
            now,
            request.CorrelationId,
            request.EventPayloadJson));

        await Task.CompletedTask;
        return (request.Defect.Id, JsonSerializer.Serialize(outcome));
    }

    private static void ValidateRequest(DetectionReviewPersistenceRequest request)
    {
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty || request.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("Actor, project, and correlation ids must not be empty.", nameof(request));
        }

        if (request.Detection is null || request.Defect is null || request.FieldInspectionTask is null || request.VerificationLog is null)
        {
            throw new ArgumentException("Detection review persistence requires all aggregate facts.", nameof(request));
        }

        if (request.VerifiedByUserId is Guid verifiedByUserId && verifiedByUserId != request.VerificationLog.VerifiedByUserId)
        {
            throw new ArgumentException("Verification actor does not match the verification log.", nameof(request));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey, nameof(request.IdempotencyKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestFingerprint, nameof(request.RequestFingerprint));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EventPayloadJson, nameof(request.EventPayloadJson));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AuditReason, nameof(request.AuditReason));
    }

    private static bool HasSqlError(DbUpdateException exception, int number)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException sqlException && sqlException.Number == number)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private static DetectionReviewPersistenceResult EmptyResult(DetectionReviewPersistenceStatus status)
        => new(status, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty);

    private sealed record PersistenceOutcome(
        Guid DetectionId,
        Guid DefectId,
        Guid FieldInspectionTaskId,
        Guid VerificationLogId,
        Guid OutboxMessageId);
}
