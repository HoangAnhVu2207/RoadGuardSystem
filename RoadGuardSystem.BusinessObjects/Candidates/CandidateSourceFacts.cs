namespace RoadGuardSystem.BusinessObjects.Candidates;

public sealed class CandidateSourceFacts
{
    private CandidateSourceFacts(
        CandidateSourceIdentity source,
        Guid projectId,
        string geometryVersion,
        ActiveCandidateDisposition? activeDisposition)
    {
        Source = source;
        ProjectId = projectId;
        GeometryVersion = geometryVersion;
        ActiveDisposition = activeDisposition;
    }

    public CandidateSourceIdentity Source { get; }
    public Guid ProjectId { get; }
    public string GeometryVersion { get; }
    public ActiveCandidateDisposition? ActiveDisposition { get; }

    public static CandidateSourceFacts Create(
        CandidateSourceIdentity source,
        Guid projectId,
        string geometryVersion,
        ActiveCandidateDisposition? activeDisposition = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project identifier must not be empty.", nameof(projectId));
        }

        return new CandidateSourceFacts(
            source,
            projectId,
            CandidateSourceIdentity.NormalizeVersion(geometryVersion, nameof(geometryVersion)),
            activeDisposition);
    }
}
