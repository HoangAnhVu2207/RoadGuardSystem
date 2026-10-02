using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Warranties;

public sealed class Warranty
{
    private Warranty()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid? RoadSectionId { get; private set; }

    public Guid? HandoverDocumentId { get; private set; }

    public DateOnly HandoverDate { get; private set; }

    public DateOnly WarrantyStartDate { get; private set; }

    public DateOnly WarrantyEndDate { get; private set; }

    public decimal? RetainedValue { get; private set; }

    public WarrantyScope Scope { get; private set; }

    public string? Terms { get; private set; }

    public Guid? SourceDocumentId { get; private set; }

    public WarrantyStatus Status { get; private set; }

    public static Warranty Create(
        Guid id,
        Guid projectId,
        Guid? roadSectionId,
        Guid? handoverDocumentId,
        DateOnly handoverDate,
        DateOnly warrantyStartDate,
        DateOnly warrantyEndDate,
        decimal? retainedValue,
        WarrantyScope scope,
        string? terms,
        Guid? sourceDocumentId,
        WarrantyStatus status)
    {
        if (id == Guid.Empty || projectId == Guid.Empty)
        {
            throw new ArgumentException("Warranty and project ids must not be empty.");
        }

        if (!Enum.IsDefined(scope) || scope == WarrantyScope.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(scope), "Warranty scope is invalid.");
        }

        if (!Enum.IsDefined(status) || status == WarrantyStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Warranty status is invalid.");
        }

        if (warrantyEndDate < warrantyStartDate)
        {
            throw new ArgumentException("Warranty end date must not be before its start date.", nameof(warrantyEndDate));
        }

        if (retainedValue is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retainedValue), "Retained value cannot be negative.");
        }

        if (scope == WarrantyScope.Project && roadSectionId is not null)
        {
            throw new ArgumentException("Project-wide warranty cannot target a road section.", nameof(roadSectionId));
        }

        if (scope == WarrantyScope.RoadSection && roadSectionId is null)
        {
            throw new ArgumentException("Road-section warranty requires a road section.", nameof(roadSectionId));
        }

        return new Warranty
        {
            Id = id,
            ProjectId = projectId,
            RoadSectionId = roadSectionId,
            HandoverDocumentId = handoverDocumentId,
            HandoverDate = handoverDate,
            WarrantyStartDate = warrantyStartDate,
            WarrantyEndDate = warrantyEndDate,
            RetainedValue = retainedValue,
            Scope = scope,
            Terms = string.IsNullOrWhiteSpace(terms) ? null : terms.Trim(),
            SourceDocumentId = sourceDocumentId,
            Status = status
        };
    }
}
