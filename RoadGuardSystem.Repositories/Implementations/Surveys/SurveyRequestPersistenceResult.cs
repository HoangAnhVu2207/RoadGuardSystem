using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyRequestPersistenceResult(
    SurveyRequestPersistenceStatus Status,
    SurveyRequestPersistenceView? Request = null,
    SurveyScheduleError? ScheduleError = null);
