using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDatasetPersistenceResult(
    SurveyDatasetPersistenceStatus Status,
    SurveyDatasetPersistenceView? Dataset = null,
    SurveyScheduleError? ScheduleError = null);
