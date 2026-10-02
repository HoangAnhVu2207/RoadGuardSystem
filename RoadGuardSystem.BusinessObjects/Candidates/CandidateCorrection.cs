namespace RoadGuardSystem.BusinessObjects.Candidates;

public sealed class CandidateCorrection
{
    private CandidateCorrection(Guid supersedesDecisionId, string expectedPreviousDecisionVersion)
    {
        SupersedesDecisionId = supersedesDecisionId;
        ExpectedPreviousDecisionVersion = expectedPreviousDecisionVersion;
    }

    public Guid SupersedesDecisionId { get; }
    public string ExpectedPreviousDecisionVersion { get; }

    public static CandidateCorrection Create(Guid supersedesDecisionId, string expectedPreviousDecisionVersion)
    {
        if (supersedesDecisionId == Guid.Empty)
        {
            throw new ArgumentException("Superseded decision identifier must not be empty.", nameof(supersedesDecisionId));
        }

        return new CandidateCorrection(
            supersedesDecisionId,
            CandidateSourceIdentity.NormalizeVersion(expectedPreviousDecisionVersion, nameof(expectedPreviousDecisionVersion)));
    }
}
