namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CasePublicationPrerequisites
{
    private readonly Dictionary<Guid, CasePublicationRecipientFacts> _factsByReportId;

    private CasePublicationPrerequisites(IEnumerable<CasePublicationRecipientFacts> recipientFacts)
    {
        _factsByReportId = [];
        foreach (var facts in recipientFacts)
        {
            ArgumentNullException.ThrowIfNull(facts, nameof(recipientFacts));
            if (!_factsByReportId.TryAdd(facts.ReportId, facts))
            {
                throw new ArgumentException("Publication facts must have one entry per recipient report.", nameof(recipientFacts));
            }
        }
    }

    public static CasePublicationPrerequisites Create(IEnumerable<CasePublicationRecipientFacts> recipientFacts)
    {
        ArgumentNullException.ThrowIfNull(recipientFacts);
        return new CasePublicationPrerequisites(recipientFacts);
    }

    internal bool Authorizes(
        IEnumerable<Guid> recipientReportIds,
        IEnumerable<Guid> defectIds,
        IEnumerable<Guid> evidenceIds)
    {
        return recipientReportIds.All(reportId =>
            _factsByReportId.TryGetValue(reportId, out var facts) && facts.Authorizes(defectIds, evidenceIds));
    }
}
