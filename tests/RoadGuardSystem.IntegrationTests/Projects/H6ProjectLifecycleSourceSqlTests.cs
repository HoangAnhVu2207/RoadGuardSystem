using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.BusinessObjects.Auditing;
using Microsoft.Data.SqlClient;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class H6ProjectLifecycleSourceSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task CandidateClosureCannotActivateConfirmedRenewedScope()
    {
        var source = await Seed(); await using var db = sql.CreateDbContext();
        var closure = ProjectLifecycleHistoryRecord.RecordCandidate(Guid.NewGuid(), source.Project,
            ProjectLifecycleFactKind.OperationalClosure, source.Actor, DateTimeOffset.UtcNow, "candidate only", "fixture, no owner authority", "{}");
        db.Add(closure); await db.SaveChangesAsync();
        var repository = new ProjectLifecycleRepository(db, TimeProvider.System);
        var view = await repository.ReadAsync(source.Actor, source.Project, default); Assert.NotNull(view);
        var result = await repository.RenewAsync(new(source.Actor, UserRoleCode.Supervisor, source.Project,
            new(closure.Id, "new intake", "handling scope", "test basis"), Guid.NewGuid().ToString(), view.Version), default);
        Assert.Equal(409, result.Status); Assert.Equal("operational_closure_source_unavailable", result.Code);
        Assert.Equal("UNKNOWN", view.OperationalClosure); Assert.Single(view.History);
    }

    [Fact]
    public async Task ActualRenewedScopeAppendsAtomicallyAndReplayRequiresCurrentSupervisorAuthority()
    {
        var source = await Seed(); await using var db = sql.CreateDbContext();
        var closure = await ControlledClosure(db, source);
        var repository = new ProjectLifecycleRepository(db, TimeProvider.System);
        var original = await repository.ReadAsync(source.Actor, source.Project, default); Assert.NotNull(original);
        var command = new ProjectRenewedHandlingCommand(source.Actor, UserRoleCode.Supervisor, source.Project,
            new(closure.Id, "new report after closure", "inspect the new reported scope", "controlled prior closure, no generic close permission"),
            Guid.NewGuid().ToString(), original.Version);
        var created = await repository.RenewAsync(command, default); Assert.Equal(201, created.Status); Assert.NotNull(created.Value);
        Assert.True(created.Value.AcceptsNewReports); Assert.Equal("UNKNOWN", created.Value.OperationalClosure);
        Assert.NotEqual(original.Version, created.Value.Version); Assert.Equal(2, created.Value.History.Length);
        Assert.Equal(command.Input.HandlingScope, created.Value.History.Single(row => row.Kind == "RenewedHandlingScope").HandlingScope);
        Assert.Single(await db.Set<AuditLog>().Where(row => row.EventType == "project_renewed_handling_scope" && row.EntityId == source.Project).ToArrayAsync());
        Assert.Single(await db.IdempotencyRecords.Where(row => row.ActorUserId == source.Actor && row.IdempotencyKey == command.Key).ToArrayAsync());
        var replay = await repository.RenewAsync(command, default); Assert.Equal(200, replay.Status); Assert.Equal(created.Value.Version, replay.Value!.Version);
        var conflict = await repository.RenewAsync(command with { Input = command.Input with { Reason = "different reason" } }, default);
        Assert.Equal(409, conflict.Status); Assert.Equal("idempotency_key_reused", conflict.Code);
        await db.ProjectMembers.Where(row => row.UserId == source.Actor && row.ProjectId == source.Project)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await repository.RenewAsync(command, default)).Status);
        Assert.Equal(403, (await repository.RenewAsync(command with { Input = command.Input with { Reason = "another reason" } }, default)).Status);
        Assert.Equal(2, await db.Set<ProjectLifecycleHistoryRecord>().CountAsync(row => row.ProjectId == source.Project));
    }

    [Fact]
    public async Task TwoRenewedDecisionsCannotConsumeTheSameProjectionVersion()
    {
        var source = await Seed(); await using var seed = sql.CreateDbContext(); var closure = await ControlledClosure(seed, source);
        var view = await new ProjectLifecycleRepository(seed, TimeProvider.System).ReadAsync(source.Actor, source.Project, default); Assert.NotNull(view);
        async Task<ProjectLifecycleWriteResult> Run(string key)
        {
            await using var db = sql.CreateDbContext(); return await new ProjectLifecycleRepository(db, TimeProvider.System).RenewAsync(
                new(source.Actor, UserRoleCode.Supervisor, source.Project, new(closure.Id, "renew", "bounded scope", "controlled fixture"), key, view.Version), default);
        }
        var outcomes = await Task.WhenAll(Run(Guid.NewGuid().ToString()), Run(Guid.NewGuid().ToString()));
        Assert.Single(outcomes.Where(row => row.Status == 201)); Assert.Single(outcomes.Where(row => row.Status == 409));
        Assert.Single(await seed.Set<ProjectLifecycleHistoryRecord>().Where(row => row.ProjectId == source.Project && row.Kind == ProjectLifecycleFactKind.RenewedHandlingScope).ToArrayAsync());
    }

    [Fact]
    public async Task AuditFailureRollsBackRenewedHistoryAndReceiptWithoutRewritingOriginalSource()
    {
        var source = await Seed(); await using var db = sql.CreateDbContext(); var closure = await ControlledClosure(db, source);
        var repository = new ProjectLifecycleRepository(db, TimeProvider.System);
        var view = await repository.ReadAsync(source.Actor, source.Project, default); Assert.NotNull(view);
        var key = Guid.NewGuid().ToString();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_TEST_LifecycleAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE EventType='project_renewed_handling_scope') THROW 51599, 'Controlled audit failure', 1; END");
        try
        {
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => repository.RenewAsync(new(source.Actor, UserRoleCode.Supervisor,
                source.Project, new(closure.Id, "renew", "scope", "rollback fixture"), key, view.Version), default));
            Assert.Equal(51599, Assert.IsType<SqlException>(error.InnerException).Number);
        }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_TEST_LifecycleAuditFailure"); db.ChangeTracker.Clear(); }
        Assert.Single(await db.Set<ProjectLifecycleHistoryRecord>().Where(row => row.ProjectId == source.Project).ToArrayAsync());
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == source.Actor && row.IdempotencyKey == key));
        Assert.False(await db.AuditLogs.AnyAsync(row => row.EntityId == source.Project && row.EventType == "project_renewed_handling_scope"));
        var retained = await repository.ReadAsync(source.Actor, source.Project, default); Assert.NotNull(retained); Assert.Equal(view.Version, retained.Version);
    }

    [Fact]
    public async Task CandidateHistoryIsImmutableEvenThoughItGrantsNoBusinessEffect()
    {
        var source = await Seed(); await using var db = sql.CreateDbContext();
        var record = ProjectLifecycleHistoryRecord.RecordCandidate(Guid.NewGuid(), source.Project, ProjectLifecycleFactKind.ConstructionCompletion,
            source.Actor, DateTimeOffset.UtcNow, "candidate history", "test source", "{}"); db.Add(record); await db.SaveChangesAsync();
        var update = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectLifecycleHistory SET Reason='rewritten' WHERE Id={record.Id}"));
        Assert.Equal(51500, update.Number);
        var delete = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ProjectLifecycleHistory WHERE Id={record.Id}"));
        Assert.Equal(51500, delete.Number);
        db.ChangeTracker.Clear(); Assert.Equal("candidate history", (await db.Set<ProjectLifecycleHistoryRecord>().SingleAsync(row => row.Id == record.Id)).Reason);
    }

    private static async Task<ProjectLifecycleHistoryRecord> ControlledClosure(RoadGuardSystem.Repositories.RoadGuardDbContext db, (Guid Project, Guid Actor) source)
    {
        var record = ProjectLifecycleHistoryRecord.RecordCandidate(Guid.NewGuid(), source.Project, ProjectLifecycleFactKind.OperationalClosure,
            source.Actor, DateTimeOffset.UtcNow, "controlled prior closure fixture", "TEST_ONLY: no actor/procedure adoption", "{}");
        db.Add(record);
        // Controlled retained source for the independently confirmed renewed command.
        // This test does not implement, expose or approve the pending generic close command.
        db.Entry(record).Property(row => row.SourceDisposition).CurrentValue = "TARGET_CONFIRMED";
        db.Entry(record).Property(row => row.AuthoritySourceReference).CurrentValue = "TEST_ONLY_CONTROLLED_RETAINED_CLOSURE";
        await db.SaveChangesAsync(); return record;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RenewedHandlingUsesConfirmedRoleButCannotInventAnOperationalClosure(bool wrongRole)
    {
        var source = await Seed(); await using var db = sql.CreateDbContext();
        var repository = new ProjectLifecycleRepository(db, TimeProvider.System);
        var view = await repository.ReadAsync(source.Actor, source.Project, default); Assert.NotNull(view);
        var result = await repository.RenewAsync(new(source.Actor, wrongRole ? UserRoleCode.ProjectManager : UserRoleCode.Supervisor,
            source.Project, new ProjectRenewedHandlingInput(Guid.NewGuid(), "renewed decision", "exact handling scope", "actual basis"),
            Guid.NewGuid().ToString(), view.Version), default);
        Assert.Equal(wrongRole ? 403 : 409, result.Status);
        Assert.Equal(wrongRole ? "access_forbidden" : "operational_closure_source_unavailable", result.Code);
        Assert.Empty(await db.Set<ProjectLifecycleHistoryRecord>().Where(row => row.ProjectId == source.Project).ToArrayAsync());
    }
    [Fact]
    public async Task LegacyClosedProjectAndEmptyInventoryDoNotManufactureVerifiedLifecycleFacts()
    {
        var source = await Seed(); await using var db = sql.CreateDbContext();
        // Controlled historical status, not a newly authorized close command.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Projects SET Status=3 WHERE Id={source.Project}");
        var view = await new ProjectLifecycleRepository(db, TimeProvider.System).ReadAsync(source.Actor, source.Project, default);
        Assert.NotNull(view); Assert.Equal("UNKNOWN", view.ConstructionCompletion);
        Assert.Equal("UNKNOWN", view.OperationalClosure); Assert.Equal("UNKNOWN", view.ObligationInventory);
        Assert.Equal("UNKNOWN", view.OperationalClosureEligibility); Assert.True(view.AcceptsNewReports);
        Assert.False(view.WarrantyRecorded); Assert.Empty(view.History);
        Assert.Contains("LIFECYCLE_AUTHORITY_SOURCE_NOT_VERIFIED", view.MissingReasons);
        Assert.Contains("OBLIGATION_INVENTORY_NOT_VERIFIED", view.MissingReasons);
    }

    [Fact]
    public async Task CurrentReadAuthorityIsRecheckedAfterMembershipRevocation()
    {
        var source = await Seed(); await using var db = sql.CreateDbContext();
        var repository = new ProjectLifecycleRepository(db, TimeProvider.System);
        Assert.NotNull(await repository.ReadAsync(source.Actor, source.Project, default));
        await db.ProjectMembers.Where(row => row.UserId == source.Actor && row.ProjectId == source.Project)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => repository.ReadAsync(source.Actor, source.Project, default));
    }

    private async Task<(Guid Project, Guid Actor)> Seed()
    {
        await using var db = sql.CreateDbContext(); var now = DateTimeOffset.UtcNow;
        if (!await db.Roles.AnyAsync(row => row.Code == UserRoleCode.Supervisor)) db.Add(new ApplicationRole(UserRoleCode.Supervisor, "Supervisor"));
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), PasswordHash = "fixture", DisplayName = "lifecycle source actor", RoleCode = UserRoleCode.Supervisor, CreatedAt = now };
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Lifecycle source fixture", null, null, null, null, now);
        db.AddRange(actor, project); await db.SaveChangesAsync();
        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = actor.Id,
            RoleCode = UserRoleCode.Supervisor,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime),
            Status = ProjectMemberStatus.Active
        });
        await db.SaveChangesAsync(); return (project.Id, actor.Id);
    }
}
