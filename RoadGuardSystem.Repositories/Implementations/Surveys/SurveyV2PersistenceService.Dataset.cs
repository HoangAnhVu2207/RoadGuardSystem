using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed partial class SurveyV2PersistenceService
{
    private static readonly JsonSerializerOptions DatasetScopeJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SurveyDatasetPersistenceResult> SubmitDatasetAsync(SurveyDatasetSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ActorUserId == Guid.Empty || request.TaskId == Guid.Empty || request.DeviceId == Guid.Empty ||
            request.VideoFileIds is not { Count: > 0 } || request.VideoFileIds.Any(id => id == Guid.Empty) ||
            request.TelemetryFileIds is null || request.TelemetryFileIds.Any(id => id == Guid.Empty) ||
            request.RecordedAt == default || string.IsNullOrWhiteSpace(request.ScopeJson) ||
            string.IsNullOrWhiteSpace(request.ExpectedTaskVersion) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return new(SurveyDatasetPersistenceStatus.InvalidInput);
        }

        var projectId = await _context.SurveyRequests.AsNoTracking()
            .Where(task => task.Id == request.TaskId)
            .Select(task => (Guid?)task.ProjectId)
            .SingleOrDefaultAsync(cancellationToken);
        if (projectId is null) return new(SurveyDatasetPersistenceStatus.NotFound);
        if (!await _context.SurveyRequests.AsNoTracking().AnyAsync(t => t.Id == request.TaskId && t.ScopeFormatVersion == "BAND_V1", cancellationToken))
            return new(SurveyDatasetPersistenceStatus.ScopeIncompatible);

        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                projectId.Value,
                "SurveyDatasetSubmitted",
                request.IdempotencyKey,
                request.RequestFingerprint,
                async token =>
                {
                    var task = await _context.SurveyRequests.SingleOrDefaultAsync(value => value.Id == request.TaskId, token)
                        ?? throw new ScopeNotFoundException();
                    var assignment = await _context.SurveyAssignments
                        .Where(value => value.SurveyRequestId == task.Id && value.EndedAt == null)
                        .OrderByDescending(value => value.AssignedAt)
                        .FirstOrDefaultAsync(token)
                        ?? throw new ScopeNotFoundException();
                    if (task.ScopeFormatVersion != "BAND_V1") return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.ScopeIncompatible, null)));
                    if (await _context.SurveyRequests.AsNoTracking().AnyAsync(childTask => childTask.ParentTaskId == task.Id, token))
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Conflict, null)));
                    if (assignment.OperatorUserId != request.ActorUserId ||
                        task.Status is not (SurveyRequestStatus.Accepted or SurveyRequestStatus.InProgress) || !await HasMembershipAsync(request.ActorUserId, task.ProjectId, token))
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Conflict, null)));
                    }
                    if (!await _context.DroneDevices.AsNoTracking().AnyAsync(d => d.Id == request.DeviceId && d.Status == DroneDeviceStatus.Active, token))
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));

                    if (!TryDecodeVersion(request.ExpectedTaskVersion, out var expectedVersion) || !task.RowVersion.SequenceEqual(expectedVersion))
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.ConcurrencyConflict, null)));
                    }
                    _context.Entry(task).Property(value => value.RowVersion).OriginalValue = expectedVersion;

                    if (!await IsDatasetScopeWithinTaskAsync(task.Id, request.ScopeJson, token))
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));
                    }

                    var allFileIds = request.VideoFileIds.Concat(request.TelemetryFileIds).Distinct().ToArray();
                    DatasetPair[] pairs;
                    try { pairs = JsonSerializer.Deserialize<DatasetPair[]>(request.PairsJson ?? "[]", DatasetScopeJsonOptions) ?? []; }
                    catch (JsonException) { return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null))); }
                    if (pairs.Length > 0 && (pairs.Length != request.VideoFileIds.Count || pairs.Select(p => p.VideoFileId).Distinct().Count() != pairs.Length ||
                        pairs.Any(p => !request.VideoFileIds.Contains(p.VideoFileId) || p.TelemetryFileId is { } t && !request.TelemetryFileIds.Contains(t)) ||
                        pairs.Where(p => p.TelemetryFileId.HasValue).Select(p => p.TelemetryFileId).Distinct().Count() != pairs.Count(p => p.TelemetryFileId.HasValue)))
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));
                    if (allFileIds.Length != request.VideoFileIds.Count + request.TelemetryFileIds.Count)
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));
                    }

                    var fileRows = await (
                        from file in _context.Files.AsNoTracking()
                        join fileScope in _context.FileScopes.AsNoTracking() on file.Id equals fileScope.FileId
                        join upload in _context.UploadSessions.AsNoTracking() on file.Id equals upload.FileId
                        where allFileIds.Contains(file.Id)
                        select new { file.Id, file.Checksum, file.SizeBytes, MediaType = file.MimeType, fileScope.ProjectId, fileScope.TargetId, fileScope.Purpose, UploadPurpose = upload.Purpose, upload.Status })
                        .ToListAsync(token);
                    var videoFileIds = request.VideoFileIds.ToHashSet();
                    if (fileRows.Count != allFileIds.Length || fileRows.Any(value =>
                            value.ProjectId != task.ProjectId || value.TargetId != task.Id || value.Status != UploadSessionStatus.Verified ||
                            value.UploadPurpose != value.Purpose ||
                            value.Purpose != (videoFileIds.Contains(value.Id) ? "SURVEY_VIDEO" : "TELEMETRY")))
                    {
                        return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));
                    }

                    var totalBytes = 0L;
                    foreach (var file in fileRows)
                    {
                        if (file.SizeBytes <= 0 || file.SizeBytes > 34_359_738_368L - totalBytes)
                            return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.InvalidInput, null)));
                        totalBytes += file.SizeBytes;
                    }

                    var survey = await _context.Surveys.SingleOrDefaultAsync(value => value.SurveyRequestId == task.Id, token);
                    if (survey is null)
                    {
                        if (task.RoadSectionVersionId is null)
                        {
                            return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Conflict, null)));
                        }

                        survey = Survey.Create(Guid.NewGuid(), task.Id, task.ProjectId, task.RoadSectionVersionId.Value, task.SurveyType, SurveyStatus.Submitted, false, null, null);
                        _context.Surveys.Add(survey);
                    }

                    var versionNo = (await _context.SurveyDataVersions.AsNoTracking()
                        .Where(value => value.SurveyId == survey.Id)
                        .Select(value => (int?)value.VersionNo)
                        .MaxAsync(token) ?? 0) + 1;
                    var sourceManifest = JsonSerializer.Serialize(fileRows.OrderBy(value => value.Id).Select(value => new { fileId = value.Id, checksumSha256 = value.Checksum, sizeBytes = value.SizeBytes, mediaType = value.MediaType, purpose = value.Purpose }));
                    var dataVersion = SurveyDataVersion.CreateSubmitted(Guid.NewGuid(), survey.Id, versionNo, request.RecordedAt, DateTimeOffset.UtcNow, request.DeviceId, sourceManifest, request.ScopeJson);
                    dataVersion.SetSubmissionProvenance(request.ActorUserId, request.PairsJson ?? "[]");
                    _context.SurveyDataVersions.Add(dataVersion);
                    _context.SurveyFiles.AddRange(fileRows.Select(value => SurveyFile.Create(
                        Guid.NewGuid(), survey.Id, null, value.Id,
                        request.VideoFileIds.Contains(value.Id) ? SurveyFileType.Video : SurveyFileType.Srt,
                        request.RecordedAt, request.RecordedAt, SurveyFileSyncStatus.ServerConfirmed, value.Checksum)));
                    task.MarkSubmitted();
                    var now = DateTimeOffset.UtcNow;
                    _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, now, "survey_dataset_submitted", "SurveyDataVersion", dataVersion.Id, null,
                        JsonSerializer.Serialize(new { surveyTaskId = task.Id, dataVersionId = dataVersion.Id, allFileIds }), "Survey dataset submitted", "p2-019", request.CorrelationId, ["surveyTaskId", "dataVersionId"]));
                    await _context.SaveChangesAsync(token);
                    var view = new SurveyDatasetPersistenceView(dataVersion.Id, task.Id, task.ProjectId, dataVersion.Id, "PASSED", request.TelemetryFileIds.Count == 0 ? "MISSING" : "PRESENT", Version(dataVersion), request.ScopeJson);
                    return (dataVersion.Id, JsonSerializer.Serialize(new StoredDatasetOutcome(SurveyDatasetPersistenceStatus.Success, view)));
                },
                cancellationToken);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(SurveyDatasetPersistenceStatus.IdempotentConflict);
            var stored = JsonSerializer.Deserialize<StoredDatasetOutcome>(outcome.OutcomeJson)
                ?? throw new InvalidOperationException("Dataset idempotency outcome is invalid.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed && stored.Status == SurveyDatasetPersistenceStatus.Success
                ? SurveyDatasetPersistenceStatus.Replayed
                : stored.Status, stored.Dataset);
        }
        catch (ScopeNotFoundException) { return new(SurveyDatasetPersistenceStatus.NotFound); }
        catch (DbUpdateConcurrencyException) { _context.ChangeTracker.Clear(); return new(SurveyDatasetPersistenceStatus.ConcurrencyConflict); }
        catch (ArgumentException) { return new(SurveyDatasetPersistenceStatus.InvalidInput); }
        catch (InvalidOperationException) { return new(SurveyDatasetPersistenceStatus.Conflict); }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { return new(SurveyDatasetPersistenceStatus.Conflict); }
    }

    public async Task<SurveyDatasetAccessView?> GetDatasetAccessAsync(Guid datasetId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from dataset in _context.SurveyDataVersions.AsNoTracking()
            join survey in _context.Surveys.AsNoTracking() on dataset.SurveyId equals survey.Id
            join task in _context.SurveyRequests.AsNoTracking() on survey.SurveyRequestId equals task.Id
            join assignment in _context.SurveyAssignments.AsNoTracking() on task.Id equals assignment.SurveyRequestId
            where dataset.Id == datasetId && assignment.EndedAt == null
            orderby assignment.AssignedAt descending
            select new { dataset, task, assignment.OperatorUserId })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new SurveyDatasetAccessView(row.dataset.Id, row.task.Id, row.task.ProjectId, row.OperatorUserId, row.dataset.ScopeManifest ?? "[]", Version(row.dataset));
    }

    public async Task<DatasetDetailRecord?> ReadDatasetAsync(Guid id, CancellationToken token = default)
    {
        var row = await (from d in _context.SurveyDataVersions.AsNoTracking() join s in _context.Surveys.AsNoTracking() on d.SurveyId equals s.Id
            where d.Id == id select new { d, s.ProjectId, s.SurveyRequestId }).SingleOrDefaultAsync(token);
        return row is null || row.SurveyRequestId is null ? null : new(row.d.Id, row.SurveyRequestId.Value, row.ProjectId, row.d.SubmittedBy, row.d.ConfirmedAt,
            row.d.RecordedAt, row.d.DeviceId, row.d.ScopeManifest ?? "[]", row.d.SourceManifest, row.d.PairsManifest ?? "[]", Version(row.d), row.d.IntegrityStatus.ToString().ToUpperInvariant());
    }

    private async Task<bool> IsDatasetScopeWithinTaskAsync(Guid taskId, string scopeJson, CancellationToken cancellationToken)
    {
        DatasetScopeItem[]? requested;
        try
        {
            requested = JsonSerializer.Deserialize<DatasetScopeItem[]>(scopeJson, DatasetScopeJsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (requested is not { Length: > 0 }) return false;
        var assigned = await _context.SurveyRequestScopes.AsNoTracking()
            .Where(scope => scope.SurveyRequestId == taskId)
            .ToListAsync(cancellationToken);
        if (assigned.Count == 0) return false;

        foreach (var item in requested)
        {
            if (item is null || item.RouteVersionId == Guid.Empty || item.SegmentSetId == Guid.Empty ||
                item.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE") ||
                item.SegmentIds is not { Length: > 0 } || item.SegmentIds.Any(id => id == Guid.Empty))
            {
                return false;
            }

            var taskScope = assigned.SingleOrDefault(scope =>
                scope.RouteSectionVersionId == item.RouteVersionId &&
                scope.SegmentSetId == item.SegmentSetId && scope.TargetBand == item.TargetBand);
            if (taskScope is null) return false;
            var assignedSegmentIds = JsonSerializer.Deserialize<Guid[]>(taskScope.SegmentIdsJson) ?? [];
            if (!item.SegmentIds.All(assignedSegmentIds.Contains)) return false;
        }

        return true;
    }

    private static string Version(SurveyDataVersion version) => Convert.ToBase64String(version.RowVersion);

    private sealed record StoredDatasetOutcome(SurveyDatasetPersistenceStatus Status, SurveyDatasetPersistenceView? Dataset);
    private sealed record DatasetScopeItem(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand);
    private sealed record DatasetPair(Guid VideoFileId, Guid? TelemetryFileId, long TimeOffsetMilliseconds);
}
