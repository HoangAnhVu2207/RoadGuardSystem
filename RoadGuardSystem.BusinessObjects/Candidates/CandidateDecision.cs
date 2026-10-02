namespace RoadGuardSystem.BusinessObjects.Candidates;

public sealed class CandidateDecision
{
    private CandidateDecision()
    {
    }

    public Guid Id { get; private set; }
    public CandidateSourceIdentity Source { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public string GeometryVersion { get; private set; } = string.Empty;
    public CandidateDecisionKind Decision { get; private set; }
    public CandidateClassification? Classification { get; private set; }
    public Guid? TargetDefectId { get; private set; }
    public string? TargetDefectVersion { get; private set; }
    public Guid? SupersedesDecisionId { get; private set; }
    public string? ExpectedPreviousDecisionVersion { get; private set; }
    public Guid DecidedByUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset DecidedAt { get; private set; }

    public static CandidateDecision Create(
        Guid id,
        CandidateSourceFacts facts,
        CandidateDecisionKind decision,
        CandidateClassification? classification,
        Guid targetDefectId,
        string? targetDefectVersion,
        CandidateCorrection? correction,
        Guid decidedByUserId,
        string reason,
        DateTimeOffset decidedAt)
    {
        if (id == Guid.Empty || decidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Decision and actor identifiers must not be empty.");
        }

        ArgumentNullException.ThrowIfNull(facts);
        if (decision == CandidateDecisionKind.Unknown || !Enum.IsDefined(decision))
        {
            throw new ArgumentOutOfRangeException(nameof(decision));
        }

        ValidateCorrection(facts.ActiveDisposition, correction);
        ValidateDecisionFields(decision, classification, targetDefectId, targetDefectVersion);
        return new CandidateDecision
        {
            Id = id,
            Source = facts.Source,
            ProjectId = facts.ProjectId,
            GeometryVersion = facts.GeometryVersion,
            Decision = decision,
            Classification = classification,
            TargetDefectId = targetDefectId == Guid.Empty ? null : targetDefectId,
            TargetDefectVersion = string.IsNullOrWhiteSpace(targetDefectVersion) ? null : CandidateSourceIdentity.NormalizeVersion(targetDefectVersion, nameof(targetDefectVersion)),
            SupersedesDecisionId = correction?.SupersedesDecisionId,
            ExpectedPreviousDecisionVersion = correction?.ExpectedPreviousDecisionVersion,
            DecidedByUserId = decidedByUserId,
            Reason = NormalizeReason(reason),
            DecidedAt = decidedAt.ToUniversalTime()
        };
    }

    private static void ValidateCorrection(ActiveCandidateDisposition? activeDisposition, CandidateCorrection? correction)
    {
        if (correction is null && activeDisposition is not null)
        {
            throw new InvalidOperationException("An active disposition requires a correction command.");
        }

        if (correction is not null && (activeDisposition is null ||
            correction.SupersedesDecisionId != activeDisposition.DecisionId ||
            !string.Equals(correction.ExpectedPreviousDecisionVersion, activeDisposition.Version, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The correction must match the active disposition identifier and version.");
        }
    }

    private static void ValidateDecisionFields(
        CandidateDecisionKind decision,
        CandidateClassification? classification,
        Guid targetDefectId,
        string? targetDefectVersion)
    {
        var hasTarget = targetDefectId != Guid.Empty;
        var hasTargetVersion = !string.IsNullOrWhiteSpace(targetDefectVersion);
        switch (decision)
        {
            case CandidateDecisionKind.KeepNew when classification is null || hasTarget || hasTargetVersion:
                throw new ArgumentException("Keep-new requires classification and forbids a target defect.");
            case CandidateDecisionKind.LinkExisting when classification is not null || !hasTarget || !hasTargetVersion:
                throw new ArgumentException("Link-existing requires target identifier and version and forbids classification.");
            case CandidateDecisionKind.Reject when classification is not null || hasTarget || hasTargetVersion:
                throw new ArgumentException("Reject forbids classification and a target defect.");
        }
    }

    private static string NormalizeReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > 1_000)
        {
            throw new ArgumentException("Reason exceeds maximum length 1000.", nameof(value));
        }

        return normalized;
    }
}
