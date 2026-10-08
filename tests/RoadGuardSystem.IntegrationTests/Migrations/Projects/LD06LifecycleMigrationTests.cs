using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class LD06LifecycleMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);

    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, options => options.UseNetTopologySuite()).Options);

    [Fact]
    public async Task EmptyActivationDowngradeAndReapplyPreserveExistingProject()
    {
        await using var db = Db();
        await db.GetService<IMigrator>().MigrateAsync();
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Existing project", null, null, null, null, DateTimeOffset.UtcNow);
        db.Add(project); await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Assert.Equal(1, await db.Projects.CountAsync(row => row.Id == project.Id));
        foreach (var trigger in new[] { "TR_LD06LifecycleActions_Immutable", "TR_LD06LifecycleActions_Scope",
            "TR_ObligationResponsibilities_Scope", "TR_LD06ActionEvidence_Immutable", "TR_ProjectLifecycleHistory_Production" })
            Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE name={trigger} AND is_disabled=0").SingleAsync());
        await db.GetService<IMigrator>().MigrateAsync();
        Assert.Equal(1, await db.Projects.CountAsync(row => row.Id == project.Id));
        await db.Database.MigrateAsync();
        Assert.Equal(1, await db.Projects.CountAsync(row => row.Id == project.Id));
    }

    [Fact]
    public async Task RecordedActivationIsImmutableAndBlocksDestructiveDowngrade()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        var action = await ControlledSchemaAction(db, LD06ActionKind.DeclareConstruction);
        var update = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE LD06LifecycleActions SET Reason='rewrite' WHERE Id={action.Id}"));
        Assert.Equal(51600, update.Number);
        var remove = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM LD06LifecycleActions WHERE Id={action.Id}"));
        Assert.Equal(51600, remove.Number);

        Assert.True(await db.Set<LD06LifecycleAction>().AnyAsync(row => row.Id == action.Id));
    }

    [Fact]
    public async Task ProductionClosureMirrorCannotInventOrBorrowAnAction()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        // Controlled persisted schema facts prove relational guards, not production command acceptance.
        var action = await ControlledSchemaAction(db, LD06ActionKind.OperationalClose);
        db.Add(ProjectLifecycleHistoryRecord.RecordProductionClosure(action)); await db.SaveChangesAsync();
        var forged = LD06LifecycleAction.Create(Guid.NewGuid(), action.ProjectId, action.ActorId,
            LD06ActionKind.OperationalClose, action.At, "uncommitted action", "{}");
        db.Add(ProjectLifecycleHistoryRecord.RecordProductionClosure(forged));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(51604, Assert.IsType<SqlException>(error.InnerException).Number);
        db.ChangeTracker.Clear();
        Assert.Single(await db.Set<ProjectLifecycleHistoryRecord>().Where(row => row.ProjectId == action.ProjectId).ToArrayAsync());
    }

    private static async Task<LD06LifecycleAction> ControlledSchemaAction(RoadGuardDbContext db, LD06ActionKind kind)
    {
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Controlled schema facts", null, null, null, null, now);
        if (!await db.Roles.AnyAsync(row => row.Code == UserRoleCode.Supervisor))
            db.Add(new ApplicationRole(UserRoleCode.Supervisor, "Supervisor"));
        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = Guid.NewGuid().ToString(),
            DisplayName = "Controlled schema actor",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Supervisor,
            CreatedAt = now
        };
        db.AddRange(project, actor); await db.SaveChangesAsync();
        var action = LD06LifecycleAction.Create(Guid.NewGuid(), project.Id, actor.Id, kind, now, "Schema invariant only", "{}");
        db.Add(action); await db.SaveChangesAsync();
        return action;
    }
}
