namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CasePublication
{
    private List<Guid> _recipientReportIds = [];
    private List<Guid> _defectIds = [];
    private List<Guid> _evidenceIds = [];

    private CasePublication()
    {
    }

    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public IReadOnlyList<Guid> RecipientReportIds => _recipientReportIds.AsReadOnly();
    public IReadOnlyList<Guid> DefectIds => _defectIds.AsReadOnly();
    public IReadOnlyList<Guid> EvidenceIds => _evidenceIds.AsReadOnly();
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; private set; }

    internal static CasePublication Create(
        Guid id,
        Guid caseId,
        IReadOnlyCollection<Guid> recipientReportIds,
        IReadOnlyCollection<Guid> defectIds,
        IReadOnlyCollection<Guid> evidenceIds,
        string summary,
        DateTimeOffset publishedAt)
    {
        if (id == Guid.Empty || caseId == Guid.Empty)
        {
            throw new ArgumentException("Publication and case identifiers must not be empty.");
        }

        var recipients = CaseConclusion.ResolveIds(recipientReportIds, nameof(recipientReportIds));
        if (recipients.Count == 0)
        {
            throw new ArgumentException("A publication requires at least one report recipient.", nameof(recipientReportIds));
        }

        return new CasePublication
        {
            Id = id,
            CaseId = caseId,
            _recipientReportIds = recipients,
            _defectIds = CaseConclusion.ResolveIds(defectIds, nameof(defectIds)),
            _evidenceIds = CaseConclusion.ResolveIds(evidenceIds, nameof(evidenceIds)),
            Summary = CaseConclusion.NormalizeReason(summary),
            PublishedAt = publishedAt.ToUniversalTime()
        };
    }
}
