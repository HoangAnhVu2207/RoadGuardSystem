using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6RepairLifecycleProofTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    [Fact]
    public void ActualNormalProposalSourceHasCurrentSupervisorDutyWithoutInventedCrew()
    {
        var (item, source, claim) = Facts(RepairMode.Normal);
        Assert.True(NotificationRepairLifecycleProof.VerifyProposal(claim, item, source));
    }
    [Fact]
    public void ConditionalFastTrackProposalCannotBeRelabelledAsNormalApprovalRequest()
    {
        var (item, source, claim) = Facts(RepairMode.FastTrack);
        Assert.False(NotificationRepairLifecycleProof.VerifyProposal(claim, item, source));
    }
    [Fact]
    public void ProposalNotificationCannotBorrowAnotherProjectRevisionActionActorRoleOrTime()
    {
        var (item, source, claim) = Facts(RepairMode.Normal);
        Assert.False(NotificationRepairLifecycleProof.VerifyProposal(claim with { ProjectId = Guid.NewGuid() }, item, source));
        Assert.False(NotificationRepairLifecycleProof.VerifyProposal(claim with { RevisionId = Guid.NewGuid() }, item, source));
        Assert.False(NotificationRepairLifecycleProof.VerifyProposal(claim, item, source with { Kind = "APPROVED" }));
        Assert.False(NotificationRepairLifecycleProof.VerifyProposal(claim, item, source with { Role = UserRoleCode.RepairCrew }));
        Assert.False(NotificationRepairLifecycleProof.VerifyProposal(claim with { OccurredAtUtc = At.AddHours(1) }, item, source));
    }
    private static (RepairItem Item, RepairItemLifecycleEvent Source, NotificationRepairLifecycleClaim Claim) Facts(RepairMode mode)
    {
        var obligation = RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "actual-frame", "road", 0, 10, 0, 3));
        var actor = Guid.NewGuid(); var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, mode, actor,
            UserRoleCode.ProjectManager, At, new("actual plan", "checklist-v1"));
        var source = new RepairItemLifecycleEvent(Guid.NewGuid(), item.ProjectId, item.DefectId, item.ObligationId, item.Id,
            item.Mode, "PROPOSED", actor, UserRoleCode.ProjectManager, "actual proposal", At, null, null, null, null, null, "actual-version");
        return (item, source, new(item.ProjectId, item.Id, source.Id, source.Id,
            "review.supervisor_required.v1", "SUPERVISOR_REQUIRED", At));
    }
}
