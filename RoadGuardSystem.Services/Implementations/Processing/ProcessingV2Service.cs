using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Implementations.Processing;

public sealed class ProcessingV2Service : IProcessingV2Service
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private readonly IProcessingV2Repository _repository;
    private readonly IProjectScopeGuard _scopeGuard;

    public ProcessingV2Service(IProcessingV2Repository repository, IProjectScopeGuard scopeGuard)
    {
        _repository = repository;
        _scopeGuard = scopeGuard;
    }

    public async Task<ProcessingV2ServiceResult> CreateAsync(Guid actorUserId, UserRoleCode role, CreateProcessingJobRequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (!IsManager(actorUserId, role) || request is null || request.DatasetId == Guid.Empty || request.ModelVersionId == Guid.Empty || request.Scope is null || !ValidScope(request.Scope) || !IsMode(request.Mode) || string.IsNullOrWhiteSpace(request.PreprocessingVersion) || string.IsNullOrWhiteSpace(request.ConfigVersion) || string.IsNullOrWhiteSpace(idempotencyKey)) return new(ProcessingV2ServiceStatus.InvalidInput);
        var datasetProjectId = await _repository.GetDatasetProjectIdAsync(request.DatasetId, cancellationToken);
        if (datasetProjectId is null) return new(ProcessingV2ServiceStatus.NotFound);
        if (!await InScope(actorUserId, role, datasetProjectId.Value, cancellationToken)) return new(ProcessingV2ServiceStatus.Forbidden);
        var scopeJson = JsonSerializer.Serialize(request.Scope);
        var result = await _repository.CreateAsync(new(actorUserId, request.DatasetId, scopeJson, request.ModelVersionId, request.PreprocessingVersion, request.ConfigVersion, request.Mode, idempotencyKey, Hash($"{request.DatasetId:N}|{scopeJson}|{request.ModelVersionId:N}|{request.PreprocessingVersion}|{request.ConfigVersion}|{request.Mode}"), correlationId), cancellationToken);
        return Map(result);
    }

    public async Task<ProcessingV2ServiceResult> GetAsync(Guid actorUserId, UserRoleCode role, Guid jobId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator)) return new(ProcessingV2ServiceStatus.Forbidden);
        var job = await _repository.GetAsync(jobId, cancellationToken);
        if (job is null) return new(ProcessingV2ServiceStatus.NotFound);
        if (!await InScope(actorUserId, role, job.ProjectId, cancellationToken)) return new(ProcessingV2ServiceStatus.Forbidden);
        return new(ProcessingV2ServiceStatus.Success, ToDto(job));
    }

    public async Task<ProcessingV2ServiceResult> RetryAsync(Guid actorUserId, UserRoleCode role, Guid jobId, RetryProcessingJobRequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (!IsManager(actorUserId, role) || request is null || string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion)) return new(ProcessingV2ServiceStatus.InvalidInput);
        var job = await _repository.GetAsync(jobId, cancellationToken);
        if (job is null) return new(ProcessingV2ServiceStatus.NotFound);
        if (!await InScope(actorUserId, role, job.ProjectId, cancellationToken)) return new(ProcessingV2ServiceStatus.Forbidden);
        return Map(await _repository.RetryAsync(new(actorUserId, jobId, request.Reason, expectedVersion.Trim().Trim('"'), idempotencyKey, Hash($"{jobId:N}|{request.Reason}|{expectedVersion}"), correlationId), cancellationToken));
    }

    public async Task<ProcessingV2ServiceResult> ReceiveResultAsync(Guid jobId, ReceiveAiResultRequestDto request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty || request is null || request.JobAttemptId == Guid.Empty || request.ModelVersionId == Guid.Empty || request.RawResultFileId == Guid.Empty || !IsMode(request.Mode) || !Sha256(request.ManifestHash) || !Sha256(request.ChecksumSha256) || request.Detections is null || string.IsNullOrWhiteSpace(idempotencyKey)) return new(ProcessingV2ServiceStatus.InvalidInput);
        if (request.Detections.Any(item => !Guid.TryParse(item.DetectionId, out _) || item.FrameFileId == Guid.Empty || item.TimestampMs < 0 || string.IsNullOrWhiteSpace(item.TypeCode) || item.Confidence is < 0m or > 1m || item.Bbox is not { Count: 4 } || item.Bbox.Any(value => value is < 0m or > 1m) || item.Bbox[0] + item.Bbox[2] > 1m || item.Bbox[1] + item.Bbox[3] > 1m)) return new(ProcessingV2ServiceStatus.InvalidInput);
        var detectionsJson = JsonSerializer.Serialize(
            request.Detections.Select(item => new
            {
                detectionId = Guid.Parse(item.DetectionId),
                item.FrameFileId,
                item.TimestampMs,
                item.TypeCode,
                item.Confidence,
                item.Bbox
            }),
            WebJson);
        return Map(await _repository.ReceiveResultAsync(new(jobId, request.JobAttemptId, request.ManifestHash, request.ModelVersionId, request.Mode, request.RawResultFileId, request.ChecksumSha256, detectionsJson, idempotencyKey), cancellationToken));
    }

    public async Task<ProcessingV2ServiceResult> CreateValidationAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateValidationRunRequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || projectId == Guid.Empty || request is null || request.ModelVersionId == Guid.Empty || request.DatasetSplitId == Guid.Empty || request.Pairs is not { Count: > 0 } || request.Pairs.Any(item => item.GroundTruthId == Guid.Empty || item.DerivedMeasurementId == Guid.Empty) || string.IsNullOrWhiteSpace(request.MeasurementType) || string.IsNullOrWhiteSpace(request.Unit) || string.IsNullOrWhiteSpace(idempotencyKey)) return new(ProcessingV2ServiceStatus.InvalidInput);
        if (role != UserRoleCode.ProjectManager) return new(ProcessingV2ServiceStatus.Forbidden);
        if (!await InScope(actorUserId, role, projectId, cancellationToken)) return new(ProcessingV2ServiceStatus.Forbidden);
        var result = await _repository.CreateValidationAsync(new(actorUserId, projectId, request.ModelVersionId, request.DatasetSplitId.ToString(), request.MeasurementType, request.Unit, JsonSerializer.Serialize(request.Pairs), idempotencyKey, Hash($"{projectId:N}|{request.ModelVersionId:N}|{request.DatasetSplitId:N}|{request.MeasurementType}|{request.Unit}|{JsonSerializer.Serialize(request.Pairs)}"), correlationId), cancellationToken);
        var validation = result.Run is null ? null : ToValidationDto(result.Run);
        return result.Status switch { ProcessingJobPersistenceStatus.Success => new(ProcessingV2ServiceStatus.Success, Job: result.Run is null ? null : ToValidationJob(result.Run), Validation: validation), ProcessingJobPersistenceStatus.Replayed => new(ProcessingV2ServiceStatus.Replayed, Job: result.Run is null ? null : ToValidationJob(result.Run), Validation: validation), ProcessingJobPersistenceStatus.IdempotentConflict => new(ProcessingV2ServiceStatus.IdempotentConflict), ProcessingJobPersistenceStatus.Conflict => new(ProcessingV2ServiceStatus.Conflict), _ => new(ProcessingV2ServiceStatus.InvalidInput) };
    }

    public async Task<ValidationResultDto?> GetValidationAsync(Guid actorUserId, UserRoleCode role, Guid runId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) return null;
        var run = await _repository.GetValidationAsync(runId, cancellationToken);
        if (run is null || !await InScope(actorUserId, role, run.ProjectId, cancellationToken)) return null;
        try { return ToValidationDto(run); } catch (JsonException) { return null; }
    }

    private async Task<bool> InScope(Guid actor, UserRoleCode role, Guid projectId, CancellationToken token) => await _scopeGuard.AuthorizeAsync(actor, role, projectId, token) is not null;
    private static bool IsManager(Guid actor, UserRoleCode role) => actor != Guid.Empty && role is UserRoleCode.ProjectManager or UserRoleCode.Supervisor;
    private static bool IsMode(string value) => value is "MOCK" or "REAL";
    private static bool Sha256(string value) => value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool ValidScope(RoadGuardSystem.DTOs.Surveys.BandScopeDto scope) => scope.RouteVersionId != Guid.Empty && scope.SegmentSetId != Guid.Empty && scope.SegmentIds is { Count: > 0 } && scope.SegmentIds.All(value => value != Guid.Empty) && scope.TargetBand is "SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE";
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static ProcessingJobResponseDto ToDto(ProcessingJobPersistenceView value) => new(value.Id, value.Status, value.ResultId, value.AttemptNumber, value.Version, "AI_ANALYSIS", value.Error);
    private static ProcessingJobResponseDto ToValidationJob(ValidationRunPersistenceView value) => new(value.Id, value.Status, null, 0, value.Version, "VALIDATION", null);
    private static ValidationResultDto ToValidationDto(ValidationRunPersistenceView value) => new(value.Id, value.UsedCount, value.ExcludedCount, value.Bias, value.Mae, value.Rmse, value.Unit, JsonSerializer.Deserialize<IReadOnlyList<string>>(value.ExclusionReasonsJson) ?? []);
    private static ProcessingV2ServiceResult Map(ProcessingJobPersistenceResult value) => value.Status switch { ProcessingJobPersistenceStatus.Success => new(ProcessingV2ServiceStatus.Success, ToDto(value.Job!)), ProcessingJobPersistenceStatus.Replayed => new(ProcessingV2ServiceStatus.Replayed, ToDto(value.Job!)), ProcessingJobPersistenceStatus.NotFound => new(ProcessingV2ServiceStatus.NotFound), ProcessingJobPersistenceStatus.ConcurrencyConflict => new(ProcessingV2ServiceStatus.ConcurrencyConflict), ProcessingJobPersistenceStatus.IdempotentConflict => new(ProcessingV2ServiceStatus.IdempotentConflict), ProcessingJobPersistenceStatus.Conflict => new(ProcessingV2ServiceStatus.Conflict), _ => new(ProcessingV2ServiceStatus.InvalidInput) };
}
