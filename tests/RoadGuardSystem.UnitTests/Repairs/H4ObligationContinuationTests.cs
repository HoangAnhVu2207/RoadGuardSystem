using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4ObligationContinuationTests
{
    [Fact]
    public void RepeatedCorrectionPinsPriorObligationHeadWithoutRewritingOriginalDecision()
    {
        var at = new DateTimeOffset(2026, 10, 6, 7, 0, 0, TimeSpan.Zero);
        var obligation = RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "TEST_FRAME", "road", 0, 10, 0, 1));
        var pm = Guid.NewGuid(); var crew = Guid.NewGuid();
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.FastTrack, pm, UserRoleCode.ProjectManager,
            at, new("repair plan", "checklist"));
        item.Assign(crew, pm, UserRoleCode.ProjectManager, at.AddMinutes(1)); item.Start(crew, at.AddMinutes(2));
        item.Submit(RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), item.Id, obligation.Id,
            item.ProjectId, item.DefectId, crew, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "TEST_FRAME", Guid.NewGuid(),
            true, null, at.AddMinutes(2), at.AddMinutes(3), at.AddMinutes(4), RepairTimeProvenance.VerifiedOnline,
            [new(Guid.NewGuid(), "v1", new string('b', 64), RepairEvidencePurpose.After, true, true, "TEST_POLICY",
                Guid.NewGuid(), at.AddMinutes(3), null, false)]));
        item.Review(pm, UserRoleCode.ProjectManager, at.AddMinutes(5));
        var original = item.Confirm(Guid.NewGuid(), pm, UserRoleCode.ProjectManager, "initial", at.AddMinutes(6));
        obligation.Resolve(original);
        var authority = new RepairCorrectionAuthority(Guid.NewGuid(), item.Id, pm, UserRoleCode.ProjectManager, "TEST_CURRENT_MEMBERSHIP");
        var first = RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), original.Id, pm, UserRoleCode.ProjectManager,
            "insufficient", at.AddMinutes(7), authority, RepairPresentationState.Unrepaired, RepairCorrectionBasis.Create("basis", []));
        var second = RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), first.Id, pm, UserRoleCode.ProjectManager,
            "reassessed", at.AddMinutes(8), authority, RepairPresentationState.ReportedAwaitingReview, RepairCorrectionBasis.Create("second basis", []));
        Assert.Null(original.PreviousObligationHeadDecisionId); Assert.Null(original.SupersedesDecisionId);
        Assert.Equal(original.Id, first.PreviousObligationHeadDecisionId); Assert.Equal(first.Id, second.PreviousObligationHeadDecisionId);
        Assert.Equal(original.Id, obligation.ResolutionHistory.Single(row => row.DecisionId == first.Id).PreviousHeadDecisionId);
        Assert.Equal(first.Id, obligation.ResolutionHistory.Single(row => row.DecisionId == second.Id).PreviousHeadDecisionId);
        Assert.Equal(original, item.Decisions.Single(row => row.Id == original.Id));
        Assert.False(obligation.IsResolved); Assert.Equal(second.Id, obligation.EffectiveResolutionHeadDecisionId);
    }
}
