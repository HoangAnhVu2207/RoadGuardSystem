using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairTaskFactoryTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 7, 0, 0, TimeSpan.Zero);
    [Theory]
    [InlineData(RepairMode.Normal, "NORMAL")]
    [InlineData(RepairMode.FastTrack, "CONDITIONAL_FT")]
    public void AssignedPlannedItemCreatesNativeReporterTaskWithImmutableRepairPin(RepairMode mode, string expected)
    {
        var item = Item(mode, true); var task = Create(item, item.AssignedBy!.Value);
        Assert.Equal(item.Id, task.RepairItemId); Assert.Equal(item.ProjectId, task.ProjectId);
        Assert.Equal(item.DefectId, task.DefectId); Assert.Null(task.SurveyId);
        Assert.Equal("REPORTER", task.SourceKind); Assert.Equal(expected, task.TaskMode);
        Assert.Equal(FieldInspectionPurpose.PostRepair, task.Purpose); Assert.Equal(2, task.LifecycleVersion);
    }
    [Fact]
    public void GenericOperationalTaskNeverAcquiresRepairRightOrItemPin()
    {
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "measure", Guid.NewGuid(), Guid.NewGuid(),
            null, "REPORTER", Guid.NewGuid(), null, null, null, FieldInspectionPurpose.PostRepair, 1, "{}", null,
            At.AddDays(1), Guid.NewGuid());
        Assert.Equal("MEASURE_ONLY", task.TaskMode); Assert.Null(task.RepairItemId);
    }
    [Fact]
    public void UngivenItemCannotCreateAssignedRepairTask()
    {
        var item = Item(RepairMode.Normal, false);
        Assert.Throws<InvalidOperationException>(() => Create(item, item.ProposedBy));
    }
    [Fact]
    public void CallerCannotRelabelAnExistingAssignmentAsItsOwn()
    {
        var item = Item(RepairMode.Normal, true);
        Assert.Throws<InvalidOperationException>(() => Create(item, Guid.NewGuid()));
    }
    [Fact]
    public void HistoricalUnplannedFoundationItemCannotProduceExecutionTask()
    {
        var obligation = Obligation(); var actor = Guid.NewGuid();
        var item = RepairItem.Propose(Guid.NewGuid(), obligation, RepairMode.FastTrack, actor, UserRoleCode.ProjectManager, At);
        item.Assign(Guid.NewGuid(), actor, UserRoleCode.ProjectManager, At.AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => Create(item, actor));
    }
    private static FieldInspectionTask Create(RepairItem item, Guid actor) => FieldInspectionTask.CreateRepair(
        Guid.NewGuid(), "repair", item, null, "REPORTER", Guid.NewGuid(), null, null, null,
        FieldInspectionPurpose.PostRepair, 1, "{}", null, At.AddDays(1), actor);
    private static RepairItem Item(RepairMode mode, bool assigned)
    {
        var actor = Guid.NewGuid(); var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), Obligation(), mode, actor,
            UserRoleCode.ProjectManager, At, new("explicit repair plan", "checklist-v1"));
        if (assigned)
        {
            if (mode == RepairMode.Normal) item.Approve(Guid.NewGuid(), UserRoleCode.Supervisor, At.AddMinutes(1));
            item.Assign(Guid.NewGuid(), actor, UserRoleCode.ProjectManager, At.AddMinutes(2));
        }
        return item;
    }
    private static RepairObligation Obligation() => RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        RepairObligationKind.FormalRepair, true, RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "TEST_FRAME", "road", 0, 10, 0, 1));
}
