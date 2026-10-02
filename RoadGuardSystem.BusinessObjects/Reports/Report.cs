namespace RoadGuardSystem.BusinessObjects.Reports;

public sealed class Report
{
    private List<ReportEvidence> _originalEvidence = [];
    private List<ReportSupplement> _supplements = [];

    private Report()
    {
    }

    public Guid Id { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }
    public IReadOnlyList<ReportEvidence> OriginalEvidence => _originalEvidence.AsReadOnly();
    public IReadOnlyList<ReportSupplement> Supplements => _supplements.AsReadOnly();

    public static Report Create(
        Guid id,
        Guid reporterUserId,
        string description,
        DateTimeOffset receivedAt,
        IReadOnlyCollection<VerifiedEvidenceReference> evidence)
    {
        if (id == Guid.Empty || reporterUserId == Guid.Empty)
        {
            throw new ArgumentException("Report and reporter identifiers must not be empty.");
        }

        return new Report
        {
            Id = id,
            ReporterUserId = reporterUserId,
            Description = NormalizeDescription(description, nameof(description)),
            ReceivedAt = receivedAt.ToUniversalTime(),
            _originalEvidence = ResolveEvidence(id, null, evidence, reporterUserId)
        };
    }

    public void AddSupplement(
        Guid supplementId,
        string description,
        IReadOnlyCollection<VerifiedEvidenceReference> evidence,
        DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (_supplements.Any(supplement => supplement.Id == supplementId))
        {
            throw new InvalidOperationException("A supplement identifier can be appended only once.");
        }

        var existingEvidenceIds = _originalEvidence
            .Concat(_supplements.SelectMany(supplement => supplement.Evidence))
            .Select(evidenceItem => evidenceItem.Id)
            .ToHashSet();
        if (evidence.Any(reference => reference is not null && existingEvidenceIds.Contains(reference.EvidenceId)))
        {
            throw new InvalidOperationException("An evidence reference can be attached to a report only once.");
        }

        var supplement = ReportSupplement.Create(supplementId, Id, description, receivedAt, evidence, ReporterUserId);
        _supplements.Add(supplement);
    }

    internal static string NormalizeDescription(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 1_000)
        {
            throw new ArgumentException("Description exceeds maximum length 1000.", parameterName);
        }

        return normalized;
    }

    internal static List<ReportEvidence> ResolveEvidence(
        Guid reportId,
        Guid? supplementId,
        IReadOnlyCollection<VerifiedEvidenceReference> evidence,
        Guid reporterUserId)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Count == 0)
        {
            throw new ArgumentException("At least one evidence reference is required.", nameof(evidence));
        }

        if (evidence.Any(reference => reference is null || reference.OwnerUserId != reporterUserId))
        {
            throw new ArgumentException("Evidence must be server-verified as owned by the report's reporter.", nameof(evidence));
        }

        if (evidence.GroupBy(reference => reference.EvidenceId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Evidence references must be unique within one report append.", nameof(evidence));
        }

        return evidence.Select(reference => ReportEvidence.FromReference(reportId, supplementId, reference)).ToList();
    }
}
