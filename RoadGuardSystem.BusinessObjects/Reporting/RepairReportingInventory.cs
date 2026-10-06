using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Reporting;

// Internal materialized-domain inventory only: not a public metric definition, SQL coverage proof,
// authority decision, period allocation or export schema. The runtime reader owns those boundaries.
public sealed record RepairReportingSource(RepairItem Item, RepairObligation Obligation,
    IReadOnlyList<Guid> LocationReferenceIds);
public sealed record RepairReportingDecision(Guid Id, Guid? SupersedesId, RepairPresentationState Result,
    Guid ActorId, DateTimeOffset At, string Reason, string? Basis, IReadOnlyList<Guid> EvidenceFileIds)
{
    public UserRoleCode Role { get; init; }
    public IReadOnlyList<RepairEvidenceReference> EvidenceFacts { get; init; } = [];
}
public sealed record RepairReportingItem(Guid ItemId, Guid DefectId, Guid ObligationId, RepairMode Mode,
    RepairItemState State, RepairPresentationState Presentation, bool Mandatory, bool ObligationResolved,
    Guid? ObligationResolutionDecisionId, Guid? OriginalPerformedAttemptId, Guid? OriginalDecisionId,
    Guid? EffectiveDecisionId, IReadOnlyList<Guid> AttemptIds, IReadOnlyList<RepairReportingDecision> Decisions,
    IReadOnlyList<Guid> LocationReferenceIds);
public sealed record RepairReportingCapture(Guid ProjectId, IReadOnlyList<RepairReportingItem> Items)
{
    public int SuppliedItemCount => Items.Count;
    public int SuppliedConfirmedItemCount => Items.Count(x => x.Presentation == RepairPresentationState.Confirmed);
}
public static class RepairReportingInventory
{
    public static RepairReportingCapture Capture(Guid projectId, IReadOnlyList<RepairReportingSource> sources)
    {
        if (projectId == Guid.Empty) throw new ArgumentException("A project identity is required.", nameof(projectId));
        ArgumentNullException.ThrowIfNull(sources);
        var items = new Dictionary<Guid, RepairReportingItem>();
        foreach (var source in sources)
        {
            if (source is null || source.Item is null || source.Obligation is null || source.LocationReferenceIds is null ||
                source.LocationReferenceIds.Contains(Guid.Empty))
                throw new ArgumentException("Materialized source and non-empty location identities are required.", nameof(sources));
            var item = source.Item; var obligation = source.Obligation;
            if (item.ProjectId != projectId || obligation.ProjectId != projectId || item.ObligationId != obligation.Id || item.DefectId != obligation.DefectId)
                throw new InvalidOperationException("Reporting sources must belong to the project and actual item obligation.");
            var decisions = item.Decisions.Select(decision => new RepairReportingDecision(decision.Id, decision.SupersedesDecisionId,
                decision.Result, decision.ActorId, decision.At, decision.Reason, decision.Basis?.Text,
                Array.AsReadOnly((decision.Basis?.Evidence.Select(e => e.FileId) ?? []).Order().ToArray()))
            {
                Role = decision.Role,
                EvidenceFacts = Array.AsReadOnly((decision.Basis?.Evidence ?? []).OrderBy(e => e.FileId).ToArray())
            }).ToArray();
            var row = new RepairReportingItem(item.Id, item.DefectId, obligation.Id, item.Mode, item.State, item.Presentation,
                obligation.Mandatory, obligation.IsResolved, obligation.EffectiveResolutionDecisionId,
                item.Attempts.FirstOrDefault(attempt => attempt.Performed)?.Id, item.Decisions.FirstOrDefault(decision => decision.SupersedesDecisionId is null)?.Id,
                item.EffectiveDecision?.Id, Array.AsReadOnly(item.Attempts.Select(attempt => attempt.Id).ToArray()),
                Array.AsReadOnly(decisions), Array.AsReadOnly(source.LocationReferenceIds.Distinct().Order().ToArray()));
            if (items.TryGetValue(item.Id, out var existing))
            {
                if (!SameFacts(existing, row)) throw new InvalidOperationException("Conflicting copies cannot establish an effective reporting item.");
                row = existing with { LocationReferenceIds = Array.AsReadOnly(existing.LocationReferenceIds.Concat(row.LocationReferenceIds).Distinct().Order().ToArray()) };
            }
            items[item.Id] = row;
        }
        return new(projectId, Array.AsReadOnly(items.Values.OrderBy(item => item.ItemId).ToArray()));
    }

    private static bool SameFacts(RepairReportingItem a, RepairReportingItem b)
        => a.ItemId == b.ItemId && a.DefectId == b.DefectId && a.ObligationId == b.ObligationId && a.Mode == b.Mode &&
            a.State == b.State && a.Presentation == b.Presentation && a.Mandatory == b.Mandatory && a.ObligationResolved == b.ObligationResolved &&
            a.ObligationResolutionDecisionId == b.ObligationResolutionDecisionId && a.OriginalPerformedAttemptId == b.OriginalPerformedAttemptId &&
            a.OriginalDecisionId == b.OriginalDecisionId && a.EffectiveDecisionId == b.EffectiveDecisionId && a.AttemptIds.SequenceEqual(b.AttemptIds) &&
            a.Decisions.Count == b.Decisions.Count && a.Decisions.Zip(b.Decisions).All(pair =>
                pair.First with { EvidenceFileIds = Array.Empty<Guid>(), EvidenceFacts = Array.Empty<RepairEvidenceReference>() } ==
                pair.Second with { EvidenceFileIds = Array.Empty<Guid>(), EvidenceFacts = Array.Empty<RepairEvidenceReference>() } &&
                pair.First.EvidenceFileIds.SequenceEqual(pair.Second.EvidenceFileIds) && pair.First.EvidenceFacts.SequenceEqual(pair.Second.EvidenceFacts));
}
