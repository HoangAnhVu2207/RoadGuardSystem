namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CaseConclusionPrerequisites
{
    private readonly HashSet<Guid> _verifiedDefectIds;
    private readonly HashSet<Guid> _verifiedEvidenceIds;

    private CaseConclusionPrerequisites(IEnumerable<Guid> verifiedDefectIds, IEnumerable<Guid> verifiedEvidenceIds)
    {
        _verifiedDefectIds = ToIds(verifiedDefectIds, nameof(verifiedDefectIds));
        _verifiedEvidenceIds = ToIds(verifiedEvidenceIds, nameof(verifiedEvidenceIds));
    }

    public static CaseConclusionPrerequisites Create(IEnumerable<Guid> verifiedDefectIds, IEnumerable<Guid> verifiedEvidenceIds)
    {
        return new CaseConclusionPrerequisites(verifiedDefectIds, verifiedEvidenceIds);
    }

    internal bool ContainsVerifiedDefects(IEnumerable<Guid> ids) => ids.All(_verifiedDefectIds.Contains);
    internal bool ContainsVerifiedEvidence(IEnumerable<Guid> ids) => ids.All(_verifiedEvidenceIds.Contains);

    private static HashSet<Guid> ToIds(IEnumerable<Guid> values, string parameterName)
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
