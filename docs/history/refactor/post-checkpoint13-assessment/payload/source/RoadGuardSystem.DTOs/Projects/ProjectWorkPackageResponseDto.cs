namespace RoadGuardSystem.DTOs.Projects;

public sealed record ProjectWorkPackageResponseDto(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string AccessRole,
    string RowVersion,
    IReadOnlyList<RoadSectionWorkPackageDto> RoadSections,
    IReadOnlyList<WarrantyWorkPackageDto> Warranties);
