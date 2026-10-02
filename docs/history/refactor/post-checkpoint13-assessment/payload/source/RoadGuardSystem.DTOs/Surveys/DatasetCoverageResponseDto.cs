namespace RoadGuardSystem.DTOs.Surveys;

public sealed record DatasetCoverageResponseDto(
    Guid DatasetId,
    IReadOnlyList<DatasetCoverageItemDto> Items,
    string MethodVersion);
