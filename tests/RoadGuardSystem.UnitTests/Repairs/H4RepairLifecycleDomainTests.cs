using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairLifecycleDomainTests
{
    private static readonly Guid Pm = Guid.NewGuid();
    private static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-10-06T00:00:00Z");

    [Fact]
    public void CancelledWorkContinuesSameObligationWithFreshNormalApprovalAndReciprocalHistory()
    {
        var (package, source, obligation) = Fixture();
        source.Cancel(Pm, UserRoleCode.ProjectManager, "normal continuation", At.AddMinutes(1), null);
        var successor = package.ContinueNormally(source.Id, Guid.NewGuid(), Pm, At.AddMinutes(2), new("new plan", "checklist"));
        Assert.Equal(RepairMode.Normal, successor.Mode);
        Assert.Equal(RepairItemState.AwaitingApproval, successor.State);
        Assert.Equal(obligation.Id, successor.ObligationId);
        Assert.Equal(source.Id, successor.PredecessorItemId);
        Assert.Equal(successor.Id, source.SupersededByItemId);
        Assert.Null(successor.ApprovedBy); Assert.Null(successor.CurrentBindingId);
        Assert.False(obligation.IsResolved); Assert.False(package.IsComplete);
        Assert.Equal(RepairItemState.Cancelled, source.State);
        Assert.Throws<InvalidOperationException>(() => package.ContinueNormally(source.Id, Guid.NewGuid(), Pm,
            At.AddMinutes(3), new("duplicate", "checklist")));
    }

    [Fact]
    public void ActiveOrSubmittedHistoryCannotBeSilentlySuperseded()
    {
        var (package, source, _) = Fixture();
        Assert.Throws<InvalidOperationException>(() => package.ContinueNormally(source.Id, Guid.NewGuid(), Pm,
            At.AddMinutes(1), new("new plan", "checklist")));
        Assert.Null(source.SupersededByItemId); Assert.Single(package.Items);
    }

    [Fact]
    public void InvalidSuccessorPlanDoesNotRetirePredecessor()
    {
        var (package, source, _) = Fixture();
        source.Cancel(Pm, UserRoleCode.ProjectManager, "cancel", At.AddMinutes(1), null);
        Assert.Throws<ArgumentException>(() => package.ContinueNormally(source.Id, Guid.NewGuid(), Pm,
            At.AddMinutes(2), new("", "checklist")));
        Assert.Null(source.SupersededByItemId); Assert.Single(package.Items);
    }

    [Fact]
    public void CorrectedPredecessorRemainsImmutableAndSuccessorAcceptanceContinuesResolutionHead()
    {
        var (package, source, obligation) = Fixture(); var crew = Guid.NewGuid(); var supervisor = Guid.NewGuid();
        SubmitAndReview(source, crew, At);
        var accepted = source.Confirm(Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, "accepted", At.AddMinutes(4));
        obligation.Resolve(accepted);
        var correction = RepairCorrectionEffects.Apply(source, obligation, Guid.NewGuid(), accepted.Id, Pm,
            UserRoleCode.ProjectManager, "repair inadequate", At.AddMinutes(5),
            new(Guid.NewGuid(), source.Id, Pm, UserRoleCode.ProjectManager, "CURRENT_PROJECT_MEMBERSHIP"),
            RepairPresentationState.Unrepaired, RepairCorrectionBasis.Create("actual failed area", []));
        var attempt = Assert.Single(source.Attempts);
        var next = package.ContinueNormally(source.Id, Guid.NewGuid(), Pm, At.AddMinutes(6), new("new plan", "checklist"));
        Assert.Equal(RepairItemState.CorrectionRequired, source.State); Assert.Same(attempt, Assert.Single(source.Attempts));
        Assert.Equal(correction.Id, obligation.EffectiveResolutionHeadDecisionId); Assert.False(obligation.IsResolved);
        Assert.Throws<InvalidOperationException>(() => source.Correct(Guid.NewGuid(), correction.Id, Pm,
            UserRoleCode.ProjectManager, "late old-item edit", At.AddMinutes(7),
            new(Guid.NewGuid(), source.Id, Pm, UserRoleCode.ProjectManager, "CURRENT_PROJECT_MEMBERSHIP")));
        next.Approve(supervisor, UserRoleCode.Supervisor, At.AddMinutes(7));
        SubmitAndReview(next, crew, At.AddMinutes(8));
        var final = next.Confirm(Guid.NewGuid(), supervisor, UserRoleCode.Supervisor, "normal acceptance",
            At.AddMinutes(12), obligation.EffectiveResolutionHeadDecisionId);
        obligation.Resolve(final);
        Assert.Equal(correction.Id, final.PreviousObligationHeadDecisionId);
        Assert.Equal(final.Id, obligation.EffectiveResolutionHeadDecisionId); Assert.Equal(3, obligation.ResolutionHistory.Count);
        Assert.Equal(2, source.Decisions.Count);
    }

    private static void SubmitAndReview(RepairItem item, Guid crew, DateTimeOffset at)
    {
        item.Assign(crew, Pm, UserRoleCode.ProjectManager, at);
        item.Start(crew, at.AddMinutes(1));
        var evidence = new RepairEvidenceReference(Guid.NewGuid(), "file-version", "sha256", RepairEvidencePurpose.After,
            true, true, "ACTUAL_CAPTURE", Guid.NewGuid(), at.AddMinutes(2), null, false);
        item.Submit(RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), "payload-hash", item.Id, item.ObligationId,
            item.ProjectId, item.DefectId, crew, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "frame", null,
            true, null, at.AddMinutes(1), at.AddMinutes(2), at.AddMinutes(2), RepairTimeProvenance.VerifiedOnline, [evidence]));
        item.Review(Pm, UserRoleCode.ProjectManager, at.AddMinutes(3));
    }

    private static (RepairPackage, RepairItem, RepairObligation) Fixture()
    {
        var project = Guid.NewGuid(); var defect = Guid.NewGuid();
        var scope = RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "frame", "road", 0, 10, 0, 1);
        var obligation = RepairObligation.Create(Guid.NewGuid(), project, defect, RepairObligationKind.FormalRepair, true, scope);
        var package = RepairPackage.Create(Guid.NewGuid(), project, defect, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.FastTrack, Pm,
            UserRoleCode.ProjectManager, At, new("old plan", "checklist"));
        package.AddItem(item); return (package, item, obligation);
    }
}
