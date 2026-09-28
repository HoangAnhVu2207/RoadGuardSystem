namespace RoadGuardSystem.Repositories.Surveys;
public sealed record SurveyV2PlanPostponementRequest(Guid ActorUserId, Guid PlanId, string Reason, string ExpectedVersion, string IdempotencyKey, string RequestFingerprint, Guid? CorrelationId);
