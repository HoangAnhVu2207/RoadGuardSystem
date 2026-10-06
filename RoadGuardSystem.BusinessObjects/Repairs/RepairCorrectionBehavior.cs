using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Repairs;

public sealed class RepairCorrectionBasis
{
    public string Text { get; private set; } = "";
    public IReadOnlyList<RepairEvidenceReference> Evidence => _evidence.AsReadOnly();
    private readonly List<RepairEvidenceReference> _evidence = [];
    private RepairCorrectionBasis() { }
    public static RepairCorrectionBasis Create(string text, IReadOnlyList<RepairEvidenceReference> evidence)
    {
        var basis = RepairGuards.Text(text); ArgumentNullException.ThrowIfNull(evidence);
        foreach (var fact in evidence)
        {
            ArgumentNullException.ThrowIfNull(fact); RepairGuards.Id(fact.FileId); RepairGuards.Id(fact.SourceId);
            RepairGuards.EnumValue(fact.Purpose); RepairGuards.Text(fact.SourceKind);
            if (!fact.Verified || !fact.Related || string.IsNullOrWhiteSpace(fact.FileVersion) || string.IsNullOrWhiteSpace(fact.Hash))
                throw new ArgumentException("Correction evidence requires verified related immutable source facts.");
            RepairGuards.Text(fact.FileVersion); RepairGuards.Text(fact.Hash);
            if (fact.CapturedAt is not null) RepairGuards.Time(fact.CapturedAt.Value);
        }
        if (evidence.Select(fact => fact.FileId).Distinct().Count() != evidence.Count)
            throw new ArgumentException("Correction basis cannot duplicate a file reference.");
        var result = new RepairCorrectionBasis { Text = basis }; result._evidence.AddRange(evidence); return result;
    }
}

public sealed record RepairReviewRequest(Guid Id, Guid ProjectId, Guid ItemId, Guid DecisionId, Guid ActorId,
    UserRoleCode Role, string Reason, DateTimeOffset At);

/// <summary>Pure domain consistency only. Repository must persist every resulting state/history/projection,
/// including package/Defect basis invalidation, within one current-authority guarded transaction.</summary>
public static class RepairCorrectionEffects
{
    public static RepairDecision Apply(RepairItem item, RepairObligation obligation, Guid id, Guid supersedes, Guid actor,
        UserRoleCode role, string reason, DateTimeOffset at, RepairCorrectionAuthority authority,
        RepairPresentationState result, RepairCorrectionBasis basis)
    {
        ArgumentNullException.ThrowIfNull(item); ArgumentNullException.ThrowIfNull(obligation);
        if (item.ObligationId != obligation.Id || item.ProjectId != obligation.ProjectId || item.DefectId != obligation.DefectId)
            throw new InvalidOperationException("Correction effects require the item's actual obligation.");
        obligation.EnsureCorrectionHead(supersedes);
        var decision = item.CorrectResult(id, supersedes, actor, role, reason, at, authority, result, basis);
        obligation.ApplyCorrection(decision);
        return decision;
    }
}
