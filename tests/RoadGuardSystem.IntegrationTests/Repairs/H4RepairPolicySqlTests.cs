using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4RepairPolicySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task CurrentPmPublishesExactVersionedDraftAndRevokesWithoutRewritingHistory()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db);
        var now = DateTimeOffset.UtcNow;
        var pm = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), PasswordHash = "fixture",
            DisplayName = "policy fixture", RoleCode = UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = now };
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Policy SQL fixture", null, null, null, null, now);
        var type = DefectType.Create("P" + Guid.NewGuid().ToString("N"), "policy defect");
        var member = ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1));
        db.AddRange(pm, project, type, member); await db.SaveChangesAsync();
        var repo = new RepairPolicyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var definition = new RepairPolicyDefinition(type.Code, "v1", [new("width", "mm", 0, 2)], ["unstable"], "explicit configured source");
        var create = new RepairPolicyCommand(pm.Id, UserRoleCode.ProjectManager, project.Id, "create", null,
            definition, null, Guid.NewGuid().ToString(), null);
        Assert.Equal(403, (await repo.ExecuteAsync(create with { Role = UserRoleCode.Supervisor }, default)).Status);
        var created = await repo.ExecuteAsync(create, default); Assert.Equal(201, created.Status);
        var draftId = created.Value!.Id; var original = await db.Set<RepairPolicyDraftChange>().AsNoTracking().SingleAsync();
        db.ChangeTracker.Clear();
        Assert.Equal(200, (await repo.ExecuteAsync(create, default)).Status);
        Assert.Equal(409, (await repo.ExecuteAsync(create with { Definition = definition with { ChecklistVersion = "changed" } }, default)).Status);
        var update = create with { Action = "update", ResourceId = draftId, Key = Guid.NewGuid().ToString(),
            ExpectedVersion = created.Value.Version, Definition = definition with { ChecklistVersion = "v2", Measurements = [new("width", "mm", 0, 3)] } };
        await using (var failing = sql.CreateDbContext(new FailPolicyReceipt()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => new RepairPolicyRepository(failing,
                new IdempotencyOperationService(failing), TimeProvider.System).ExecuteAsync(update, default));
        Assert.Equal(1, await db.Set<RepairPolicyDraftChange>().CountAsync(row => EF.Property<Guid?>(row, "DraftId") == draftId));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ProjectId == project.Id && row.IdempotencyKey == update.Key));
        var updated = await repo.ExecuteAsync(update, default); Assert.Equal(201, updated.Status); db.ChangeTracker.Clear();
        Assert.Equal(2, await db.Set<RepairPolicyDraftChange>().CountAsync(row => EF.Property<Guid?>(row, "DraftId") == draftId));
        Assert.Equal("v1", (await db.Set<RepairPolicyDraftChange>().AsNoTracking().SingleAsync(row => row.Id == original.Id)).ChecklistVersion);
        var publish = update with { Action = "publish", Definition = null, Reason = "publish configured version",
            Key = Guid.NewGuid().ToString(), ExpectedVersion = updated.Value!.Version };
        await using var rival = sql.CreateDbContext();
        var race = await Task.WhenAll(repo.ExecuteAsync(publish, default), new RepairPolicyRepository(rival,
            new IdempotencyOperationService(rival), TimeProvider.System).ExecuteAsync(publish, default));
        Assert.Equal(new[] { 200, 201 }, race.Select(row => row.Status).Order().ToArray());
        var published = race.Single(row => row.Status == 201); db.ChangeTracker.Clear();
        Assert.Equal("PUBLISHED", published.Value!.State); Assert.Equal("v2", published.Value.ChecklistVersion);
        Assert.Equal(3m, Assert.Single(published.Value.Measurements).Maximum);
        Assert.Equal(200, (await repo.ExecuteAsync(publish, default)).Status);
        var freshDraft = await repo.ExecuteAsync(publish with { Action = "draft-get", Key = null }, default);
        Assert.Equal(409, (await repo.ExecuteAsync(update with { Key = Guid.NewGuid().ToString(), ExpectedVersion = freshDraft.Value!.Version }, default)).Status);
        var revoke = publish with { Action = "revoke", ResourceId = published.Value.Id, Key = Guid.NewGuid().ToString(),
            ExpectedVersion = published.Value.Version, Reason = "unsafe policy withdrawn" };
        var revoked = await repo.ExecuteAsync(revoke, default); Assert.Equal(201, revoked.Status); db.ChangeTracker.Clear();
        Assert.Equal("REVOKED", revoked.Value!.State); Assert.Equal(200, (await repo.ExecuteAsync(revoke, default)).Status);
        Assert.Equal(1, await db.Set<RepairPolicyRevision>().CountAsync(row => row.Id == published.Value.Id));
        Assert.Equal(2, await db.Set<RepairPolicyDraftChange>().CountAsync(row => EF.Property<Guid?>(row, "DraftId") == draftId));
        var current = await db.ProjectMembers.SingleAsync(row => row.Id == member.Id); current.Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await repo.ExecuteAsync(revoke, default)).Status);
        Assert.Equal(403, (await repo.ExecuteAsync(publish, default)).Status);
    }
    private sealed class FailPolicyReceipt : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (data.Context!.ChangeTracker.Entries<IdempotencyRecord>().Any(row => row.State == EntityState.Added &&
                row.Entity.Operation == "h4.repair.policy.update.v1")) throw new InvalidOperationException("Controlled policy receipt failure.");
            return ValueTask.FromResult(result);
        }
    }
}
