using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.BusinessObjects.Messaging;

public sealed record NotificationRepairCorrectionClaim(Guid DecisionId, Guid ProjectId, Guid ItemId,
    Guid ObligationId, Guid SupersedesDecisionId, RepairPresentationState Result, DateTimeOffset OccurredAtUtc);
public static class NotificationRepairCorrectionProof
{
    public static bool Verify(NotificationRepairCorrectionClaim claim, RepairItem item, RepairObligation obligation,
        RepairDecision decision)
    {
        ArgumentNullException.ThrowIfNull(claim); ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(obligation); ArgumentNullException.ThrowIfNull(decision);
        if (claim.DecisionId == Guid.Empty || claim.ProjectId != item.ProjectId || claim.ItemId != item.Id ||
            claim.ObligationId != obligation.Id || item.ObligationId != obligation.Id || item.ProjectId != obligation.ProjectId ||
            item.DefectId != obligation.DefectId || decision.Id != claim.DecisionId || decision.ItemId != item.Id ||
            decision.ObligationId != obligation.Id || decision.DefectId != item.DefectId || decision.Mode != item.Mode ||
            decision.SupersedesDecisionId != claim.SupersedesDecisionId || claim.SupersedesDecisionId == Guid.Empty ||
            decision.Result != claim.Result || decision.At != claim.OccurredAtUtc || decision.Basis is null ||
            item.EffectiveDecisionId is null || item.EffectiveDecisionId != obligation.EffectiveResolutionHeadDecisionId)
            return false;
        if (item.Decisions.Select(row => row.Id).Distinct().Count() != item.Decisions.Count ||
            obligation.ResolutionHistory.Select(row => row.DecisionId).Distinct().Count() != obligation.ResolutionHistory.Count)
            return false;
        var decisions = item.Decisions.ToDictionary(row => row.Id);
        var history = obligation.ResolutionHistory.ToDictionary(row => row.DecisionId);
        var seen = new HashSet<Guid>(); Guid? cursor = item.EffectiveDecisionId; var found = false;
        while (cursor is Guid id)
        {
            if (!seen.Add(id) || !decisions.TryGetValue(id, out var current) || !history.TryGetValue(id, out var fact) ||
                current.ItemId != item.Id || current.ObligationId != obligation.Id || current.DefectId != item.DefectId || current.Mode != item.Mode ||
                fact.Accepted != current.Accepted || fact.SupersedesDecisionId != current.SupersedesDecisionId || fact.At != current.At)
                return false;
            if (id == item.EffectiveDecisionId && obligation.EffectiveResolutionDecisionId != (current.Accepted ? current.Id : (Guid?)null)) return false;
            if (id == decision.Id) found = current == decision;
            cursor = current.SupersedesDecisionId;
        }
        return found;
    }
}
