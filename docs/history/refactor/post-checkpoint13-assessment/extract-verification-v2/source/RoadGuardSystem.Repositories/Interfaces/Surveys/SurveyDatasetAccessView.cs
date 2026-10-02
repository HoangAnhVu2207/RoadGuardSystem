namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyDatasetAccessView(
    Guid DatasetId,
    Guid SurveyTaskId,
    Guid ProjectId,
    Guid OperatorId,
    string ScopeJson,
    string Version);
