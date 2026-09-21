namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanCreationPersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid RoadSectionId,
    DateTimeOffset PlannedStartAt,
    DateTimeOffset PlannedEndAt,
    RoadGuardSystem.aBusinessObjects.Commons.SurveyType SurveyType,
    string OutputRequirements,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid OperationId,
    Guid? CorrelationId);
