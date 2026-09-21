namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyRequestCreationPersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid RoadSectionId,
    Guid? SurveyPlanId,
    RoadGuardSystem.aBusinessObjects.Commons.SurveyType SurveyType,
    DateTimeOffset DueAt,
    string OutputRequirements,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid OperationId,
    Guid? CorrelationId);
