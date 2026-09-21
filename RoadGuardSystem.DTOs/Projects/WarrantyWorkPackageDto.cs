namespace RoadGuardSystem.DTOs.Projects;

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
