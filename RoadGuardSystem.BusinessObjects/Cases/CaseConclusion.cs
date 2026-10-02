namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CaseConclusion
{
    private List<Guid> _defectIds = [];
    private List<Guid> _evidenceIds = [];

    private CaseConclusion()
    {
    }

    public Guid Id { get; private set; }
    public CaseConclusionOutcome Outcome { get; private set; }
    public IReadOnlyList<Guid> DefectIds => _defectIds.AsReadOnly();
    public IReadOnlyList<Guid> EvidenceIds => _evidenceIds.AsReadOnly();
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset ConcludedAt { get; private set; }

    public static CaseConclusion Create(
        Guid id,
        CaseConclusionOutcome outcome,
        IReadOnlyCollection<Guid> defectIds,
        IReadOnlyCollection<Guid> evidenceIds,
        string reason,
        DateTimeOffset concludedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Conclusion identifier must not be empty.", nameof(id));
        }

        if (outcome == CaseConclusionOutcome.Unknown || !Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        var resolvedDefectIds = ResolveIds(defectIds, nameof(defectIds));
        var resolvedEvidenceIds = ResolveIds(evidenceIds, nameof(evidenceIds));
        if (outcome == CaseConclusionOutcome.Confirmed && (resolvedDefectIds.Count == 0 || resolvedEvidenceIds.Count == 0))
        {
            throw new ArgumentException("A confirmed conclusion requires defects and evidence.");
        }

        if (outcome == CaseConclusionOutcome.NoDefect && resolvedDefectIds.Count != 0)
        {
            throw new ArgumentException("A no-defect conclusion cannot reference defects.", nameof(defectIds));
        }

        return new CaseConclusion
        {
            Id = id,
            Outcome = outcome,
            Reason = NormalizeReason(reason),
            ConcludedAt = concludedAt.ToUniversalTime(),
            _defectIds = resolvedDefectIds,
            _evidenceIds = resolvedEvidenceIds
        };
    }

    internal static string NormalizeReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > 1_000)
        {
            throw new ArgumentException("Reason exceeds maximum length 1000.", nameof(value));
        }

        return normalized;
    }

    internal static List<Guid> ResolveIds(IReadOnlyCollection<Guid> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        if (values.Any(id => id == Guid.Empty) || values.Distinct().Count() != values.Count)
        {
            throw new ArgumentException("Identifiers must be non-empty and unique.", parameterName);
        }

        return values.ToList();
    }
}
