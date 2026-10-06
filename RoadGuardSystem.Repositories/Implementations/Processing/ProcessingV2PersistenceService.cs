using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Processing;

public sealed class ProcessingV2PersistenceService : IProcessingV2Repository
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public ProcessingV2PersistenceService(RoadGuardDbContext context, IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public Task<Guid?> GetDatasetProjectIdAsync(Guid datasetId, CancellationToken cancellationToken = default)
        => (from data in _context.SurveyDataVersions.AsNoTracking()
            join survey in _context.Surveys.AsNoTracking() on data.SurveyId equals survey.Id
            where data.Id == datasetId
            select (Guid?)survey.ProjectId).SingleOrDefaultAsync(cancellationToken);

    public async Task<ProcessingJobPersistenceResult> CreateAsync(ProcessingJobCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ActorUserId == Guid.Empty || request.DatasetId == Guid.Empty || request.ModelVersionId == Guid.Empty ||
            !IsMode(request.Mode) || string.IsNullOrWhiteSpace(request.ScopeJson) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return new(ProcessingJobPersistenceStatus.InvalidInput);
        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, null, "ProcessingJobCreated", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var dataset = await (from data in _context.SurveyDataVersions.AsNoTracking()
                                     join survey in _context.Surveys.AsNoTracking() on data.SurveyId equals survey.Id
                                     where data.Id == request.DatasetId && data.Status == SurveyDataVersionStatus.ServerConfirmed && data.IntegrityStatus == SurveyDataIntegrityStatus.Passed
                                     select new { data, survey.ProjectId }).SingleOrDefaultAsync(token) ?? throw new ScopeNotFoundException();
                var modelReleased = await _context.AIModelVersions.AsNoTracking().AnyAsync(model => model.Id == request.ModelVersionId && model.Status == AIModelVersionStatus.Released, token);
                if (!modelReleased) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Conflict, null)));
                var rangeMetadata = JsonSerializer.Serialize(new { scope = JsonSerializer.Deserialize<JsonElement>(request.ScopeJson) });
                var block = await _context.ProcessingBlocks.SingleOrDefaultAsync(
                    value => value.SurveyDataVersionId == request.DatasetId && value.RangeMetadata == rangeMetadata,
                    token);
                if (block is null)
                {
                    var nextBlockNo = (await _context.ProcessingBlocks
                        .Where(value => value.SurveyDataVersionId == request.DatasetId)
                        .MaxAsync(value => (int?)value.BlockNo, token) ?? 0) + 1;
                    block = ProcessingBlock.Create(Guid.NewGuid(), request.DatasetId, nextBlockNo, rangeMetadata);
                    _context.ProcessingBlocks.Add(block);
                }
                var manifest = JsonSerializer.Serialize(new { datasetId = request.DatasetId, sourceManifest = dataset.data.SourceManifest, scope = JsonSerializer.Deserialize<JsonElement>(request.ScopeJson), request.ModelVersionId, request.PreprocessingVersion, request.ConfigVersion, request.Mode });
                var job = ProcessingJob.CreateQueued(Guid.NewGuid(), block.Id, request.ModelVersionId, dataset.ProjectId, Sha256(manifest), manifest, request.Mode.Trim());
                var attempt = ProcessingAttempt.Create(Guid.NewGuid(), job.Id, 1, DateTimeOffset.UtcNow, null, null, null);
                _context.ProcessingJobs.Add(job);
                _context.ProcessingAttempts.Add(attempt);
                _context.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), "processing_job.dispatch", DateTimeOffset.UtcNow, job.Id, JsonSerializer.Serialize(new { jobId = job.Id, attemptId = attempt.Id, job.ManifestHash })));
                _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, DateTimeOffset.UtcNow, "processing_job_created", "ProcessingJob", job.Id, null, "{}", "Processing manifest created", "p2-030", request.CorrelationId, ["datasetId", "modelVersionId"]));
                await _context.SaveChangesAsync(token);
                var view = new ProcessingJobPersistenceView(job.Id, job.ProjectId, ToApiStatus(job.Status), null, 1, Version(job), null);
                return (job.Id, JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Success, view)));
            }, cancellationToken);
            return ReadJobOutcome(outcome);
        }
        catch (ScopeNotFoundException) { return new(ProcessingJobPersistenceStatus.NotFound); }
        catch (ArgumentException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (JsonException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (DbUpdateException exception) when (IsUnique(exception)) { return new(ProcessingJobPersistenceStatus.Conflict); }
    }

    public async Task<ProcessingJobPersistenceView?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _context.ProcessingJobs.AsNoTracking().SingleOrDefaultAsync(value => value.Id == jobId, cancellationToken);
        if (job is null) return null;
        var attempt = await _context.ProcessingAttempts.AsNoTracking().Where(value => value.ProcessingJobId == jobId).MaxAsync(value => (int?)value.AttemptNo, cancellationToken) ?? 0;
        var result = job.Status == ProcessingJobStatus.Completed
            ? await _context.AIDetections.AsNoTracking().Where(value => value.ProcessingJobId == jobId).Select(value => (Guid?)value.Id).FirstOrDefaultAsync(cancellationToken)
            : null;
        return new(job.Id, job.ProjectId, ToApiStatus(job.Status), result, attempt, Version(job), job.ErrorCode);
    }

    public async Task<ProcessingJobPersistenceResult> RetryAsync(ProcessingJobRetryRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _context.ProcessingJobs.AsNoTracking().SingleOrDefaultAsync(value => value.Id == request.JobId, cancellationToken);
        if (job is null) return new(ProcessingJobPersistenceStatus.NotFound);
        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, job.ProjectId, "ProcessingJobRetried", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var tracked = await _context.ProcessingJobs.SingleOrDefaultAsync(value => value.Id == request.JobId, token) ?? throw new ScopeNotFoundException();
                if (!TryDecodeVersion(request.ExpectedVersion, out var expected) || !tracked.RowVersion.SequenceEqual(expected)) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.ConcurrencyConflict, null)));
                _context.Entry(tracked).Property(value => value.RowVersion).OriginalValue = expected;
                try { tracked.Retry(); } catch (InvalidOperationException) { return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Conflict, null))); }
                var nextAttempt = (await _context.ProcessingAttempts.AsNoTracking().Where(value => value.ProcessingJobId == tracked.Id).MaxAsync(value => (int?)value.AttemptNo, token) ?? 0) + 1;
                _context.ProcessingAttempts.Add(ProcessingAttempt.Create(Guid.NewGuid(), tracked.Id, nextAttempt, DateTimeOffset.UtcNow, null, null, null));
                _context.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), "processing_job.dispatch", DateTimeOffset.UtcNow, tracked.Id, JsonSerializer.Serialize(new { jobId = tracked.Id, attemptNumber = nextAttempt, tracked.ManifestHash })));
                _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, DateTimeOffset.UtcNow, "processing_job_retried", "ProcessingJob", tracked.Id, null, JsonSerializer.Serialize(new { request.Reason }), "Processing job retried", "p2-032", request.CorrelationId, ["reason"]));
                await _context.SaveChangesAsync(token);
                return (tracked.Id, JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Success, new ProcessingJobPersistenceView(tracked.Id, tracked.ProjectId, ToApiStatus(tracked.Status), null, nextAttempt, Version(tracked), null))));
            }, cancellationToken);
            return ReadJobOutcome(outcome);
        }
        catch (ScopeNotFoundException) { return new(ProcessingJobPersistenceStatus.NotFound); }
        catch (DbUpdateConcurrencyException) { return new(ProcessingJobPersistenceStatus.ConcurrencyConflict); }
    }

    public async Task<ProcessingJobPersistenceResult> ReceiveResultAsync(ProcessingAiResultRequest request, CancellationToken cancellationToken = default)
    {
        var existingJob = await _context.ProcessingJobs.AsNoTracking().SingleOrDefaultAsync(value => value.Id == request.JobId, cancellationToken);
        if (existingJob is null) return new(ProcessingJobPersistenceStatus.NotFound);
        try
        {
            var outcome = await _idempotency.ExecuteAsync(null, existingJob.ProjectId, "ProcessingAiResultReceived", request.IdempotencyKey, Sha256(request.DetectionsJson + request.ChecksumSha256), async token =>
            {
                var job = await _context.ProcessingJobs.SingleOrDefaultAsync(value => value.Id == request.JobId, token) ?? throw new ScopeNotFoundException();
                var attempt = await _context.ProcessingAttempts.AsNoTracking().SingleOrDefaultAsync(value => value.Id == request.AttemptId && value.ProcessingJobId == job.Id, token);
                if (attempt is null || job.ManifestHash != request.ManifestHash || job.ModelVersionId != request.ModelVersionId || job.Mode != request.Mode) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Conflict, null)));
                var rawFile = await (
                    from file in _context.Files.AsNoTracking()
                    join upload in _context.UploadSessions.AsNoTracking() on file.Id equals upload.FileId
                    join scope in _context.FileScopes.AsNoTracking() on file.Id equals scope.FileId
                    where file.Id == request.RawResultFileId && file.Checksum == request.ChecksumSha256 &&
                          upload.Status == UploadSessionStatus.Verified && scope.ProjectId == job.ProjectId
                    select file).SingleOrDefaultAsync(token);
                if (rawFile is null) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Conflict, null)));
                using var document = JsonDocument.Parse(request.DetectionsJson);
                if (document.RootElement.ValueKind != JsonValueKind.Array) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.InvalidInput, null)));
                foreach (var detection in document.RootElement.EnumerateArray())
                {
                    var id = detection.GetProperty("detectionId").GetGuid();
                    var type = detection.GetProperty("typeCode").GetString();
                    var confidence = detection.GetProperty("confidence").GetDecimal();
                    _context.AIDetections.Add(AIDetection.Create(id, job.Id, job.ModelVersionId, null, null, type, confidence, null, null, detection.GetRawText()));
                }
                job.Complete(DateTimeOffset.UtcNow);
                await _context.SaveChangesAsync(token);
                var resultId = await _context.AIDetections.AsNoTracking().Where(value => value.ProcessingJobId == job.Id).Select(value => (Guid?)value.Id).FirstOrDefaultAsync(token);
                return (job.Id, JsonSerializer.Serialize(new StoredJobOutcome(ProcessingJobPersistenceStatus.Success, new ProcessingJobPersistenceView(job.Id, job.ProjectId, ToApiStatus(job.Status), resultId, attempt.AttemptNo, Version(job), null))));
            }, cancellationToken);
            return ReadJobOutcome(outcome);
        }
        catch (JsonException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (KeyNotFoundException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (InvalidOperationException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (ArgumentException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (DbUpdateException exception) when (IsUnique(exception)) { return new(ProcessingJobPersistenceStatus.Conflict); }
    }

    public async Task<ValidationRunPersistenceResult> CreateValidationAsync(ValidationRunCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty || request.ModelVersionId == Guid.Empty || string.IsNullOrWhiteSpace(request.PairsJson)) return new(ProcessingJobPersistenceStatus.InvalidInput);
        try
        {
            var outcome = await _idempotency.ExecuteAsync(request.ActorUserId, request.ProjectId, "ValidationRunCreated", request.IdempotencyKey, request.RequestFingerprint, async token =>
            {
                var released = await _context.AIModelVersions.AsNoTracking().AnyAsync(value => value.Id == request.ModelVersionId && value.Status == AIModelVersionStatus.Released, token);
                if (!released) return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredValidationOutcome(ProcessingJobPersistenceStatus.Conflict, null)));
                var measurementType = ParseMeasurementType(request.MeasurementType);
                var requestedPairs = JsonSerializer.Deserialize<IReadOnlyList<ValidationPairReference>>(request.PairsJson, WebJson);
                if (measurementType == MeasurementType.Unknown || requestedPairs is not { Count: > 0 } || requestedPairs.Any(value => value.GroundTruthId == Guid.Empty || value.DerivedMeasurementId == Guid.Empty))
                    return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredValidationOutcome(ProcessingJobPersistenceStatus.InvalidInput, null)));
                var facts = await LoadValidationPairFactsAsync(request.ProjectId, requestedPairs, token);
                if (facts.Count != requestedPairs.Count || !PairsAreEligible(facts, requestedPairs, measurementType, request.Unit))
                    return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredValidationOutcome(ProcessingJobPersistenceStatus.InvalidInput, null)));
                var run = ValidationRun.CreateQueued(Guid.NewGuid(), request.ProjectId, request.ModelVersionId, request.DatasetSplitId, request.MeasurementType, request.Unit, request.PairsJson);
                _context.ValidationRuns.Add(run);
                foreach (var pair in requestedPairs)
                {
                    var fact = facts.Single(value => value.GroundTruthId == pair.GroundTruthId && value.DerivedMeasurementId == pair.DerivedMeasurementId);
                    var signedError = ConvertUnit(fact.DerivedValue, fact.Unit, request.Unit) - ConvertUnit(fact.GroundTruthValue, fact.Unit, request.Unit);
                    _context.MeasurementValidationSamples.Add(MeasurementValidationSample.Create(Guid.NewGuid(), run.Id, fact.GroundTruthId, fact.DerivedMeasurementId, signedError, ValidationSampleInclusionStatus.Included, null));
                }
                _context.OutboxMessages.Add(OutboxMessage.Create(Guid.NewGuid(), "validation_run.dispatch", DateTimeOffset.UtcNow, run.Id, JsonSerializer.Serialize(new { validationRunId = run.Id, request.ModelVersionId, request.DatasetSplitId })));
                _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), request.ActorUserId, DateTimeOffset.UtcNow, "validation_run_created", "ValidationRun", run.Id, null, "{}", "Validation run queued", "p2-034", request.CorrelationId, ["modelVersionId", "datasetSplitId"]));
                await _context.SaveChangesAsync(token);
                return (run.Id, JsonSerializer.Serialize(new StoredValidationOutcome(ProcessingJobPersistenceStatus.Success, ToValidationView(run))));
            }, cancellationToken);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(ProcessingJobPersistenceStatus.IdempotentConflict);
            var stored = JsonSerializer.Deserialize<StoredValidationOutcome>(outcome.OutcomeJson) ?? throw new InvalidOperationException();
            // A replayed receipt preserves the original business outcome. Only a stored
            // success is exposed as Replayed; failed validation must remain a failure so
            // the HTTP layer does not turn it into a null-success fallback.
            return new(outcome.Status == IdempotencyOperationStatus.Replayed && stored.Status == ProcessingJobPersistenceStatus.Success
                ? ProcessingJobPersistenceStatus.Replayed
                : stored.Status, stored.Run);
        }
        catch (ArgumentException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
        catch (JsonException) { return new(ProcessingJobPersistenceStatus.InvalidInput); }
    }

    public async Task<ValidationRunPersistenceView?> GetValidationAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _context.ValidationRuns.AsNoTracking().SingleOrDefaultAsync(value => value.Id == runId, cancellationToken);
        return run is null ? null : ToValidationView(run);
    }

    public async Task<bool> CompleteNextValidationRunAsync(CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .Where(value => value.MessageType == "validation_run.dispatch" && value.DeliveryStatus == OutboxDeliveryStatus.Pending && value.NextAttemptAtUtc <= DateTimeOffset.UtcNow)
            .OrderBy(value => value.OccurredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (message?.CorrelationId is not Guid runId) return false;

        message.AcquireLease("validation-run-worker", DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), 3);
        var run = await _context.ValidationRuns.SingleOrDefaultAsync(value => value.Id == runId && value.Status == ValidationRunStatus.Queued, cancellationToken);
        if (run is null)
        {
            message.CompleteLease("validation-run-worker");
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var samples = await _context.MeasurementValidationSamples.AsNoTracking()
            .Where(value => value.ValidationRunId == run.Id && value.InclusionStatus == ValidationSampleInclusionStatus.Included)
            .ToListAsync(cancellationToken);
        if (samples.Count == 0)
        {
            message.ScheduleRetry("validation-run-worker", DateTimeOffset.UtcNow.AddMinutes(5), "validation_samples_missing", "Validation run has no included sample provenance.", 3);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        var bias = decimal.Round(samples.Average(value => value.SignedError), 6, MidpointRounding.AwayFromZero);
        var mae = decimal.Round(samples.Average(value => value.AbsoluteError), 6, MidpointRounding.AwayFromZero);
        var rmse = decimal.Round((decimal)Math.Sqrt(samples.Average(value => (double)(value.SignedError * value.SignedError))), 6, MidpointRounding.AwayFromZero);
        try
        {
            run.Complete(samples.Count, 0, bias, mae, rmse, "[]");
            message.CompleteLease("validation-run-worker");
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static ProcessingJobPersistenceResult ReadJobOutcome(IdempotencyOperationResult outcome)
    {
        if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(ProcessingJobPersistenceStatus.IdempotentConflict);
        var stored = JsonSerializer.Deserialize<StoredJobOutcome>(outcome.OutcomeJson) ?? throw new InvalidOperationException("Processing outcome is invalid.");
        return new(outcome.Status == IdempotencyOperationStatus.Replayed && stored.Status == ProcessingJobPersistenceStatus.Success ? ProcessingJobPersistenceStatus.Replayed : stored.Status, stored.Job);
    }

    private async Task<List<ValidationPairFact>> LoadValidationPairFactsAsync(Guid projectId, IReadOnlyList<ValidationPairReference> pairs, CancellationToken cancellationToken)
    {
        var groundTruthIds = pairs.Select(value => value.GroundTruthId).Distinct().ToArray();
        var derivedIds = pairs.Select(value => value.DerivedMeasurementId).Distinct().ToArray();
        return await (from groundTruth in _context.GroundTruthMeasurements.AsNoTracking()
                      join session in _context.FieldInspectionSessions.AsNoTracking() on groundTruth.FieldInspectionSessionId equals session.Id
                      join derived in _context.DerivedMeasurements.AsNoTracking() on groundTruth.SampleId equals derived.SampleId
                      join data in _context.SurveyDataVersions.AsNoTracking() on derived.SurveyDataVersionId equals data.Id
                      join survey in _context.Surveys.AsNoTracking() on data.SurveyId equals survey.Id
                      where groundTruthIds.Contains(groundTruth.Id) && derivedIds.Contains(derived.Id) &&
                            session.ProjectId == projectId && survey.ProjectId == projectId &&
                            groundTruth.Value.HasValue && groundTruth.ValueState == "KNOWN"
                      select new ValidationPairFact(groundTruth.Id, derived.Id, groundTruth.SampleId, groundTruth.RoadSectionVersionId, derived.RoadSectionVersionId, groundTruth.MeasurementType, derived.MeasurementType, groundTruth.Value!.Value, derived.Value, groundTruth.Unit, derived.Unit, derived.Status)).ToListAsync(cancellationToken);
    }

    private static bool PairsAreEligible(IReadOnlyList<ValidationPairFact> facts, IReadOnlyList<ValidationPairReference> pairs, MeasurementType measurementType, string unit)
    {
        if (!IsSupportedUnit(unit)) return false;
        return pairs.Distinct().Count() == pairs.Count && pairs.All(pair => facts.Count(value => value.GroundTruthId == pair.GroundTruthId && value.DerivedMeasurementId == pair.DerivedMeasurementId) == 1 &&
            facts.Single(value => value.GroundTruthId == pair.GroundTruthId && value.DerivedMeasurementId == pair.DerivedMeasurementId) is { MeasurementType: var groundTruthType, DerivedMeasurementType: var derivedType, GroundTruthRoadSectionVersionId: var groundTruthRoadVersion, DerivedRoadSectionVersionId: var derivedRoadVersion, Status: DerivedMeasurementStatus.Published } &&
            groundTruthType == measurementType && derivedType == measurementType && groundTruthRoadVersion == derivedRoadVersion && IsSupportedUnit(facts.Single(value => value.GroundTruthId == pair.GroundTruthId && value.DerivedMeasurementId == pair.DerivedMeasurementId).Unit) && IsSupportedUnit(facts.Single(value => value.GroundTruthId == pair.GroundTruthId && value.DerivedMeasurementId == pair.DerivedMeasurementId).DerivedUnit));
    }

    private static MeasurementType ParseMeasurementType(string value) => value.Trim() switch { "DEPRESSION_DEPTH" => MeasurementType.DepressionDepth, "SLAB_FAULTING_HEIGHT" => MeasurementType.SlabFaultingHeight, "SHOULDER_EROSION_EXTENT" => MeasurementType.ShoulderErosionExtent, _ => MeasurementType.Unknown };
    private static bool IsSupportedUnit(string value) => value.Trim().ToLowerInvariant() is "mm" or "cm" or "m";
    private static decimal ConvertUnit(decimal value, string sourceUnit, string destinationUnit)
    {
        var millimetres = sourceUnit.Trim().ToLowerInvariant() switch { "mm" => value, "cm" => value * 10m, "m" => value * 1000m, _ => throw new ArgumentException("Unsupported source unit.", nameof(sourceUnit)) };
        return destinationUnit.Trim().ToLowerInvariant() switch { "mm" => millimetres, "cm" => millimetres / 10m, "m" => millimetres / 1000m, _ => throw new ArgumentException("Unsupported destination unit.", nameof(destinationUnit)) };
    }
    private static ValidationRunPersistenceView ToValidationView(ValidationRun run) => new(run.Id, run.ProjectId, run.Status.ToString().ToUpperInvariant(), Convert.ToBase64String(run.RowVersion), run.UsedCount, run.ExcludedCount, run.Bias, run.Mae, run.Rmse, run.Unit, run.ExclusionReasonsJson);
    private static bool IsMode(string value) => value is "MOCK" or "REAL";
    private static string ToApiStatus(ProcessingJobStatus status) => status switch { ProcessingJobStatus.Completed => "SUCCEEDED", ProcessingJobStatus.RetryableFailure or ProcessingJobStatus.DataFailure => "FAILED", _ => status.ToString().ToUpperInvariant() };
    private static string Version(ProcessingJob job) => Convert.ToBase64String(job.RowVersion);
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static bool TryDecodeVersion(string value, out byte[] result) { try { result = Convert.FromBase64String(value.Trim().Trim('"')); return result.Length > 0; } catch (FormatException) { result = []; return false; } }
    private static bool IsUnique(DbUpdateException exception) { for (Exception? current = exception; current is not null; current = current.InnerException) if (current is SqlException { Number: 2601 or 2627 }) return true; return false; }

    private sealed record StoredJobOutcome(ProcessingJobPersistenceStatus Status, ProcessingJobPersistenceView? Job);
    private sealed record StoredValidationOutcome(ProcessingJobPersistenceStatus Status, ValidationRunPersistenceView? Run);
    private sealed record ValidationPairReference(Guid GroundTruthId, Guid DerivedMeasurementId);
    private sealed record ValidationPairFact(Guid GroundTruthId, Guid DerivedMeasurementId, string SampleId, Guid GroundTruthRoadSectionVersionId, Guid DerivedRoadSectionVersionId, MeasurementType MeasurementType, MeasurementType DerivedMeasurementType, decimal GroundTruthValue, decimal DerivedValue, string Unit, string DerivedUnit, DerivedMeasurementStatus Status);
    private sealed class ScopeNotFoundException : Exception;
}
