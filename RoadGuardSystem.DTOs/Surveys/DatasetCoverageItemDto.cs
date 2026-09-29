namespace RoadGuardSystem.DTOs.Surveys;

public sealed record DatasetCoverageItemDto(
    BandScopeDto Scope,
    string PositionCoverage,
    string QualityCoverage,
    string OverallCoverage,
    IReadOnlyList<string> Reasons);
