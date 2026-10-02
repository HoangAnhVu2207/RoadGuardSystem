using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateProjectRequestDto(
    [MaxLength(50)] string? Code,
    [Required, MaxLength(255)] string Name,
    Guid PrimaryPmId,
    DateOnly? HandoverDate,
    DateOnly? WarrantyEndDate,
    IReadOnlyList<Guid>? HandoverFileIds,
    string? ProjectCode = null,
    string? Description = null,
    int? EngineeringUtmSrid = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    Guid PrimaryProjectManagerUserId = default,
    CreateHandoverRequestDto? Handover = null,
    Guid OperationId = default);
