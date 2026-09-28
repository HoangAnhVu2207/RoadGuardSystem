using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    public SurveyV2Service(ISurveyV2Repository repository, IProjectScopeGuard scopeGuard) { _repository = repository; _scopeGuard = scopeGuard; }

    public async Task<SurveyV2ServiceResult> CreatePlanAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateSurveyPlanV2RequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (!IsManager(actorUserId, role) || projectId == Guid.Empty || request.Scope is null || request.Scope.Count == 0 || string.IsNullOrWhiteSpace(idempotencyKey) || !TryType(request.SurveyType, out var type) || request.PlannedAt == default || !ValidScope(request.Scope)) return new(SurveyV2ServiceStatus.InvalidInput);
        if (!await InScope(actorUserId, role, projectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var scope = JsonSerializer.Serialize(request.Scope);
        var scopeItems = request.Scope.Select(item => new SurveyV2ScopeRequest(
            item.RouteVersionId,
            item.SegmentSetId,
            JsonSerializer.Serialize(item.SegmentIds),
            item.TargetBand)).ToArray();
        return MapPlan(await _repository.CreatePlanAsync(new(actorUserId, projectId, request.Scope[0].RouteVersionId, request.PlannedAt, type, scope, idempotencyKey, Fingerprint(scope + request.PlannedAt + request.SurveyType), correlationId, scopeItems), cancellationToken));
    }

    public async Task<SurveyV2ServiceResult> PostponePlanAsync(Guid actorUserId, UserRoleCode role, Guid planId, PostponeSurveyPlanV2RequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (!IsManager(actorUserId, role) || planId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion)) return new(SurveyV2ServiceStatus.InvalidInput);
        var projectId = await _repository.GetPlanProjectIdAsync(planId, cancellationToken);
        if (projectId is null) return new(SurveyV2ServiceStatus.NotFound);
        if (!await InScope(actorUserId, role, projectId.Value, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var result = await _repository.PostponePlanAsync(new(actorUserId, planId, request.Reason, expectedVersion.Trim().Trim('"'), idempotencyKey, Fingerprint(planId + request.Reason + expectedVersion), correlationId), cancellationToken);
        return result.Status switch { SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, ToPlan(result.Plan)), SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, ToPlan(result.Plan)), SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound), SurveyV2PersistenceStatus.ConcurrencyConflict => new(SurveyV2ServiceStatus.ConcurrencyConflict), SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict), _ => new(SurveyV2ServiceStatus.Conflict) };
    }

    public async Task<SurveyV2ServiceResult> CreateTaskAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateSurveyTaskV2RequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (!IsManager(actorUserId, role) || projectId == Guid.Empty || request.Scope is null || request.Scope.Count == 0 || request.OperatorId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey) || !TryType(request.SurveyType, out var type) || !ValidScope(request.Scope) || !ValidPosition(request.AccessPoint)) return new(SurveyV2ServiceStatus.InvalidInput);
        if (!await InScope(actorUserId, role, projectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        var scope = JsonSerializer.Serialize(request.Scope);
        var accessPointJson = request.AccessPoint is null ? null : JsonSerializer.Serialize(request.AccessPoint);
        var scopeItems = request.Scope.Select(item => new SurveyV2ScopeRequest(
            item.RouteVersionId,
            item.SegmentSetId,
            JsonSerializer.Serialize(item.SegmentIds),
            item.TargetBand)).ToArray();
        var result = await _repository.CreateTaskAsync(new(actorUserId, projectId, request.Scope[0].RouteVersionId, request.OperatorId, type, request.DueAt, scope, accessPointJson, idempotencyKey, Fingerprint($"{scope}|{request.OperatorId:N}|{request.SurveyType}|{request.DueAt:O}|{accessPointJson}"), correlationId, scopeItems), cancellationToken);
        return result.Status switch { SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, Task: ToTask(result.Task)), SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, Task: ToTask(result.Task)), SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound), SurveyV2PersistenceStatus.OperatorNotFound => new(SurveyV2ServiceStatus.OperatorNotFound), SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict), _ => new(SurveyV2ServiceStatus.Conflict) };
    }

    public async Task<SurveyV2ServiceResult> GetTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || taskId == Guid.Empty || role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor or UserRoleCode.DroneOperator)) return new(SurveyV2ServiceStatus.Forbidden);
        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task is null) return new(SurveyV2ServiceStatus.NotFound);
        if (role == UserRoleCode.DroneOperator && task.OperatorId != actorUserId) return new(SurveyV2ServiceStatus.Forbidden);
        if (role != UserRoleCode.DroneOperator && !await InScope(actorUserId, role, task.ProjectId, cancellationToken)) return new(SurveyV2ServiceStatus.Forbidden);
        return new(SurveyV2ServiceStatus.Success, Task: ToTask(task));
    }

    public async Task<SurveyTaskPageV2ResponseDto?> ListMyTasksAsync(Guid actorUserId, UserRoleCode role, string? cursor, int? limit, CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || role != UserRoleCode.DroneOperator) return null;
        var pageSize = limit ?? 50;
        if (pageSize is < 1 or > 100) return null;
        try
        {
            var page = await _repository.ListMyTasksAsync(actorUserId, cursor, pageSize, cancellationToken);
            return new(page.Items.Select(item => ToTask(item)!).ToArray(), page.NextCursor, page.AsOf);
        }
        catch (ArgumentException)
        {
            return null;
        }
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

    private async Task<SurveyV2ServiceResult> MutateTaskAsync(Guid actorUserId, UserRoleCode role, Guid taskId, string operation, string? reason, Guid? operatorId, DateTimeOffset? dueAt, string? scopeJson, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || taskId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion)) return new(SurveyV2ServiceStatus.InvalidInput);
        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task is null) return new(SurveyV2ServiceStatus.NotFound);
        var operatorOperation = operation is "accept" or "decline";
        if (operatorOperation)
        {
            if (role != UserRoleCode.DroneOperator || task.OperatorId != actorUserId) return new(SurveyV2ServiceStatus.Forbidden);
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
        return result.Status switch
        {
            SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, Task: ToTask(result.Task)),
            SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, Task: ToTask(result.Task)),
            SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound),
            SurveyV2PersistenceStatus.ConcurrencyConflict => new(SurveyV2ServiceStatus.ConcurrencyConflict),
            SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict),
            SurveyV2PersistenceStatus.OperatorNotFound => new(SurveyV2ServiceStatus.OperatorNotFound),
            SurveyV2PersistenceStatus.InvalidInput => new(SurveyV2ServiceStatus.InvalidInput),
            _ => new(SurveyV2ServiceStatus.Conflict)
        };
    }

    private async Task<bool> InScope(Guid actor, UserRoleCode role, Guid project, CancellationToken token) => await _scopeGuard.AuthorizeAsync(actor, role, project, token) is not null;
    private static bool IsManager(Guid id, UserRoleCode role) => id != Guid.Empty && role == UserRoleCode.ProjectManager;
    private static bool TryType(string value, out SurveyType type) { type = value?.Trim().ToUpperInvariant() switch { "BASELINE" => SurveyType.Original, "PERIODIC" => SurveyType.Periodic, "AD_HOC" => SurveyType.Supplementary, _ => SurveyType.Unknown }; return type != SurveyType.Unknown; }
    private static SurveyV2ServiceResult MapPlan(SurveyV2PlanPersistenceResult result) => result.Status switch { SurveyV2PersistenceStatus.Success => new(SurveyV2ServiceStatus.Success, ToPlan(result.Plan)), SurveyV2PersistenceStatus.Replayed => new(SurveyV2ServiceStatus.Replayed, ToPlan(result.Plan)), SurveyV2PersistenceStatus.NotFound => new(SurveyV2ServiceStatus.NotFound), SurveyV2PersistenceStatus.IdempotentConflict => new(SurveyV2ServiceStatus.IdempotentConflict), SurveyV2PersistenceStatus.Conflict => new(SurveyV2ServiceStatus.Conflict), _ => new(SurveyV2ServiceStatus.InvalidInput) };
    private static SurveyPlanV2ResponseDto? ToPlan(SurveyV2PlanPersistenceView? value)
    {
        if (value is null) return null;
        try
        {
            return new(value.Id, value.ProjectId, JsonSerializer.Deserialize<IReadOnlyList<BandScopeDto>>(value.ScopeJson, ResponseJsonOptions) ?? [], value.PlannedAt, ToApiStatus(value.Status), value.Version);
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
            return new(value.Id, value.ProjectId, scope, value.OperatorId, ToApiStatus(value.Status), value.Version);
        }
        catch (JsonException) { return null; }
    }
    private static bool ValidScope(IReadOnlyList<BandScopeDto> scope)
        => scope.All(item => item.RouteVersionId != Guid.Empty && item.SegmentSetId != Guid.Empty && item.SegmentIds is { Count: > 0 } && item.SegmentIds.All(id => id != Guid.Empty) && item.TargetBand is "SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE");
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
    private static string Fingerprint(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
}
