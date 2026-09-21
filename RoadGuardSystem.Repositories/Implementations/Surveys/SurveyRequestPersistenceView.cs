using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyRequestPersistenceView(
    Guid RequestId,
    Guid ProjectId,
    Guid RoadSectionId,
    Guid? SurveyPlanId,
    Guid RequestedByUserId,
    SurveyType SurveyType,
    SurveyRequestStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset DueAt,
    string OutputRequirements);
