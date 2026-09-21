using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateProjectRequestDto(
    [Required, MaxLength(50)] string ProjectCode,
    [Required, MaxLength(255)] string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid PrimaryProjectManagerUserId,
    [Required] CreateHandoverRequestDto? Handover,
    Guid OperationId);
