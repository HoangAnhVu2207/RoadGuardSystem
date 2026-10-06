using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6RepairAssignmentProofTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 0, 0, 0, TimeSpan.Zero);
    [Fact]
    public void ActualAssignmentEventBindsNativeTaskAndImmutableCrewSource()
    {
        var f = Facts(); Assert.True(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event, f.Binding, f.Task, f.Assignment, f.Crew));
        // The actual H4 producer omits this optional hint; the exact binding remains authoritative.
        Assert.True(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event, f.Binding, f.Task, f.Assignment, null));
    }
    [Fact]
    public void HistoricalAssignmentEventRemainsGenuineAfterHandoverWithoutGrantingFormerCrew()
    {
        var f = Facts(); f.Assignment.End(At.AddMinutes(1), "actual subsequent handover");
        Assert.True(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event, f.Binding, f.Task, f.Assignment, f.Crew));
    }
    [Fact]
    public void DeclaredCrewOrUnrelatedTaskBindingCannotBorrowAnActualEvent()
    {
        var f = Facts();
        Assert.False(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event, f.Binding, f.Task, f.Assignment, Guid.NewGuid()));
        Assert.False(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event, f.Binding with { TaskId = Guid.NewGuid() }, f.Task, f.Assignment, f.Crew));
        Assert.False(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event with { BindingId = Guid.NewGuid() }, f.Binding, f.Task, f.Assignment, f.Crew));
        Assert.False(NotificationRepairLifecycleProof.VerifyAssignment(f.Claim, f.Item, f.Event, f.Binding with { RouteVersionId = Guid.NewGuid() }, f.Task, f.Assignment, f.Crew));
    }
    private sealed record Fixture(RepairItem Item, RepairItemLifecycleEvent Event, RepairFieldTaskBinding Binding,
        FieldInspectionTask Task, FieldInspectionAssignment Assignment, NotificationRepairLifecycleClaim Claim, Guid Crew);
    private static Fixture Facts()
    {
        var obligation = RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), Guid.NewGuid(), "actual-frame", "road", 0, 10, 0, 3));
        var pm = Guid.NewGuid(); var crew = Guid.NewGuid(); var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation,
            RepairMode.Normal, pm, UserRoleCode.ProjectManager, At, new("actual plan", "checklist-v1"));
        item.Approve(Guid.NewGuid(), UserRoleCode.Supervisor, At); item.Assign(crew, pm, UserRoleCode.ProjectManager, At);
        var route = Guid.NewGuid(); var task = FieldInspectionTask.CreateRepair(Guid.NewGuid(), "actual-task", item, null,
            "REPORTER", route, Guid.NewGuid(), null, null, FieldInspectionPurpose.PostRepair, 1, "{}", null, At.AddDays(1), pm);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, crew, pm, At, null, FieldInspectionAssignmentStatus.Active, null);
        var binding = new RepairFieldTaskBinding(Guid.NewGuid(), item.ProjectId, item.DefectId, item.ObligationId, item.Id,
            task.Id, assignment.Id, crew, item.Mode, null, null, null, item.ProposalPlanHash!, item.ChecklistVersion!, route,
            task.SegmentSetId, null, null, null, null, "actual-frame", pm, At, "actual assignment", "task-version", "assignment-version");
        var source = new RepairItemLifecycleEvent(Guid.NewGuid(), item.ProjectId, item.DefectId, item.ObligationId, item.Id,
            item.Mode, "ASSIGNED", pm, UserRoleCode.ProjectManager, "actual assignment", At, binding.Id, null, null, null, null, "actual-version");
        return new(item, source, binding, task, assignment,
            new(item.ProjectId, item.Id, source.Id, source.Id, "repair.work.assigned.v1", "ASSIGNED", At), crew);
    }
}
