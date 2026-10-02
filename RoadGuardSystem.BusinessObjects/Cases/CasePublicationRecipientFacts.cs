namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CasePublicationRecipientFacts
{
    private readonly HashSet<Guid> _verifiedDefectIds;
    private readonly HashSet<Guid> _permittedEvidenceIds;

    private CasePublicationRecipientFacts(
        Guid reportId,
        IEnumerable<Guid> verifiedDefectIds,
        IEnumerable<Guid> permittedEvidenceIds)
    {
        ReportId = reportId;
        _verifiedDefectIds = ResolveIds(verifiedDefectIds, nameof(verifiedDefectIds));
        _permittedEvidenceIds = ResolveIds(permittedEvidenceIds, nameof(permittedEvidenceIds));
    }

    public Guid ReportId { get; }

    public static CasePublicationRecipientFacts Create(
        Guid reportId,
        IEnumerable<Guid> verifiedDefectIds,
        IEnumerable<Guid> permittedEvidenceIds)
    {
        if (reportId == Guid.Empty)
        {
            throw new ArgumentException("Report identifier must not be empty.", nameof(reportId));
        }

        return new CasePublicationRecipientFacts(reportId, verifiedDefectIds, permittedEvidenceIds);
    }

    internal bool Authorizes(IEnumerable<Guid> defectIds, IEnumerable<Guid> evidenceIds)
    {
        return defectIds.All(_verifiedDefectIds.Contains) && evidenceIds.All(_permittedEvidenceIds.Contains);
    }

    private static HashSet<Guid> ResolveIds(IEnumerable<Guid> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var ids = values.ToHashSet();
        if (ids.Contains(Guid.Empty))
        {
            throw new ArgumentException("Identifiers must not contain an empty value.", parameterName);
        }

        return ids;
    }
}
