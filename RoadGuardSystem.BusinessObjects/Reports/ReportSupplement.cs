namespace RoadGuardSystem.BusinessObjects.Reports;

public sealed class ReportSupplement
{
    private List<ReportEvidence> _evidence = [];

    private ReportSupplement()
    {
    }

    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public IReadOnlyList<ReportEvidence> Evidence => _evidence.AsReadOnly();

    internal static ReportSupplement Create(
        Guid id,
        Guid reportId,
        string description,
        DateTimeOffset receivedAt,
        IReadOnlyCollection<VerifiedEvidenceReference> evidence,
        Guid reporterUserId)
    {
        if (id == Guid.Empty || reportId == Guid.Empty)
        {
            throw new ArgumentException("Supplement and report identifiers must not be empty.");
        }

        var normalizedDescription = Report.NormalizeDescription(description, nameof(description));
        var resolvedEvidence = Report.ResolveEvidence(reportId, id, evidence, reporterUserId);
        return new ReportSupplement
        {
            Id = id,
            ReportId = reportId,
            Description = normalizedDescription,
            ReceivedAt = receivedAt.ToUniversalTime(),
            _evidence = resolvedEvidence
        };
    }
}
