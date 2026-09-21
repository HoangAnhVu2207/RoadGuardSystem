namespace RoadGuardSystem.DTOs.Warranties;

public sealed record CreateWarrantyRequestDto(
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    string Scope,
    string? Terms,
    Guid? SourceDocumentId,
    string Status,
    Guid OperationId);
