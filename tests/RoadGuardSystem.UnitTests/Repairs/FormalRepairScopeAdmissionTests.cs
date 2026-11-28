using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class FormalRepairScopeAdmissionTests
{
    private static readonly Guid Road = Guid.NewGuid();
    private static RepairActualScope Scope(decimal from = 1, decimal to = 3, string frame = "pinned-v1", Guid? road = null)
        => RepairActualScope.Create(Guid.NewGuid(), road ?? Road, frame, "source label", from, to, 0, 2);
    private static RepairObligation Obligation(RepairActualScope scope, RepairObligationKind kind = RepairObligationKind.FormalRepair)
        => RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), kind, true, scope);

    [Theory]
    [InlineData(1, 3)]
    [InlineData(0, 4)]
    [InlineData(2, 4)]
    public void EqualContainsAndOverlapRemainReserved(int from, int to)
    {
        var obligation = Obligation(Scope());
        var conflict = FormalRepairScopeAdmission.Evaluate(Scope(from, to), [obligation]);
        Assert.Equal("active_scope_conflict", conflict!.Code);
        Assert.Equal("ACTIVE_FORMAL_SCOPE", conflict.Reason);
        Assert.Equal(obligation.Id, conflict.Obligation.Id);
    }

    [Fact]
    public void UnknownFramesAndRoadIdentitiesCannotEstablishDisjointness()
    {
        var obligation = Obligation(Scope());
        Assert.Equal("SCOPE_COMPARISON_UNVERIFIED", FormalRepairScopeAdmission.Evaluate(Scope(10, 20, "other-version"), [obligation])!.Reason);
        Assert.Equal("SCOPE_COMPARISON_UNVERIFIED", FormalRepairScopeAdmission.Evaluate(Scope(10, 20, road: Guid.NewGuid()), [obligation])!.Reason);
        Assert.Null(FormalRepairScopeAdmission.Evaluate(Scope(3, 5), [obligation]));
    }

    [Fact]
    public void CompletedFormalScopeStillBlocksAndOwnFirstItemAndSafetyAreExcluded()
    {
        var obligation = Obligation(Scope());
        var decision = new RepairDecision(Guid.NewGuid(), Guid.NewGuid(), obligation.Id, obligation.DefectId,
            RepairMode.Normal, Guid.NewGuid(), RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Supervisor,
            "final confirmation", DateTimeOffset.UtcNow, null, RepairPresentationState.Confirmed);
        obligation.Resolve(decision);
        Assert.Equal("completed_scope_conflict", FormalRepairScopeAdmission.Evaluate(Scope(), [obligation])!.Code);
        Assert.Null(FormalRepairScopeAdmission.Evaluate(Scope(), [obligation], obligation.Id));
        Assert.Null(FormalRepairScopeAdmission.Evaluate(Scope(), [Obligation(Scope(), RepairObligationKind.TemporarySafety)]));
        Assert.Null(FormalRepairScopeAdmission.Evaluate(Scope(3, 5), [obligation]));
    }
}
