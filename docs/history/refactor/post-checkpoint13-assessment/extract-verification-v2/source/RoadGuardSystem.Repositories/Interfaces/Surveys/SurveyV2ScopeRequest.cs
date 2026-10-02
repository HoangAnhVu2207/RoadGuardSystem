namespace RoadGuardSystem.Repositories.Surveys;

public sealed record SurveyV2ScopeRequest(
    Guid RouteVersionId,
    Guid SegmentSetId,
    string SegmentIdsJson,
    string TargetBand);
