namespace RoadGuardSystem.DTOs.Surveys;

public sealed record SurveyTaskPageV2ResponseDto(
    IReadOnlyList<SurveyTaskV2ResponseDto> Items,
    string? NextCursor,
    DateTimeOffset AsOf);
