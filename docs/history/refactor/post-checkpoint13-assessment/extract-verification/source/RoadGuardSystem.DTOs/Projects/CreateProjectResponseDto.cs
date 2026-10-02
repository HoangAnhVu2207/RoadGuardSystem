namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateProjectResponseDto(
    Guid Id,
    string Code,
    string Name,
    Guid PrimaryPmId,
    DateOnly HandoverDate,
    DateOnly WarrantyEndDate,
    string Status,
    string Version,
    Guid ProjectId,
    string ProjectCode,
    Guid PrimaryProjectManagerUserId,
    Guid HandoverDocumentId,
    string RowVersion);
