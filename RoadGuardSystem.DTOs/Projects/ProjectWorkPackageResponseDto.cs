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

public sealed record RoadSectionWorkPackageDto(
    Guid RoadSectionId,
    string Code,
    string? Name,
    Guid CurrentVersionId,
    int VersionNo,
    string GeometryWkt,
    int Srid,
    DateTimeOffset EffectiveFrom,
    string ChangeReason);

public sealed record WarrantyWorkPackageDto(
    Guid WarrantyId,
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    string Scope,
    string? Terms,
    Guid? SourceDocumentId,
    string Status);
