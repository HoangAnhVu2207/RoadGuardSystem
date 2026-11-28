using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;

namespace RoadGuardSystem.Services.Surveys;

public sealed record SurveyRequestServiceResult(
    SurveyPlanningServiceStatus Status,
    SurveyRequestPersistenceView? Request = null,
    SurveyScheduleError? ScheduleError = null);
