namespace RoadGuardSystem.BusinessObjects.Candidates;

public sealed record ActiveCandidateDisposition
{
    public ActiveCandidateDisposition(Guid decisionId, string version)
    {
        if (decisionId == Guid.Empty)
        {
            throw new ArgumentException("Active decision identifier must not be empty.", nameof(decisionId));
        }

        DecisionId = decisionId;
        Version = CandidateSourceIdentity.NormalizeVersion(version, nameof(version));
    }

    public Guid DecisionId { get; }
    public string Version { get; }
}
