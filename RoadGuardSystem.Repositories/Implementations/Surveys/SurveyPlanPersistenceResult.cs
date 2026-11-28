using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanPersistenceResult(
    SurveyPlanPersistenceStatus Status,
    SurveyPlanPersistenceView? Plan = null,
    SurveyScheduleError? ScheduleError = null);
