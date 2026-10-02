using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanPersistenceView(
    Guid PlanId,
    Guid ProjectId,
    Guid RoadSectionId,
    Guid? RoadSectionVersionId,
    DateTimeOffset PlannedStartAt,
    DateTimeOffset PlannedEndAt,
    SurveyType SurveyType,
    SurveyPlanStatus Status,
    string OutputRequirements);
