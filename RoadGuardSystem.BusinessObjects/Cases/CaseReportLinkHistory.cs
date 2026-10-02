namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CaseReportLinkHistory
{
    private List<Guid> _reportIds = [];

    private CaseReportLinkHistory()
    {
    }

    public Guid Id { get; private set; }
    public Guid FromCaseId { get; private set; }
    public Guid ToCaseId { get; private set; }
    public IReadOnlyList<Guid> ReportIds => _reportIds.AsReadOnly();
    public string Reason { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    internal static CaseReportLinkHistory Create(
        Guid id,
        Guid fromCaseId,
        Guid toCaseId,
        IReadOnlyCollection<Guid> reportIds,
        Guid actorUserId,
        string reason,
        DateTimeOffset occurredAt)
    {
        if (id == Guid.Empty || fromCaseId == Guid.Empty || toCaseId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException("History, case, and actor identifiers must not be empty.");
        }

        return new CaseReportLinkHistory
        {
            Id = id,
            FromCaseId = fromCaseId,
            ToCaseId = toCaseId,
            _reportIds = CaseConclusion.ResolveIds(reportIds, nameof(reportIds)),
            Reason = CaseConclusion.NormalizeReason(reason),
            ActorUserId = actorUserId,
            OccurredAt = occurredAt.ToUniversalTime()
        };
    }
}
