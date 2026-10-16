using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed partial class H4RepairExecutionSqlTests
{
    private static Task<FieldWorkflowResultFact> Bootstrap(RoadGuardDbContext db, Ready state,
        Guid? actor = null, Guid? project = null, UserRoleCode role = UserRoleCode.RepairCrew)
    {
        var caller = actor ?? state.Source.Crew;
        var scope = project ?? state.Source.Project;
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        return new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System)
            .ExecuteAsync(new(scope, state.Binding.TaskId, "get", null, null, null,
                new(caller, role, caller, "DIRECT", true)),
                async ct => await guard.AuthorizeAsync(caller, role, scope, ct) is not null, default);
    }

    [Fact]
    public async Task CrewBootstrapSuppliesItemVersionForCommandAndRefreshWithoutPmRead()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var read = await Bootstrap(db, state); Assert.Equal(200, read.Status);
        var task = Assert.IsType<FieldTaskViewFact>(read.Value);
        var context = Assert.IsType<CrewRepairContextFact>(task.RepairContext);
        Assert.Equal(state.Package.Id, context.PackageId); Assert.Equal(state.Item.Id, context.ItemId);
        Assert.Equal(state.Binding.Id, context.BindingId); Assert.Equal("NORMAL", context.Mode);
        Assert.Equal(state.Binding.AssignmentId, task.AssignmentId);
        Assert.Equal(Version(db, state.Item), context.ItemVersion);
        Assert.NotEqual(task.Version, context.ItemVersion);
        FieldTaskView wire = task;
        FieldTaskViewFact roundTrip = wire;
        Assert.Equal(task, roundTrip);
        var command = new RoadGuardSystem.Repositories.Repairs.RepairAssessmentCommand(state.Source.Crew,
            UserRoleCode.RepairCrew, state.Source.Project, context.PackageId, context.ItemId, task.Id,
            new(Guid.NewGuid(), state.FirstStart.Id, null, null, null), Guid.NewGuid().ToString(), context.ItemVersion);
        var result = await Repo(db).AssessAsync(command, default); Assert.Equal(201, result.Status);
        db.ChangeTracker.Clear();
        var fresh = Assert.IsType<FieldTaskViewFact>((await Bootstrap(db, state)).Value);
        Assert.Equal(result.Version, fresh.RepairContext!.ItemVersion);
        Assert.NotEqual(context.ItemVersion, fresh.RepairContext.ItemVersion);
        var stale = await Repo(db).AssessAsync(command with
        {
            Key = Guid.NewGuid().ToString(),
            Input = new(Guid.NewGuid(), state.FirstStart.Id, null, null, null)
        }, default);
        Assert.Equal(409, stale.Status); Assert.Equal("concurrency_conflict", stale.Code);
        Assert.Equal(403, (await Repo(db).ReadItemAsync(state.Source.Crew, UserRoleCode.RepairCrew,
            state.Source.Project, state.Package.Id, state.Item.Id, false, default)).Status);
        Assert.Null(Assert.IsType<FieldTaskViewFact>((await Bootstrap(db, state,
            state.Source.Pm, role: UserRoleCode.ProjectManager)).Value).RepairContext);
    }

    [Fact]
    public async Task CrewBootstrapRejectsUnassignedActorAndWrongProject()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var other = await AssignedAndStarted(db);
        Assert.Equal(403, (await Bootstrap(db, state, other.Source.Crew)).Status);
        Assert.Equal(403, (await Bootstrap(db, state, project: other.Source.Project)).Status);
    }

    [Fact]
    public async Task CrewBootstrapRejectsRevokedMembership()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var member = await db.ProjectMembers.SingleAsync(x => x.ProjectId == state.Source.Project && x.UserId == state.Source.Crew);
        member.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Bootstrap(db, state)).Status);
    }

    [Fact]
    public async Task CrewBootstrapRejectsEndedAssignment()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var assignment = await db.FieldInspectionAssignments.SingleAsync(x => x.Id == state.Binding.AssignmentId);
        assignment.End(DateTimeOffset.UtcNow, "isolated regression fixture end");
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await Bootstrap(db, state)).Status);
    }

    [Fact]
    public async Task CrewBootstrapRejectsSupersededBindingAfterNormalContinuation()
    {
        await using var db = sql.CreateDbContext(); var state = await AssignedAndStarted(db);
        var result = await Repo(db).ContinueNormallyAsync(new(state.Source.Pm, UserRoleCode.ProjectManager,
            state.Source.Project, state.Package.Id, state.Item.Id,
            new("replacement plan", "checklist-v1", "supersede old binding",
                new(state.FirstStart.Id, state.Source.Pm, "measurement retained; no repair claimed", "no safety work claimed")),
            Guid.NewGuid().ToString(), Version(db, state.Item)), default);
        Assert.True(result.Status == 201, $"{result.Status}/{result.Code}"); db.ChangeTracker.Clear();
        Assert.NotNull((await db.Set<RepairItem>().SingleAsync(x => x.Id == state.Item.Id)).SupersededByItemId);
        Assert.Equal(403, (await Bootstrap(db, state)).Status);
        Assert.Null(Assert.IsType<FieldTaskViewFact>((await Bootstrap(db, state,
            state.Source.Pm, role: UserRoleCode.ProjectManager)).Value).RepairContext);
    }
}
