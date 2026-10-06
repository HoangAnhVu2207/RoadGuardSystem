using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4CorePersistencePrerequisiteTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    private static RepairObligation Obligation() => RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        RepairObligationKind.FormalRepair, true, RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "actual-frame", "road", 0, 10, 0, 3));
    [Fact]
    public void ProposalRetainsActualPlanAndChecklistWithStableImmutableHash()
    {
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), Obligation(), RepairMode.Normal, Guid.NewGuid(),
            UserRoleCode.ProjectManager, At, new("replace damaged slab", "CHECKLIST-3"));
        Assert.Equal("replace damaged slab", item.RepairPlan); Assert.Equal("CHECKLIST-3", item.ChecklistVersion);
        Assert.Equal(64, item.ProposalPlanHash!.Length); Assert.Null(item.ApprovedPlanHash);
        item.Approve(Guid.NewGuid(), UserRoleCode.Supervisor, At);
        Assert.Equal(item.ProposalPlanHash, item.ApprovedPlanHash);
    }
    [Fact]
    public void DifferentPlanCannotReuseApprovalIdentity()
    {
        var obligation = Obligation(); var pm = Guid.NewGuid();
        var first = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, pm, UserRoleCode.ProjectManager, At, new("repair north edge", "v1"));
        var second = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, pm, UserRoleCode.ProjectManager, At, new("repair south edge", "v1"));
        Assert.NotEqual(first.ProposalPlanHash, second.ProposalPlanHash);
        first.Approve(Guid.NewGuid(), UserRoleCode.Supervisor, At);
        Assert.Null(second.ApprovedPlanHash);
    }
    [Fact]
    public void ActualProposalCannotInventMissingPlanOrChecklist()
    {
        Assert.Throws<ArgumentException>(() => RepairItem.ProposeWithPlan(Guid.NewGuid(), Obligation(), RepairMode.Normal,
            Guid.NewGuid(), UserRoleCode.ProjectManager, At, new("", "v1")));
    }
    [Fact]
    public void DraftHeadAllowsOnlyTechnicalInitialStagingWithoutRequiredFkCycle()
    {
        using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(sql => sql.UseNetTopologySuite()).Options);
        Assert.True(db.Model.FindEntityType(typeof(RepairPolicyDraft))!.FindProperty("CurrentChangeId")!.IsNullable);
    }
    [Theory]
    [InlineData("0.0000004")]
    [InlineData("100000000000000")]
    public void PolicyThresholdCannotSilentlyRoundOrOverflowStoredEligibilityRule(string maximum)
    {
        var value = decimal.Parse(maximum, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(() => RepairPolicyRevision.Publish(Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
            UserRoleCode.ProjectManager, At, "CRACK", "v1", [new("CrackWidth", "mm", 0, value)], []));
    }
}
