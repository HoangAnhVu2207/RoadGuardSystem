namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class CasePublicationPrerequisites
{
    private readonly HashSet<Guid> _verifiedDefectIds;
    private readonly HashSet<Guid> _permittedEvidenceIds;

    private CasePublicationPrerequisites(IEnumerable<Guid> verifiedDefectIds, IEnumerable<Guid> permittedEvidenceIds)
    {
        _verifiedDefectIds = verifiedDefectIds.ToHashSet();
        _permittedEvidenceIds = permittedEvidenceIds.ToHashSet();
        if (_verifiedDefectIds.Contains(Guid.Empty) || _permittedEvidenceIds.Contains(Guid.Empty))
        {
            throw new ArgumentException("Prerequisite identifiers must not be empty.");
        }
    }

    public static CasePublicationPrerequisites Create(IEnumerable<Guid> verifiedDefectIds, IEnumerable<Guid> permittedEvidenceIds)
    {
        ArgumentNullException.ThrowIfNull(verifiedDefectIds);
        ArgumentNullException.ThrowIfNull(permittedEvidenceIds);
        return new CasePublicationPrerequisites(verifiedDefectIds, permittedEvidenceIds);
    }

    internal bool ContainsVerifiedDefects(IEnumerable<Guid> ids) => ids.All(_verifiedDefectIds.Contains);
    internal bool ContainsPermittedEvidence(IEnumerable<Guid> ids) => ids.All(_permittedEvidenceIds.Contains);
}
