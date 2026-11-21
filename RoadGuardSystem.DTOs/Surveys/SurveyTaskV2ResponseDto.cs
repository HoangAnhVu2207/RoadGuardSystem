namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SurveyTaskV2ResponseDto(
    Guid Id,
    Guid ProjectId,
    IReadOnlyList<BandScopeDto> Scope,
    Guid OperatorId,
    string Status,
    string Version,
    Guid? SupplementTaskId = null, DateTimeOffset? DueAt = null, string? ProjectName = null, Guid? ParentTaskId = null);
