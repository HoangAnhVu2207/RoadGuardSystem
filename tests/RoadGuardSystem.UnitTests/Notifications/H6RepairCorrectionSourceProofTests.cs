using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6RepairCorrectionSourceProofTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    [Fact]
    public void CorrectionMustMatchActualImmutableDecisionAndResolutionHistory()
    {
        var (item, obligation, correction) = Corrected();
        Assert.True(NotificationRepairCorrectionProof.Verify(Claim(item, correction), item, obligation, correction));
    }
    [Fact]
    public void LaterCorrectionDoesNotInvalidateAGenuineEarlierEventInTheActualHeadChain()
    {
        var (item, obligation, correction) = Corrected();
        RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), correction.Id, correction.ActorId, correction.Role,
            "later authoritative correction", At.AddHours(5), new(Guid.NewGuid(), item.Id, correction.ActorId, correction.Role, "fixture authority"),
            RepairPresentationState.Confirmed, RepairCorrectionBasis.Create("later basis", []));
        Assert.True(NotificationRepairCorrectionProof.Verify(Claim(item, correction), item, obligation, correction));
    }
    [Fact]
    public void PayloadCannotReplaceActualSupersededDecision()
    {
        var (item, obligation, correction) = Corrected();
        Assert.False(NotificationRepairCorrectionProof.Verify(Claim(item, correction) with { SupersedesDecisionId = Guid.NewGuid() }, item, obligation, correction));
    }
    [Fact]
    public void PayloadCannotReplaceActualCorrectedResultOrTimestamp()
    {
        var (item, obligation, correction) = Corrected(); var claim = Claim(item, correction);
        Assert.False(NotificationRepairCorrectionProof.Verify(claim with { Result = RepairPresentationState.Confirmed }, item, obligation, correction));
        Assert.False(NotificationRepairCorrectionProof.Verify(claim with { OccurredAtUtc = At }, item, obligation, correction));
    }
    [Fact]
    public void DecisionRowAloneCannotProveAnotherObligationHistoryOrProject()
    {
        var (item, obligation, correction) = Corrected();
        var other = RepairObligation.Create(Guid.NewGuid(), item.ProjectId, item.DefectId, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "sample", "road", 0, 10, 0, 3));
        Assert.False(NotificationRepairCorrectionProof.Verify(Claim(item, correction), item, other, correction));
        Assert.False(NotificationRepairCorrectionProof.Verify(Claim(item, correction) with { ProjectId = Guid.NewGuid() }, item, obligation, correction));
        Assert.Equal(2, obligation.ResolutionHistory.Count);
    }
    private static NotificationRepairCorrectionClaim Claim(RepairItem item, RepairDecision correction)
        => new(correction.Id, item.ProjectId, item.Id, correction.ObligationId, correction.SupersedesDecisionId!.Value, correction.Result, correction.At);
    private static (RepairItem, RepairObligation, RepairDecision) Corrected()
    {
        var pm = Guid.NewGuid(); var crew = Guid.NewGuid();
        var obligation = RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "sample", "road", 0, 10, 0, 3));
        var item = RepairItem.Propose(Guid.NewGuid(), obligation, RepairMode.FastTrack, pm, UserRoleCode.ProjectManager, At);
        item.Assign(crew, pm, UserRoleCode.ProjectManager, At); item.Start(crew, At.AddMinutes(1));
        item.Submit(RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), item.Id, obligation.Id,
            item.ProjectId, item.DefectId, crew, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "sample", Guid.NewGuid(), true, null,
            At.AddMinutes(1), At.AddHours(1), At.AddHours(1), RepairTimeProvenance.VerifiedOnline,
            [new(Guid.NewGuid(), "v1", new string('a', 64), RepairEvidencePurpose.After, true, true, "CREW_CAPTURE", Guid.NewGuid(), At.AddHours(1), null, false)]));
        item.Review(pm, UserRoleCode.ProjectManager, At.AddHours(2));
        var initial = item.Confirm(Guid.NewGuid(), pm, UserRoleCode.ProjectManager, "confirmed", At.AddHours(3)); obligation.Resolve(initial);
        var correction = RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), initial.Id, pm, UserRoleCode.ProjectManager,
            "correct scope", At.AddHours(4), new(Guid.NewGuid(), item.Id, pm, UserRoleCode.ProjectManager, "fixture authority"),
            RepairPresentationState.Unrepaired, RepairCorrectionBasis.Create("source basis", []));
        return (item, obligation, correction);
    }
}
