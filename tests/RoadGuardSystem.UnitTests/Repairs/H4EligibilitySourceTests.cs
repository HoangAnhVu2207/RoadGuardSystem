using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Warranties;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4EligibilitySourceTests
{
    private static readonly Guid Project = Guid.NewGuid(), RoadSection = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 10, 6);
    [Fact]
    public void ActualActiveProjectWarrantyAndAcceptedDocumentDoNotInventRoadCoverageMapping()
    {
        var doc = HandoverDocument.Create(Guid.NewGuid(), Project, "fixture-handover", Day, Guid.NewGuid(), null, null);
        var warranty = Warranty.Create(Guid.NewGuid(), Project, null, doc.Id, Day, Day, Day.AddDays(30), null,
            WarrantyScope.Project, null, null, WarrantyStatus.Active);
        var snapshot = RepairEligibilitySourceSnapshot.Capture(Project, RoadSection, [warranty], [doc]);
        Assert.Equal(RepairFactState.Unknown, snapshot.RoadHandover); Assert.Equal(RepairFactState.Unknown, snapshot.Coverage);
        Assert.Equal(warranty.Id, Assert.Single(snapshot.Warranties).Id); Assert.Equal(doc.Id, Assert.Single(snapshot.Handovers).Id);
        Assert.Equal("UNKNOWN_OWNER_MAPPING", snapshot.SourceMapping);
    }
    [Fact]
    public void SourceSnapshotRejectsCrossProjectOrDifferentRoadRawRows()
    {
        var other = Warranty.Create(Guid.NewGuid(), Project, Guid.NewGuid(), null, Day, Day, Day, null, WarrantyScope.RoadSection, null, null, WarrantyStatus.Active);
        Assert.Throws<InvalidOperationException>(() => RepairEligibilitySourceSnapshot.Capture(Project, RoadSection, [other], []));
        var doc = HandoverDocument.Create(Guid.NewGuid(), Guid.NewGuid(), "other project", Day, Guid.NewGuid(), null, null);
        Assert.Throws<InvalidOperationException>(() => RepairEligibilitySourceSnapshot.Capture(Project, RoadSection, [], [doc]));
    }
    [Fact]
    public void MutableProjectDocumentCannotRewriteCapturedSourceVersionOrActor()
    {
        var actor = Guid.NewGuid(); var doc = HandoverDocument.Create(Guid.NewGuid(), Project, "original", Day, actor, null, null);
        doc.RowVersion = [1, 2, 3];
        var snapshot = RepairEligibilitySourceSnapshot.Capture(Project, RoadSection, [], [doc]);
        doc.DocumentNo = "changed"; doc.AcceptedByUserId = Guid.NewGuid(); doc.RowVersion[0] = 9;
        var captured = Assert.Single(snapshot.Handovers); Assert.Equal("original", captured.DocumentNo);
        Assert.Equal(actor, captured.AcceptedByUserId); Assert.Equal(Convert.ToBase64String(new byte[] { 1, 2, 3 }), captured.RowVersion);
    }
    [Fact]
    public void AbsentActualSourcesRemainUnknownWithoutFallbackWarrantyOrHandover()
    {
        var snapshot = RepairEligibilitySourceSnapshot.Capture(Project, RoadSection, [], []);
        Assert.Empty(snapshot.Warranties); Assert.Empty(snapshot.Handovers);
        Assert.Equal(RepairFactState.Unknown, snapshot.Coverage); Assert.Equal(RepairFactState.Unknown, snapshot.RoadHandover);
    }
}
