namespace RoadGuardSystem.DTOs.Projects;

public sealed record ProjectListItemResponseDto(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string Status,
    Guid? PrimaryProjectManagerUserId,
    string? PrimaryProjectManagerName);

public sealed record ProjectListResponseDto(
    IReadOnlyList<ProjectListItemResponseDto> Items,
    string? NextCursor,
    DateTimeOffset AsOf);
