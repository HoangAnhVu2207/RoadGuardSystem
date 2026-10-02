namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2TaskPersistenceResult(SurveyV2PersistenceStatus Status, SurveyV2TaskPersistenceView? Task = null);
