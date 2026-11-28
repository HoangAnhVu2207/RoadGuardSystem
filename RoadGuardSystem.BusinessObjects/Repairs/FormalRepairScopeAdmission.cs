namespace RoadGuardSystem.BusinessObjects.Repairs;

public sealed record FormalRepairConflict(RepairObligation Obligation, string Code, string Reason);

public static class FormalRepairScopeAdmission
{
    // Different identities/frames alone do not prove disjointness. No spatial buffer or snapping.
    public static FormalRepairConflict? Evaluate(RepairActualScope requested, IEnumerable<RepairObligation> obligations,
        Guid? ownObligationId = null)
    {
        foreach (var obligation in obligations.Where(o => o.Kind == RepairObligationKind.FormalRepair && o.Id != ownObligationId)
            .OrderByDescending(o => o.IsResolved).ThenBy(o => o.Id))
        {
            var relation = obligation.Scope is null || requested.PhysicalRoadId != obligation.Scope.PhysicalRoadId
                ? RepairScopeComparison.Unknown : requested.Compare(obligation.Scope);
            if (relation == RepairScopeComparison.Disjoint) continue;
            return new(obligation, obligation.IsResolved ? "completed_scope_conflict" : "active_scope_conflict",
                relation == RepairScopeComparison.Unknown ? "SCOPE_COMPARISON_UNVERIFIED" : obligation.IsResolved ? "COMPLETED_FORMAL_SCOPE" : "ACTIVE_FORMAL_SCOPE");
        }
        return null;
    }
}
