using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanPostponementPersistenceResult(
    SurveyPlanPostponementPersistenceStatus Status,
    SurveyPlanPostponementPersistenceView? Postponement = null,
    SurveyScheduleError? ScheduleError = null);
