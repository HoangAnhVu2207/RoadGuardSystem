namespace RoadGuardSystem.DTOs.Projects;

public sealed record CreateProjectResponseDto(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string Status,
    Guid PrimaryProjectManagerUserId,
    Guid HandoverDocumentId,
    DateOnly HandoverDate,
    string RowVersion);
