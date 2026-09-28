namespace RoadGuardSystem.DTOs.Inspections;

public sealed record InspectionTaskPageResponseDto(
    IReadOnlyList<InspectionTaskResponseDto> Items,
    string? NextCursor,
    DateTimeOffset AsOf);
