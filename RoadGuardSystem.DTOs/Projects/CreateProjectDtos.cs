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

public sealed record CreateHandoverRequestDto(
    [Required, MaxLength(80)] string DocumentNo,
    [Required] DateOnly? HandoverDate,
    Guid? FileId,
    string? Notes);

public sealed record CreateProjectResponseDto(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string Status,
    Guid PrimaryProjectManagerUserId,
    Guid HandoverDocumentId,
    DateOnly HandoverDate,
    string RowVersion);
