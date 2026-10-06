using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class H7RepairInventoryTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    private static readonly Guid Project = Guid.NewGuid(), Defect = Guid.NewGuid(), Pm = Guid.NewGuid(), Supervisor = Guid.NewGuid(), Crew = Guid.NewGuid();

    [Theory]
    [InlineData(RepairMode.Normal)]
    [InlineData(RepairMode.FastTrack)]
    public void CorrectedResultChangesNewInventoryButPreservesOldCaptureAndPerformedHistory(RepairMode mode)
    {
        var source = Confirmed(mode); var accepted = source.Item.EffectiveDecision!;
        var old = RepairReportingInventory.Capture(Project, [source]);
        Correct(source, RepairPresentationState.Unrepaired, At.AddDays(40));
        var current = RepairReportingInventory.Capture(Project, [source]);
        Assert.Equal(1, old.SuppliedConfirmedItemCount); Assert.Equal(0, current.SuppliedConfirmedItemCount);
        var row = Assert.Single(current.Items);
        Assert.Equal(accepted.Id, row.OriginalDecisionId); Assert.Equal(source.Item.Attempts[0].Id, row.OriginalPerformedAttemptId);
        Assert.Equal(source.Item.EffectiveDecision!.Id, row.EffectiveDecisionId);
        Assert.False(row.ObligationResolved); Assert.Equal(2, row.Decisions.Count);
        Assert.Equal(accepted.Id, row.Decisions[1].SupersedesId); Assert.Equal("independent observed basis", row.Decisions[1].Basis);
        Assert.Single(old.Items[0].Decisions); Assert.True(old.Items[0].ObligationResolved);
    }

    [Fact]
    public void SharedSegmentJoinsCountOneItemWithoutAllocatingItsCountAcrossSegments()
    {
        var source = Confirmed(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var inventory = RepairReportingInventory.Capture(Project,
            [source with { LocationReferenceIds = [first] }, source with { LocationReferenceIds = [second, first] }]);
        Assert.Equal(1, inventory.SuppliedItemCount); Assert.Equal(1, inventory.SuppliedConfirmedItemCount);
        Assert.Equal(new[] { first, second }.Order().ToArray(), Assert.Single(inventory.Items).LocationReferenceIds);
    }

    [Fact]
    public void DistinctItemsInSameDefectAreNotCollapsedIntoOneDefectCount()
    {
        var a = Confirmed(); var b = Confirmed();
        var inventory = RepairReportingInventory.Capture(Project, [a, b]);
        Assert.Equal(2, inventory.SuppliedItemCount); Assert.Equal(2, inventory.SuppliedConfirmedItemCount);
    }

    [Fact]
    public void ConflictingSameItemCopiesCannotBeSilentlyDistinctByFirstRow()
    {
        var source = Confirmed(); var oldCopy = Confirmed(id: source.Item.Id);
        Correct(source, RepairPresentationState.Unrepaired, At.AddHours(5));
        Assert.Throws<InvalidOperationException>(() => RepairReportingInventory.Capture(Project, [oldCopy, source]));
    }

    [Fact]
    public void ReviewRequestDoesNotChangeEffectiveInventory()
    {
        var source = Confirmed(); var id = source.Item.EffectiveDecision!.Id;
        source.Item.RequestReview(Guid.NewGuid(), Crew, UserRoleCode.RepairCrew, "request only", At.AddHours(5));
        var inventory = RepairReportingInventory.Capture(Project, [source]);
        Assert.Equal(1, inventory.SuppliedConfirmedItemCount); Assert.Equal(id, inventory.Items[0].EffectiveDecisionId);
        Assert.Single(inventory.Items[0].Decisions);
    }

    [Fact]
    public void UnacceptedClaimDoesNotBecomeConfirmed()
    {
        var source = Confirmed(confirm: false);
        var row = Assert.Single(RepairReportingInventory.Capture(Project, [source]).Items);
        Assert.Equal(RepairPresentationState.ReportedAwaitingReview, row.Presentation);
        Assert.Null(row.EffectiveDecisionId); Assert.Null(row.OriginalDecisionId); Assert.Empty(row.Decisions);
    }

    [Fact]
    public void ScopeMismatchIsRejected()
    {
        var source = Confirmed();
        Assert.Throws<InvalidOperationException>(() => RepairReportingInventory.Capture(Guid.NewGuid(), [source]));
        Assert.Throws<InvalidOperationException>(() => RepairReportingInventory.Capture(Project, [source with { Obligation = Confirmed().Obligation }]));
    }

    [Fact]
    public void InvalidSourceAndLocationCannotEstablishInventory()
    {
        Assert.Throws<ArgumentException>(() => RepairReportingInventory.Capture(Guid.Empty, []));
        Assert.Throws<ArgumentNullException>(() => RepairReportingInventory.Capture(Project, null!));
        var source = Confirmed();
        Assert.Throws<ArgumentException>(() => RepairReportingInventory.Capture(Project, [source with { LocationReferenceIds = [Guid.Empty] }]));
    }

    [Fact]
    public void CaptureCopiesCallerLocationsAndDecisionEvidence()
    {
        var source = Confirmed(); var locations = new[] { Guid.NewGuid() }; var original = locations[0];
        Correct(source, RepairPresentationState.ReportedAwaitingReview, At.AddHours(5));
        var capture = RepairReportingInventory.Capture(Project, [source with { LocationReferenceIds = locations }]);
        locations[0] = Guid.NewGuid();
        Assert.Equal(original, capture.Items[0].LocationReferenceIds[0]); Assert.Single(capture.Items[0].Decisions[1].EvidenceFileIds);
        Correct(source, RepairPresentationState.Confirmed, At.AddHours(6));
        Assert.Equal(2, capture.Items[0].Decisions.Count); Assert.Equal(0, capture.SuppliedConfirmedItemCount);
    }

    private static RepairReportingSource Confirmed(RepairMode mode = RepairMode.Normal, Guid? id = null, bool confirm = true)
    {
        var scope = RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "observed-location-v1", "fixture", 0, 10, -2, 2);
        var obligation = RepairObligation.Create(Guid.NewGuid(), Project, Defect, RepairObligationKind.FormalRepair, true, scope);
        var item = RepairItem.Propose(id ?? Guid.NewGuid(), obligation, mode, Pm, UserRoleCode.ProjectManager, At);
        if (mode == RepairMode.Normal) item.Approve(Supervisor, UserRoleCode.Supervisor, At);
        item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At); item.Start(Crew, At);
        var after = new RepairEvidenceReference(Guid.NewGuid(), "file-v1", "hash", RepairEvidencePurpose.After, true, true, "FIELD", Guid.NewGuid(), At.AddHours(1), null, false);
        item.Submit(RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), "payload-hash", item.Id, obligation.Id, Project, Defect,
            Crew, Guid.NewGuid(), Guid.NewGuid(), mode == RepairMode.FastTrack ? Guid.NewGuid() : null, "location-v1", null,
            true, null, At, At.AddHours(1), At.AddHours(2), RepairTimeProvenance.VerifiedOnline, [after]));
        if (confirm)
        {
            item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(3));
            obligation.Resolve(item.Confirm(Guid.NewGuid(), mode == RepairMode.Normal ? Supervisor : Pm,
                mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager, "accepted", At.AddHours(4)));
        }
        return new(item, obligation, []);
    }

    [Theory]
    [InlineData(RepairMode.Normal, UserRoleCode.Supervisor)]
    [InlineData(RepairMode.FastTrack, UserRoleCode.ProjectManager)]
    public void CaptureKeepsActualDecisionRoleInsteadOfInferringAuthority(RepairMode mode, UserRoleCode role)
    {
        var source = Confirmed(mode); Correct(source, RepairPresentationState.Unrepaired, At.AddHours(5));
        var row = Assert.Single(RepairReportingInventory.Capture(Project, [source]).Items);
        Assert.All(row.Decisions, decision => Assert.Equal(role, decision.Role));
    }

    [Fact]
    public void CorrectionBasisKeepsImmutableFileVersionHashAndSourceProvenance()
    {
        var source = Confirmed(); Correct(source, RepairPresentationState.Unrepaired, At.AddHours(5));
        var basis = Assert.Single(source.Item.EffectiveDecision!.Basis!.Evidence);
        var row = Assert.Single(RepairReportingInventory.Capture(Project, [source]).Items);
        Assert.Equal(basis, Assert.Single(row.Decisions[1].EvidenceFacts));
        Assert.Equal("file-v1", row.Decisions[1].EvidenceFacts[0].FileVersion);
        Assert.Equal("FIELD", row.Decisions[1].EvidenceFacts[0].SourceKind);
    }
    private static void Correct(RepairReportingSource source, RepairPresentationState result, DateTimeOffset at)
    {
        var role = source.Item.Mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager;
        var actor = Guid.NewGuid();
        var basis = RepairCorrectionBasis.Create("independent observed basis", [new(Guid.NewGuid(), "file-v1", "hash",
            RepairEvidencePurpose.Before, true, true, "FIELD", Guid.NewGuid(), at, null, false)]);
        RepairCorrectionEffects.Apply(source.Item, source.Obligation, Guid.NewGuid(), source.Item.EffectiveDecision!.Id,
            actor, role, "correction", at, new(Guid.NewGuid(), source.Item.Id, actor, role, "fixture current authority"), result, basis);
    }
}
