using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Surveys;

public sealed class SurveyPlanningService : ISurveyPlanningService
{
    private readonly ISurveyPlanningRepository _repository;
    private readonly IProjectScopeGuard _scopeGuard;
    private readonly TimeProvider _timeProvider;

    public SurveyPlanningService(
        ISurveyPlanningRepository repository,
        IProjectScopeGuard scopeGuard,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _scopeGuard = scopeGuard;
        _timeProvider = timeProvider;
    }

    public async Task<SurveyPlanServiceResult> CreatePlanAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateSurveyPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsProjectManager(actorUserId, actorRole) ||
            command.ProjectId == Guid.Empty || command.RoadSectionId == Guid.Empty ||
            command.RoadSectionVersionId == Guid.Empty ||
            command.OperationId == Guid.Empty || command.PlannedEndAt < command.PlannedStartAt ||
            command.SurveyType == SurveyType.Unknown || string.IsNullOrWhiteSpace(command.OutputRequirements))
        {
            return new(SurveyPlanningServiceStatus.InvalidInput);
        }

        if (await _scopeGuard.AuthorizeAsync(actorUserId, actorRole, command.ProjectId, cancellationToken) is null)
        {
            return new(SurveyPlanningServiceStatus.Forbidden);
        }

        var result = await _repository.CreatePlanAsync(
            new SurveyPlanCreationPersistenceRequest(
                actorUserId,
                command.ProjectId,
                command.RoadSectionId,
                command.RoadSectionVersionId,
                command.PlannedStartAt,
                command.PlannedEndAt,
                command.SurveyType,
                command.OutputRequirements,
                command.OperationId.ToString("N"),
                Fingerprint(new
                {
                    command.ProjectId,
                    command.RoadSectionId,
                    command.RoadSectionVersionId,
                    command.PlannedStartAt,
                    command.PlannedEndAt,
                    command.SurveyType,
                    command.OutputRequirements
                }),
                command.OperationId,
                command.CorrelationId),
            cancellationToken);
        return new(MapStatus(result.Status), result.Plan);
    }

    public async Task<SurveyRequestServiceResult> CreateRequestAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        CreateSurveyRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsProjectManager(actorUserId, actorRole) ||
            command.ProjectId == Guid.Empty || command.RoadSectionId == Guid.Empty ||
            command.RoadSectionVersionId == Guid.Empty ||
            command.OperationId == Guid.Empty || command.SurveyType == SurveyType.Unknown ||
            string.IsNullOrWhiteSpace(command.OutputRequirements))
        {
            return new(SurveyPlanningServiceStatus.InvalidInput);
        }

        if (await _scopeGuard.AuthorizeAsync(actorUserId, actorRole, command.ProjectId, cancellationToken) is null)
        {
            return new(SurveyPlanningServiceStatus.Forbidden);
        }

        var dueAt = command.DueAt ?? _timeProvider.GetUtcNow();
        var result = await _repository.CreateRequestAsync(
            new SurveyRequestCreationPersistenceRequest(
                actorUserId,
                command.ProjectId,
                command.RoadSectionId,
                command.RoadSectionVersionId,
                command.SurveyPlanId,
                command.SurveyType,
                dueAt,
                command.OutputRequirements,
                command.OperationId.ToString("N"),
                Fingerprint(new
                {
                    command.ProjectId,
                    command.RoadSectionId,
                    command.RoadSectionVersionId,
                    command.SurveyPlanId,
                    command.SurveyType,
                    command.DueAt,
                    command.OutputRequirements,
                }),
                command.OperationId,
                command.CorrelationId),
            cancellationToken);
        return new(MapStatus(result.Status), result.Request);
    }

    public async Task<SurveyPlanPostponementServiceResult> PostponePlanAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        PostponeSurveyPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsProjectManager(actorUserId, actorRole) ||
            command.ProjectId == Guid.Empty || command.SurveyPlanId == Guid.Empty ||
            command.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason))
        {
            return new(SurveyPlanningServiceStatus.InvalidInput);
        }

        if (await _scopeGuard.AuthorizeAsync(actorUserId, actorRole, command.ProjectId, cancellationToken) is null)
        {
            return new(SurveyPlanningServiceStatus.Forbidden);
        }

        var result = await _repository.PostponePlanAsync(
            new SurveyPlanPostponementPersistenceRequest(
                actorUserId,
                command.ProjectId,
                command.SurveyPlanId,
                command.NewPlannedStartAt,
                command.Reason,
                command.OperationId.ToString("N"),
                Fingerprint(new
                {
                    command.ProjectId,
                    command.SurveyPlanId,
                    command.NewPlannedStartAt,
                    command.Reason
                }),
                command.OperationId,
                command.CorrelationId),
            cancellationToken);
        return new(MapStatus(result.Status), result.Postponement);
    }

    private static bool IsProjectManager(Guid actorUserId, UserRoleCode actorRole)
        => actorUserId != Guid.Empty && actorRole == UserRoleCode.ProjectManager;

    private static SurveyPlanningServiceStatus MapStatus(SurveyPlanPersistenceStatus status) => status switch
    {
        SurveyPlanPersistenceStatus.Success => SurveyPlanningServiceStatus.Success,
        SurveyPlanPersistenceStatus.Replayed => SurveyPlanningServiceStatus.Replayed,
        SurveyPlanPersistenceStatus.NotFound => SurveyPlanningServiceStatus.NotFound,
        SurveyPlanPersistenceStatus.ScopeConflict => SurveyPlanningServiceStatus.Forbidden,
        SurveyPlanPersistenceStatus.Conflict => SurveyPlanningServiceStatus.Conflict,
        SurveyPlanPersistenceStatus.IdempotentConflict => SurveyPlanningServiceStatus.IdempotentConflict,
        _ => SurveyPlanningServiceStatus.InvalidInput
    };

    private static SurveyPlanningServiceStatus MapStatus(SurveyRequestPersistenceStatus status) => status switch
    {
        SurveyRequestPersistenceStatus.Success => SurveyPlanningServiceStatus.Success,
        SurveyRequestPersistenceStatus.Replayed => SurveyPlanningServiceStatus.Replayed,
        SurveyRequestPersistenceStatus.NotFound => SurveyPlanningServiceStatus.NotFound,
        SurveyRequestPersistenceStatus.ScopeConflict => SurveyPlanningServiceStatus.Forbidden,
        SurveyRequestPersistenceStatus.Conflict => SurveyPlanningServiceStatus.Conflict,
        SurveyRequestPersistenceStatus.IdempotentConflict => SurveyPlanningServiceStatus.IdempotentConflict,
        _ => SurveyPlanningServiceStatus.InvalidInput
    };

    private static SurveyPlanningServiceStatus MapStatus(SurveyPlanPostponementPersistenceStatus status) => status switch
    {
        SurveyPlanPostponementPersistenceStatus.Success => SurveyPlanningServiceStatus.Success,
        SurveyPlanPostponementPersistenceStatus.Replayed => SurveyPlanningServiceStatus.Replayed,
        SurveyPlanPostponementPersistenceStatus.NotFound => SurveyPlanningServiceStatus.NotFound,
        SurveyPlanPostponementPersistenceStatus.ScopeConflict => SurveyPlanningServiceStatus.Forbidden,
        SurveyPlanPostponementPersistenceStatus.Conflict => SurveyPlanningServiceStatus.Conflict,
        SurveyPlanPostponementPersistenceStatus.IdempotentConflict => SurveyPlanningServiceStatus.IdempotentConflict,
        _ => SurveyPlanningServiceStatus.InvalidInput
    };

    private static string Fingerprint<T>(T command)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command)))).ToLowerInvariant();
}
