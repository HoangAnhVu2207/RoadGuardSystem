namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SurveyPlanV2ResponseDto(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<BandScopeDto> Scope,
    DateTimeOffset PlannedAt,
    string Status,
    string Version);
