namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyPlanPersistenceResult(
    SurveyPlanPersistenceStatus Status,
    SurveyPlanPersistenceView? Plan = null);
