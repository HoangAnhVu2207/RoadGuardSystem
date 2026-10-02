namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDatasetPersistenceView(
    Guid Id,
    Guid SurveyTaskId,
    Guid ProjectId,
    Guid DataVersionId,
    string IntegrityStatus,
    string TelemetryStatus,
    string Version,
    string ScopeJson);
