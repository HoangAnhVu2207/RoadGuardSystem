using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed record SurveyPlanPostponementServiceResult(
    SurveyPlanningServiceStatus Status,
    SurveyPlanPostponementPersistenceView? Postponement = null,
    SurveyScheduleError? ScheduleError = null);
