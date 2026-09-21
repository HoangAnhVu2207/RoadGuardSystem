using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Interfaces.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

/// <summary>
/// Persists an already-authorized dataset admission and its durable validation intent.
/// P1-30 owns HTTP/workflow policy and P2-31 owns worker execution.
/// </summary>
public sealed class SurveyDataValidationAdmissionPersistenceService : ISurveyDataValidationAdmissionRepository
{
    private const string Operation = "SurveyDataValidationAdmitted";
    private const string EventType = "survey_data_validation.admitted";
    private static readonly string[] AuditProperties = ["surveyDataVersionId", "surveyFileIds"];
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public SurveyDataValidationAdmissionPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public async Task<SurveyDataValidationAdmissionResult> AdmitAsync(
        SurveyDataValidationAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.CorrelationId == Guid.Empty || request.DataVersion.Id == Guid.Empty)
        {
            throw new ArgumentException("Admission actor, correlation, and dataset ids must not be empty.", nameof(request));
        }

        var survey = await _context.Surveys.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.DataVersion.SurveyId, cancellationToken)
            ?? throw new InvalidOperationException("Survey dataset admission requires an existing survey.");
        var surveyFileIds = request.SurveyFileIds.Distinct().ToArray();
        if (surveyFileIds.Length == 0 || surveyFileIds.Any(item => item == Guid.Empty))
        {
            throw new ArgumentException("Dataset admission requires at least one survey file.", nameof(request));
        }
        if (request.DataVersion.Status is not (SurveyDataVersionStatus.Draft or SurveyDataVersionStatus.Uploading) ||
            request.DataVersion.IntegrityStatus != SurveyDataIntegrityStatus.Pending)
        {
            throw new InvalidOperationException("Dataset admission requires a pending draft or uploading dataset version.");
        }

        var outcome = await _idempotency.ExecuteAsync(
            request.ActorUserId,
            survey.ProjectId,
            Operation,
            request.IdempotencyKey,
            request.RequestFingerprint,
            async operationCancellationToken =>
            {
                if (await _context.SurveyDataVersions.AsNoTracking().AnyAsync(
                        item => item.Id == request.DataVersion.Id,
                        operationCancellationToken))
                {
                    throw new InvalidOperationException("Survey dataset version already exists.");
                }

                var files = await _context.SurveyFiles.AsNoTracking()
                    .Where(item => surveyFileIds.Contains(item.Id))
                    .Select(item => new { item.Id, item.SurveyId, item.SyncStatus, item.Checksum })
                    .ToListAsync(operationCancellationToken);
                if (files.Count != surveyFileIds.Length || files.Any(item => item.SurveyId != request.DataVersion.SurveyId || item.SyncStatus == SurveyFileSyncStatus.Invalid))
                {
                    throw new InvalidOperationException("Dataset admission requires valid survey files from the same survey.");
                }

                var now = DateTimeOffset.UtcNow;
                var outboxMessageId = Guid.NewGuid();
                var canonicalManifest = JsonSerializer.Serialize(files
                    .OrderBy(item => item.Id)
                    .Select(item => new { survey_file_id = item.Id, checksum = item.Checksum }));
                var admittedVersion = SurveyDataVersion.Create(
                    request.DataVersion.Id,
                    request.DataVersion.SurveyId,
                    request.DataVersion.VersionNo,
                    request.DataVersion.Status,
                    request.DataVersion.IntegrityStatus,
                    request.DataVersion.ConfirmedAt,
                    request.DataVersion.ConfirmedBy,
                    canonicalManifest);
                _context.SurveyDataVersions.Add(admittedVersion);
                _context.AuditLogs.Add(AuditLog.Create(
                    Guid.NewGuid(),
                    request.ActorUserId,
                    now,
                    EventType,
                    "SurveyDataVersion",
                    request.DataVersion.Id,
                    null,
                    JsonSerializer.Serialize(new { surveyDataVersionId = request.DataVersion.Id, surveyFileIds }),
                    "Dataset validation admitted",
                    "p2-30.persistence",
                    request.CorrelationId,
                    AuditProperties));
                _context.OutboxMessages.Add(OutboxMessage.Create(
                    outboxMessageId,
                    EventType,
                    now,
                    request.DataVersion.Id,
                    JsonSerializer.Serialize(new
                    {
                        surveyDataVersionId = request.DataVersion.Id,
                        request.DataVersion.SurveyId,
                        surveyFileIds
                    })));
                return (request.DataVersion.Id, JsonSerializer.Serialize(new AdmissionOutcome(request.DataVersion.Id, outboxMessageId)));
            },
            cancellationToken);
        var admission = JsonSerializer.Deserialize<AdmissionOutcome>(outcome.OutcomeJson)
            ?? throw new InvalidOperationException("Dataset admission idempotency outcome was invalid.");
        return new SurveyDataValidationAdmissionResult(admission.SurveyDataVersionId, admission.OutboxMessageId, outcome.Status);
    }

    private sealed record AdmissionOutcome(Guid SurveyDataVersionId, Guid OutboxMessageId);
}
