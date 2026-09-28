using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Surveys;
public sealed record SurveyV2TaskCreationRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid RouteVersionId,
    Guid OperatorId,
    SurveyType SurveyType,
    DateTimeOffset? DueAt,
    string ScopeJson,
    string? AccessPointJson,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid? CorrelationId,
    IReadOnlyList<SurveyV2ScopeRequest>? Scope = null);
