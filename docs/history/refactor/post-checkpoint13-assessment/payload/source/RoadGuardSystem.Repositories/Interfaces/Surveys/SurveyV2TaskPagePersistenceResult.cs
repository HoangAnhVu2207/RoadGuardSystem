namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2TaskPagePersistenceResult(
    IReadOnlyList<SurveyV2TaskPersistenceView> Items,
    string? NextCursor,
    DateTimeOffset AsOf);
