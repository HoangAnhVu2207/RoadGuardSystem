using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class HandoverDocument : IHasRowVersion
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public DateOnly HandoverDate { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public Guid? FileId { get; set; }

    public string? Notes { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public static HandoverDocument Create(
        Guid id,
        Guid projectId,
        string documentNo,
        DateOnly handoverDate,
        Guid acceptedByUserId,
        Guid? fileId,
        string? notes)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || acceptedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Handover, project and accepting user ids must not be empty.");
        }

        if (handoverDate == default)
        {
            throw new ArgumentException("Handover date is required.", nameof(handoverDate));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(documentNo);
        var normalizedDocumentNo = documentNo.Trim();
        if (normalizedDocumentNo.Length > 80)
        {
            throw new ArgumentException("Document number exceeds maximum length 80.", nameof(documentNo));
        }

        return new HandoverDocument
        {
            Id = id,
            ProjectId = projectId,
            DocumentNo = normalizedDocumentNo,
            HandoverDate = handoverDate,
            AcceptedByUserId = acceptedByUserId,
            FileId = fileId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
