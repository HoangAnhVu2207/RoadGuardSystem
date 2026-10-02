namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDatasetPersistenceResult(
    SurveyDatasetPersistenceStatus Status,
    SurveyDatasetPersistenceView? Dataset = null);
