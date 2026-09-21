using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateHandoverRequestDto(
    [Required, MaxLength(80)] string DocumentNo,
    [Required] DateOnly? HandoverDate,
    Guid? FileId,
    string? Notes);
