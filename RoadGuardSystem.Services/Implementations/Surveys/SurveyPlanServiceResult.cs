using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed record SurveyPlanServiceResult(
    SurveyPlanningServiceStatus Status,
    SurveyPlanPersistenceView? Plan = null,
    SurveyScheduleError? ScheduleError = null);
