namespace RoadGuardSystem.DTOs.Projects;

public sealed record UpdateProjectResponseDto(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    string RowVersion);
