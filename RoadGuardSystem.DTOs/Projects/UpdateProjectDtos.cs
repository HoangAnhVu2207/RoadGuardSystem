using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record UpdateProjectRequestDto(
    [Required, MaxLength(255)] string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    [Required] string ExpectedRowVersion,
    Guid OperationId);

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
