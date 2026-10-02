namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanPostponementPersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid SurveyPlanId,
    DateTimeOffset? NewPlannedStartAt,
    string Reason,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid OperationId,
    Guid? CorrelationId);
