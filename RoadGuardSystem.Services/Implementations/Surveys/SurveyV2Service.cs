using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public sealed class SurveyV2Service : ISurveyV2Service
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly ISurveyV2Repository _repository;
    private readonly IProjectScopeGuard _scopeGuard;
    private readonly ISurveyAssessmentRepository? _assessments;

    public SurveyV2Service(ISurveyV2Repository repository, IProjectScopeGuard scopeGuard, ISurveyAssessmentRepository? assessments = null) { _repository = repository; _scopeGuard = scopeGuard; _assessments = assessments; }

    public async Task<(SurveyV2ServiceStatus Status, IReadOnlyList<EligibleDroneDto> Items)> ListEligibleDronesAsync(
        Guid actor, UserRoleCode role, Guid taskId, Guid? afterId, int take, CancellationToken cancellationToken = default)
    {
        if (role != UserRoleCode.DroneOperator) return (SurveyV2ServiceStatus.Forbidden, []);
        var access = await GetTaskAsync(actor, role, taskId, cancellationToken);
        if (access.Status != SurveyV2ServiceStatus.Success) return (access.Status, []);
        if (take is < 1 or > 101) return (SurveyV2ServiceStatus.InvalidInput, []);
        var drones = await _repository.ListActiveDronesAsync(afterId, take, cancellationToken);
        return (SurveyV2ServiceStatus.Success, drones.Select(d => new EligibleDroneDto(d.Id, d.SerialNo)).ToArray());
    }

    public async Task<SurveyV2ServiceResult> CreatePlanAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateSurveyPlanV2RequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role != UserRoleCode.ProjectManager) return new(SurveyV2ServiceStatus.Forbidden);
        if (projectId == Guid.Empty || request.Scope is null || request.Scope.Count == 0 || string.IsNullOrWhiteSpace(idempotencyKey) || !TryType(request.SurveyType, out var type) || !ValidScope(request.Scope)) return new(SurveyV2ServiceStatus.InvalidInput);
        if (!await InScope(actorUserId, role, projectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var frequency = request.Recurrence?.Frequency?.Trim().ToUpperInvariant();
        if (request.Recurrence is not null && frequency is not ("NONE" or "WEEKLY" or "MONTHLY"))
            return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: new("survey_recurrence_invalid", "recurrence.frequency"));
        if (frequency is "WEEKLY" or "MONTHLY" && request.Recurrence?.UntilWarrantyEnd != true)
            return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: new("survey_recurrence_invalid", "recurrence.untilWarrantyEnd"));

        var scope = JsonSerializer.Serialize(request.Scope, ResponseJsonOptions);
        var scopeItems = request.Scope.Select(item => new SurveyV2ScopeRequest(
            item.RouteVersionId,
            item.SegmentSetId,
            JsonSerializer.Serialize(item.SegmentIds),
            item.TargetBand)).ToArray();
        var basePayload = new
        {
            projectId,
            scope = request.Scope,
            plannedAt = request.PlannedAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            surveyType = request.SurveyType.Trim().ToUpperInvariant()
        };
        var fingerprint = request.WarrantyId is null && request.Recurrence is null
            ? Fingerprint(basePayload)
            : Fingerprint(new { plan = basePayload, request.WarrantyId, frequency, untilWarrantyEnd = request.Recurrence?.UntilWarrantyEnd });
        return MapPlan(await _repository.CreatePlanAsync(new(actorUserId, projectId, request.Scope[0].RouteVersionId,
            request.PlannedAt, type, scope, idempotencyKey, fingerprint, correlationId, scopeItems,
            request.WarrantyId, frequency == "NONE" ? null : frequency), cancellationToken));
    }

    public async Task<SurveyV2ServiceResult> PostponePlanAsync(Guid actorUserId, UserRoleCode role, Guid planId, PostponeSurveyPlanV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role != UserRoleCode.ProjectManager) return new(SurveyV2ServiceStatus.Forbidden);
        if (planId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion)) return new(SurveyV2ServiceStatus.InvalidInput);
        var projectId = await _repository.GetPlanProjectIdAsync(planId, cancellationToken);
        if (projectId is null) return new(SurveyV2ServiceStatus.NotFound);
        if (!await InScope(actorUserId, role, projectId.Value, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var version = expectedVersion.Trim().Trim('"');
        var basePayload = new { planId, reason = request.Reason.Trim(), expectedVersion = version };
        if (request.WarrantyId is not null && request.PlannedAt is null)
            return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: new("survey_time_range_invalid", "plannedAt"));
        var fingerprint = request.PlannedAt is null
            ? Fingerprint(basePayload)
            : Fingerprint(new { plan = basePayload, plannedAt = request.PlannedAt?.ToUniversalTime(), request.WarrantyId });
        var result = await _repository.PostponePlanAsync(new(actorUserId, planId, request.Reason, version, idempotencyKey,
            fingerprint, correlationId, request.PlannedAt, request.WarrantyId), cancellationToken);
        if (result.ScheduleError is not null) return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: result.ScheduleError);
        return result.Status switch { SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, ToPlan(result.Plan)), SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, ToPlan(result.Plan)), SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound), SurveyV2PersistenceStatus.ConcurrencyConflict => new(SurveyV2ServiceStatus.ConcurrencyConflict), SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict), SurveyV2PersistenceStatus.ScopeIncompatible => new(SurveyV2ServiceStatus.ScopeIncompatible), _ => new(SurveyV2ServiceStatus.Conflict) };
    }

    public async Task<SurveyV2ServiceResult> CreateTaskAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateSurveyTaskV2RequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role != UserRoleCode.ProjectManager) return new(SurveyV2ServiceStatus.Forbidden);
        if (projectId == Guid.Empty || request.Scope is null || request.Scope.Count == 0 || request.OperatorId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey) || !TryType(request.SurveyType, out var type) || !ValidScope(request.Scope) || !ValidPosition(request.AccessPoint)) return new(SurveyV2ServiceStatus.InvalidInput);
        if (!await InScope(actorUserId, role, projectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var scope = JsonSerializer.Serialize(request.Scope, ResponseJsonOptions);
        var accessPointJson = request.AccessPoint is null ? null : JsonSerializer.Serialize(request.AccessPoint);
        var scopeItems = request.Scope.Select(item => new SurveyV2ScopeRequest(
            item.RouteVersionId,
            item.SegmentSetId,
            JsonSerializer.Serialize(item.SegmentIds),
            item.TargetBand)).ToArray();
        if (request.OccurrenceId is not null && request.PlanId is null)
            return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: new("survey_recurrence_invalid", "planId"));
        var payload = $"{projectId:N}|{scope}|{request.OperatorId:N}|{request.SurveyType}|{request.DueAt:O}|{accessPointJson}|{request.PlanId}";
        var fingerprint = Fingerprint(request.OccurrenceId is null ? payload : $"{payload}|{request.OccurrenceId}");
        var result = await _repository.CreateTaskAsync(new(actorUserId, projectId, request.Scope[0].RouteVersionId,
            request.OperatorId, type, request.DueAt, scope, accessPointJson, idempotencyKey, fingerprint,
            correlationId, scopeItems, request.PlanId, request.OccurrenceId), cancellationToken);
        if (result.ScheduleError is not null) return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: result.ScheduleError);
        return result.Status switch { SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, Task: ToTask(result.Task)), SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, Task: ToTask(result.Task)), SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound), SurveyV2PersistenceStatus.TaskPlanMismatch => new(SurveyV2ServiceStatus.TaskPlanMismatch), SurveyV2PersistenceStatus.PlanStateConflict => new(SurveyV2ServiceStatus.PlanStateConflict), SurveyV2PersistenceStatus.SourceScopeConflict => new(SurveyV2ServiceStatus.SourceScopeConflict), SurveyV2PersistenceStatus.OperatorNotFound => new(SurveyV2ServiceStatus.OperatorNotFound), SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict), _ => new(SurveyV2ServiceStatus.Conflict) };
    }

    public async Task<SurveyV2ServiceResult> GetTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || taskId == Guid.Empty || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator)) return new(SurveyV2ServiceStatus.Forbidden);
        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task is null) return new(SurveyV2ServiceStatus.NotFound);
        if (role == UserRoleCode.DroneOperator && (task.OperatorId != actorUserId || !task.AssignmentIsActive)) return new(SurveyV2ServiceStatus.Forbidden);
        if (!await InScope(actorUserId, role, task.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        if (role == UserRoleCode.DroneOperator && task.SupplementTaskId is Guid childId)
        {
            var child = await _repository.GetTaskAsync(childId, cancellationToken);
            if (child is null || child.ProjectId != task.ProjectId || child.OperatorId != actorUserId ||
                !child.AssignmentIsActive || ToTask(child) is null)
                task = task with { SupplementTaskId = null };
        }
        return new(SurveyV2ServiceStatus.Success, Task: ToTask(task));
    }

    public async Task<SurveyTaskPageV2ResponseDto?> ListMyTasksAsync(Guid actorUserId, UserRoleCode role, string? cursor, int? limit, RoadGuardSystem.Repositories.Mobile.MobileTaskListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role != UserRoleCode.DroneOperator) return null;
        var pageSize = limit ?? 50;
        if (pageSize is < 1 or > 100) return null;
        try
        {
            var page = await _repository.ListMyTasksAsync(actorUserId, cursor, pageSize, filter, cancellationToken);
            var visible = new List<SurveyTaskV2ResponseDto>();
            var readableProjects = new HashSet<Guid>();
            foreach (var project in page.Items.Select(item => item.ProjectId).Distinct())
                if (await InScope(actorUserId, role, project, cancellationToken)) readableProjects.Add(project);
            foreach (var item in page.Items)
                if (readableProjects.Contains(item.ProjectId) && ToTask(item) is { } view) visible.Add(view);
            return new(visible, page.NextCursor, page.AsOf);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public async Task<SurveyV2ServiceResult> CompletePlanAsync(Guid actor, UserRoleCode role, Guid planId, CompleteSurveyPlanRequestDto request, string key, string expectedVersion, CancellationToken token = default)
    {
        if (actor == Guid.Empty || role != UserRoleCode.ProjectManager) return new(SurveyV2ServiceStatus.Forbidden);
        var project = await _repository.GetPlanProjectIdAsync(planId, token);
        if (project is null) return new(SurveyV2ServiceStatus.NotFound);
        if (!await InScope(actor, role, project.Value, token)) return new(SurveyV2ServiceStatus.Forbidden);
        if (request.Sources is not { Count: > 0 and <= 100 } || request.Sources.Any(x => x is null || x.TaskId == Guid.Empty || x.DatasetId == Guid.Empty || x.AssessmentId == Guid.Empty) || request.Sources.Select(x => x.DatasetId).Distinct().Count() != request.Sources.Count || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000 || string.IsNullOrWhiteSpace(key) || key.Length > 128) return new(SurveyV2ServiceStatus.InvalidInput);
        var result = await _repository.CompletePlanAsync(new(actor, planId, expectedVersion.Trim().Trim('"'), key, Fingerprint(new { planId, request, expectedVersion }), request.Reason, request.Sources.Select(x => new SurveyPlanCompletionSourceRecord(x.TaskId, x.DatasetId, x.AssessmentId)).ToArray()), token);
        return result.Status == SurveyV2PersistenceStatus.PlanCompletionBlocked ? new(SurveyV2ServiceStatus.PlanCompletionBlocked, ReasonCode: result.ReasonCode) : result.Status == SurveyV2PersistenceStatus.ConcurrencyConflict ? new(SurveyV2ServiceStatus.ConcurrencyConflict) : result.Status == SurveyV2PersistenceStatus.Forbidden ? new(SurveyV2ServiceStatus.Forbidden) : MapPlan(result);
    }

    public async Task<SurveyV2ServiceResult> CompleteTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId,
        CompleteSurveyTaskRequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (role != UserRoleCode.ProjectManager || actorUserId == Guid.Empty) return new(SurveyV2ServiceStatus.Forbidden);
        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task is null) return new(SurveyV2ServiceStatus.NotFound);
        if (!await InScope(actorUserId, role, task.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        if (request.DatasetId == Guid.Empty || request.AssessmentId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000 ||
            string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128 || string.IsNullOrWhiteSpace(expectedVersion)) return new(SurveyV2ServiceStatus.InvalidInput);
        var result = await _repository.MutateTaskAsync(new(actorUserId, taskId, "complete", request.Reason, null, null, null,
            expectedVersion.Trim('"'), idempotencyKey, Fingerprint(new { taskId, request, expectedVersion }), correlationId, request.DatasetId, request.AssessmentId), cancellationToken);
        return result.Status switch
        {
            SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, Task: ToTask(result.Task)),
            SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, Task: ToTask(result.Task)),
            SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound),
            SurveyV2PersistenceStatus.Forbidden => new(SurveyV2ServiceStatus.Forbidden),
            SurveyV2PersistenceStatus.ConcurrencyConflict => new(SurveyV2ServiceStatus.ConcurrencyConflict),
            SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict),
            SurveyV2PersistenceStatus.InvalidInput => new(SurveyV2ServiceStatus.InvalidInput),
            _ => new(SurveyV2ServiceStatus.Conflict)
        };
    }

    public Task<SurveyV2ServiceResult> AcceptTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
        => MutateTaskAsync(actorUserId, role, taskId, "accept", null, null, null, null, idempotencyKey, expectedVersion, correlationId, cancellationToken);

    public Task<SurveyV2ServiceResult> DeclineTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, SurveyTaskReasonV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
        => MutateTaskAsync(actorUserId, role, taskId, "decline", request?.Reason, null, null, null, idempotencyKey, expectedVersion, correlationId, cancellationToken);

    public Task<SurveyV2ServiceResult> CancelTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, SurveyTaskReasonV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
        => MutateTaskAsync(actorUserId, role, taskId, "cancel", request?.Reason, null, null, null, idempotencyKey, expectedVersion, correlationId, cancellationToken);

    public Task<SurveyV2ServiceResult> ReassignTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, ReassignSurveyTaskV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
        => MutateTaskAsync(actorUserId, role, taskId, "reassign", request?.Reason, request?.OperatorId, request?.DueAt, null, idempotencyKey, expectedVersion, correlationId, cancellationToken);

    public Task<SurveyV2ServiceResult> RequestSupplementAsync(Guid actorUserId, UserRoleCode role, Guid taskId, SupplementSurveyTaskV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (request is null) return Task.FromResult(new SurveyV2ServiceResult(SurveyV2ServiceStatus.InvalidInput));
        return MutateTaskAsync(actorUserId, role, taskId, "supplement", request.Reason, request.OperatorId, null, JsonSerializer.Serialize(request.Scope), idempotencyKey, expectedVersion, correlationId, cancellationToken);
    }

    public async Task<SurveyV2ServiceResult> SubmitDatasetAsync(Guid actorUserId, UserRoleCode role, Guid taskId, SubmitDatasetRequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role != UserRoleCode.DroneOperator) return new(SurveyV2ServiceStatus.Forbidden);
        if (actorUserId == Guid.Empty || role != UserRoleCode.DroneOperator || taskId == Guid.Empty || request is null ||
            request.VideoFileIds is not { Count: > 0 } || request.TelemetryFileIds is null || request.RecordedAt == default ||
            request.DeviceId is not { } deviceId || deviceId == Guid.Empty || request.Scope is null || !ValidScope(request.Scope) ||
            string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion))
        {
            return new(SurveyV2ServiceStatus.InvalidInput);
        }

        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task is null) return new(SurveyV2ServiceStatus.NotFound);
        if (task.OperatorId != actorUserId || !task.AssignmentIsActive || !await InScope(actorUserId, role, task.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var scopeJson = JsonSerializer.Serialize(request.Scope);
        var pairsJson = JsonSerializer.Serialize(request.Pairs ?? []);
        if (request.Pairs is { } pairs && (pairs.Any(p => p is null) || pairs.Count != request.VideoFileIds.Count || pairs.Select(p => p.VideoFileId).Distinct().Count() != pairs.Count ||
            pairs.Any(p => !request.VideoFileIds.Contains(p.VideoFileId) || p.TelemetryFileId is { } telemetry && !request.TelemetryFileIds.Contains(telemetry)) ||
            pairs.Where(p => p.TelemetryFileId.HasValue).Select(p => p.TelemetryFileId).Distinct().Count() != pairs.Count(p => p.TelemetryFileId.HasValue))) return new(SurveyV2ServiceStatus.InvalidInput);
        var fingerprint = Fingerprint($"{taskId:N}|{string.Join(',', request.VideoFileIds.Order())}|{string.Join(',', request.TelemetryFileIds.Order())}|{request.RecordedAt:O}|{deviceId:N}|{scopeJson}|{expectedVersion}|{pairsJson}");
        var result = await _repository.SubmitDatasetAsync(new SurveyDatasetSubmissionRequest(
            actorUserId, taskId, request.VideoFileIds, request.TelemetryFileIds, request.RecordedAt, deviceId,
            scopeJson, expectedVersion.Trim().Trim('"'), idempotencyKey, fingerprint, correlationId, pairsJson), cancellationToken);
        if (result.ScheduleError is not null) return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: result.ScheduleError);
        return result.Status switch
        {
            SurveyDatasetPersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, Dataset: ToDataset(result.Dataset)),
            SurveyDatasetPersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, Dataset: ToDataset(result.Dataset)),
            SurveyDatasetPersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound),
            SurveyDatasetPersistenceStatus.ConcurrencyConflict => new(SurveyV2ServiceStatus.ConcurrencyConflict),
            SurveyDatasetPersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict),
            SurveyDatasetPersistenceStatus.Conflict => new(SurveyV2ServiceStatus.Conflict),
            SurveyDatasetPersistenceStatus.ScopeIncompatible => new(SurveyV2ServiceStatus.ScopeIncompatible),
            _ => new(SurveyV2ServiceStatus.InvalidInput)
        };
    }

    public async Task<(SurveyV2ServiceStatus Status, IReadOnlyList<DatasetListItemDto> Items)> ListTaskDatasetsAsync(
        Guid actor, UserRoleCode role, Guid taskId, DateTimeOffset? afterSubmittedAt, Guid? afterId, int take,
        CancellationToken cancellationToken = default)
    {
        if (actor == Guid.Empty || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator))
            return (SurveyV2ServiceStatus.Forbidden, []);
        var projectId = await _repository.GetTaskProjectIdAsync(taskId, cancellationToken);
        if (projectId is null) return (SurveyV2ServiceStatus.NotFound, []);
        if (!await InScope(actor, role, projectId.Value, cancellationToken)) return (SurveyV2ServiceStatus.Forbidden, []);

        if (role == UserRoleCode.DroneOperator)
        {
            var task = await _repository.GetTaskAsync(taskId, cancellationToken);
            if (task is null || task.ProjectId != projectId || task.OperatorId != actor || !task.AssignmentIsActive)
                return (SurveyV2ServiceStatus.Forbidden, []);
        }

        var datasets = await _repository.ListTaskDatasetsAsync(taskId, projectId.Value, afterSubmittedAt, afterId, take, cancellationToken);
        return (SurveyV2ServiceStatus.Success, datasets.Select(d => new DatasetListItemDto(
            d.Id, taskId, d.VersionNo, d.SubmittedAt, d.IntegrityStatus, d.Version, d.AssessmentId, d.ProcessingCounts)).ToArray());
    }

    public async Task<DatasetCoverageServiceResult> GetDatasetCoverageAsync(Guid actorUserId, UserRoleCode role, Guid datasetId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || datasetId == Guid.Empty || role is not (UserRoleCode.DroneOperator or UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) return new(SurveyV2ServiceStatus.Forbidden);
        var dataset = await _repository.GetDatasetAccessAsync(datasetId, cancellationToken);
        if (dataset is null) return new(SurveyV2ServiceStatus.NotFound);
        if (role == UserRoleCode.DroneOperator && dataset.OperatorId != actorUserId) return new(SurveyV2ServiceStatus.Forbidden);
        if (!await InScope(actorUserId, role, dataset.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        try
        {
            var scope = JsonSerializer.Deserialize<IReadOnlyList<BandScopeDto>>(dataset.ScopeJson, ResponseJsonOptions);
            if (scope is null || !ValidScope(scope)) return new(SurveyV2ServiceStatus.InvalidInput);
            var assessment = _assessments is null ? null : await _assessments.ReadAsync(datasetId, null, cancellationToken);
            if (assessment is not null)
            {
                var items = scope.SelectMany(s => s.SegmentIds.Select(segment =>
                {
                    var item = assessment.Items.SingleOrDefault(i => i.RouteVersionId == s.RouteVersionId && i.SegmentSetId == s.SegmentSetId && i.SegmentId == segment && i.TargetBand == s.TargetBand);
                    var position = item?.PositionStatus ?? "UNKNOWN";
                    var quality = item?.QualityStatus ?? "UNKNOWN";
                    var coverage = item?.CoverageStatus ?? "UNKNOWN";
                    var dimensions = new[] { position, quality, coverage };
                    var overall = dimensions.Contains("FAIL") ? "FAIL" : dimensions.Contains("UNKNOWN") ? "UNKNOWN" : "PASS";
                    return new DatasetCoverageItemDto(new BandScopeDto(s.RouteVersionId, s.SegmentSetId, [segment], s.TargetBand), position, quality, overall,
                        [item?.Reason ?? "assessment_scope_not_reviewed"])
                    { CoverageStatus = coverage };
                })).ToArray();
                return new(SurveyV2ServiceStatus.Success, new DatasetCoverageResponseDto(datasetId, items, assessment.MethodVersion, assessment.Id, assessment.Version));
            }
            return new(SurveyV2ServiceStatus.Success, new DatasetCoverageResponseDto(dataset.DatasetId,
                scope.Select(value => new DatasetCoverageItemDto(value, "UNKNOWN", "UNKNOWN", "UNKNOWN", ["position_evidence_not_available", "quality_evidence_not_available"])).ToArray(),
                "coverage-not-evaluated.v1"));
        }
        catch (JsonException)
        {
            return new(SurveyV2ServiceStatus.InvalidInput);
        }
    }

    private async Task<SurveyV2ServiceResult> MutateTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, string operation, string? reason, Guid? operatorId, DateTimeOffset? dueAt, string? scopeJson, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || taskId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion)) return new(SurveyV2ServiceStatus.InvalidInput);
        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task is null) return new(SurveyV2ServiceStatus.NotFound);
        var operatorOperation = operation is "accept" or "decline";
        if (operatorOperation)
        {
            // The latest declined assignment may reach its receipt, but never grants read/accept/submit authority.
            // A replacement assignment or revoked membership rejects the old actor before receipt lookup.
            var mayReplayDecline = operation == "decline" && task.AssignmentWasDeclined;
            if (role != UserRoleCode.DroneOperator || task.OperatorId != actorUserId ||
                (!task.AssignmentIsActive && !mayReplayDecline) || !await InScope(actorUserId, role, task.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        }
        else
        {
            if (role != UserRoleCode.ProjectManager || !await InScope(actorUserId, role, task.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        }

        if (operation is "decline" or "cancel" or "reassign" or "supplement" && string.IsNullOrWhiteSpace(reason)) return new(SurveyV2ServiceStatus.InvalidInput);
        if (operation is "reassign" or "supplement" && (!operatorId.HasValue || operatorId == Guid.Empty)) return new(SurveyV2ServiceStatus.InvalidInput);
        if (operation == "supplement")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(scopeJson) || !ValidScope(JsonSerializer.Deserialize<IReadOnlyList<BandScopeDto>>(scopeJson) ?? [])) return new(SurveyV2ServiceStatus.InvalidInput);
            }
            catch (JsonException)
            {
                return new(SurveyV2ServiceStatus.InvalidInput);
            }
        }

        var fingerprint = Fingerprint($"{operation}|{taskId:N}|{reason}|{operatorId:N}|{dueAt:O}|{scopeJson}|{expectedVersion}");
        var result = await _repository.MutateTaskAsync(new(actorUserId, taskId, operation, reason, operatorId, dueAt, scopeJson, expectedVersion.Trim().Trim('"'), idempotencyKey, fingerprint, correlationId), cancellationToken);
        if (result.ScheduleError is not null) return new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: result.ScheduleError);
        return result.Status switch
        {
            SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, Task: ToTask(result.Task)),
            SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, Task: ToTask(result.Task)),
            SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound),
            SurveyV2PersistenceStatus.ConcurrencyConflict => new(SurveyV2ServiceStatus.ConcurrencyConflict),
            SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict),
            SurveyV2PersistenceStatus.OperatorNotFound => new(SurveyV2ServiceStatus.OperatorNotFound),
            SurveyV2PersistenceStatus.ScopeIncompatible => new(SurveyV2ServiceStatus.ScopeIncompatible),
            SurveyV2PersistenceStatus.InvalidInput => new(SurveyV2ServiceStatus.InvalidInput),
            _ => new(SurveyV2ServiceStatus.Conflict)
        };
    }

    public async Task<(SurveyV2ServiceStatus Status, object? Value, string? Version)> ReadResourceAsync(Guid actor, UserRoleCode role, Guid id, string resource, CancellationToken token = default)
    {
        if (resource == "plan")
        {
            var plan = await _repository.ReadPlanAsync(id, token);
            if (plan is null) return (SurveyV2ServiceStatus.NotFound, null, null);
            if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || !await InScope(actor, role, plan.ProjectId, token)) return (SurveyV2ServiceStatus.Forbidden, null, null);
            return (SurveyV2ServiceStatus.Success, ToPlan(plan), plan.Version);
        }
        if (resource == "work-package")
        {
            var result = await GetTaskAsync(actor, role, id, token);
            if (result.Task is null) return (result.Status, null, null);
            var task = await _repository.GetTaskAsync(id, token);
            var point = task?.AccessPointJson is null ? null : JsonSerializer.Deserialize<PositionDto>(task.AccessPointJson, ResponseJsonOptions);
            var geometryRefs = result.Task.Scope.GroupBy(scope => (scope.RouteVersionId, scope.SegmentSetId))
                .Select(group => new SurveyGeometryRefDto(group.Key.RouteVersionId, group.Key.SegmentSetId,
                    group.SelectMany(scope => scope.SegmentIds).Distinct().Order().ToArray())).ToArray();
            return (SurveyV2ServiceStatus.Success, new SurveyTaskWorkPackage("anh01.survey-work.v1", result.Task, task?.DueAt, point, geometryRefs, result.Task.Version), result.Task.Version);
        }
        var access = await _repository.GetDatasetAccessAsync(id, token);
        if (access is null) return (SurveyV2ServiceStatus.NotFound, null, null);
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator) ||
            role == UserRoleCode.DroneOperator && access.OperatorId != actor || !await InScope(actor, role, access.ProjectId, token)) return (SurveyV2ServiceStatus.Forbidden, null, null);
        var dataset = await _repository.ReadDatasetAsync(id, token);
        if (dataset is null) return (SurveyV2ServiceStatus.NotFound, null, null);
        var files = JsonSerializer.Deserialize<DatasetSourceFileDto[]>(dataset.SourceJson, ResponseJsonOptions) ?? [];
        var scope = JsonSerializer.Deserialize<BandScopeDto[]>(dataset.ScopeJson, ResponseJsonOptions) ?? [];
        var pairs = JsonSerializer.Deserialize<DatasetPairDto[]>(dataset.PairsJson, ResponseJsonOptions) ?? [];
        return (SurveyV2ServiceStatus.Success, new DatasetDetailView(id, dataset.TaskId, dataset.ProjectId, id, dataset.SubmittedBy, dataset.SubmittedAt,
            dataset.RecordedAt, dataset.DeviceId, scope, dataset.IntegrityStatus, files.Any(f => f.Purpose == "TELEMETRY") ? "PRESENT" : "MISSING", files, pairs, dataset.Version, dataset.SurveyId), dataset.Version);
    }

    private async Task<bool> InScope(Guid actor, UserRoleCode role, Guid project, CancellationToken token) => await _scopeGuard.AuthorizeAsync(actor, role, project, token) is not null;
    private static bool IsManager(Guid id, UserRoleCode role) => id != Guid.Empty && role == UserRoleCode.ProjectManager;
    private static bool TryType(string value, out SurveyType type) { type = value?.Trim().ToUpperInvariant() switch { "BASELINE" => SurveyType.Original, "PERIODIC" => SurveyType.Periodic, "AD_HOC" => SurveyType.Supplementary, _ => SurveyType.Unknown }; return type != SurveyType.Unknown; }
    private static SurveyV2ServiceResult MapPlan(SurveyV2PlanPersistenceResult result) => result.Status switch { SurveyV2PersistenceStatus.Forbidden => new(SurveyV2ServiceStatus.Forbidden), SurveyV2PersistenceStatus.ActivePlanConflict => new(SurveyV2ServiceStatus.ActivePlanConflict, ExistingPlanId: result.ExistingPlanId, ExistingPlanStatus: result.ExistingPlanStatus), SurveyV2PersistenceStatus.SourceScopeConflict => new(SurveyV2ServiceStatus.SourceScopeConflict), SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, ToPlan(result.Plan)), SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, ToPlan(result.Plan)), SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound), SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict), SurveyV2PersistenceStatus.Conflict => new(SurveyV2ServiceStatus.Conflict), _ => new(SurveyV2ServiceStatus.InvalidInput, ScheduleError: result.ScheduleError) };
    private static SurveyPlanV2ResponseDto? ToPlan(SurveyV2PlanPersistenceView? value)
    {
        if (value is null) return null;
        try
        {
            var occurrences = value.Occurrences?.Select(item => new SurveyPlanOccurrenceDto(
                item.Id, item.Start, item.End, ToApiStatus(item.Status), item.SurveyPlanId)).ToArray();
            return new(value.Id, value.ProjectId, JsonSerializer.Deserialize<IReadOnlyList<BandScopeDto>>(value.ScopeJson, ResponseJsonOptions) ?? [],
                value.PlannedAt, ToApiStatus(value.Status), value.Version, value.SurveyType,
                value.Completion is null ? null : new(value.Completion.Id, value.Completion.At, value.Completion.ActorId,
                    value.Completion.Reason, value.Completion.Sources.Select(x => new SurveyPlanCompletionSource(x.TaskId, x.DatasetId, x.AssessmentId)).ToArray()),
                value.WarrantyId, occurrences?.Length, occurrences?.FirstOrDefault()?.PlannedStartAt,
                occurrences?.LastOrDefault()?.PlannedStartAt, occurrences);
        }
        catch (JsonException) { return null; }
    }
    private static SurveyTaskV2ResponseDto? ToTask(SurveyV2TaskPersistenceView? value)
    {
        if (value is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(value.ScopeJson);
            var scopeElement = doc.RootElement.ValueKind == JsonValueKind.Object &&
                               doc.RootElement.TryGetProperty("scope", out var wrapped)
                ? wrapped
                : doc.RootElement;
            var scope = scopeElement.Deserialize<IReadOnlyList<BandScopeDto>>(ResponseJsonOptions) ?? [];
            return new(value.Id, value.ProjectId, scope, value.OperatorId, ToApiStatus(value.Status), value.Version, value.SupplementTaskId, value.DueAt, value.ProjectName, value.ParentTaskId, value.SurveyType);
        }
        catch (JsonException) { return null; }
    }
    private static DatasetResponseDto? ToDataset(SurveyDatasetPersistenceView? value)
        => value is null ? null : new(value.Id, value.SurveyTaskId, value.DataVersionId, value.IntegrityStatus, value.TelemetryStatus, value.Version);
    private static bool ValidScope(IReadOnlyList<BandScopeDto> scope)
        => scope.Count > 0 && scope.Select(i => (i.RouteVersionId, i.SegmentSetId, i.TargetBand)).Distinct().Count() == scope.Count &&
           scope.All(item => item.RouteVersionId != Guid.Empty && item.SegmentSetId != Guid.Empty && item.SegmentIds is { Count: > 0 } &&
           item.SegmentIds.Distinct().Count() == item.SegmentIds.Count && item.SegmentIds.All(id => id != Guid.Empty) && item.TargetBand is "SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE");
    private static bool ValidPosition(PositionDto? position)
        => position is null ||
           (position.Point is not null &&
            (position.Source is "EXIF" or "MANUAL" or "SURVEY" or "DERIVED") &&
            (position.AccuracyMeters is null or >= 0));
    private static string ToApiStatus(string status)
        => status switch
        {
            "NewAssigned" => "NEW_ASSIGNED",
            "InProgress" => "IN_PROGRESS",
            "SupplementRequired" => "SUPPLEMENT_REQUIRED",
            "TaskCreated" => "TASK_CREATED",
            _ => status.ToUpperInvariant()
        };
    private static string Fingerprint<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, ResponseJsonOptions))).ToLowerInvariant();
}
