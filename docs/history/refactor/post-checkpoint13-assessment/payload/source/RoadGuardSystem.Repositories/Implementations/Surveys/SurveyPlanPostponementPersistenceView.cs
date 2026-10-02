using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanPostponementPersistenceView(
    Guid PlanId,
    SurveyPlanStatus Status,
    DateTimeOffset? NewPlannedStartAt,
    Guid PostponementId);
