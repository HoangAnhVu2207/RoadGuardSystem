namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2PlanPersistenceResult(SurveyV2PersistenceStatus Status, SurveyV2PlanPersistenceView? Plan = null);
