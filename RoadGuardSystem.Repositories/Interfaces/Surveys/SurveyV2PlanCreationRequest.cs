using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2PlanCreationRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid RouteVersionId,
    DateTimeOffset PlannedAt,
    SurveyType SurveyType,
    string ScopeJson,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid? CorrelationId,
    IReadOnlyList<SurveyV2ScopeRequest>? Scope = null);
