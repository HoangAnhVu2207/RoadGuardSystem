using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.TestFixtures;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class NotificationSupervisorDeleteSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteRaceHasOneDurableRevocationAndScopedReceipt(bool sameKey)
    {
        await using var seed = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(seed, sql);
        var state = await ND01OriginalWindowFixture.Prepare(seed, new(source.Pm, source.Crew, source.Supervisor, source.Project, source.Road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        var create = new NotificationSupervisorRequest(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item, "PUT", "delete-create", IfNoneMatch: "*", Body: JsonSerializer.Serialize(new { supervisorId = source.Supervisor }));
        var created = await new NotificationSupervisorRepository(seed, new IdempotencyOperationService(seed), TimeProvider.System).ExecuteAsync(create, default);
        Assert.Equal(201, created.Status);
        var request = create with { Method = "DELETE", Key = "delete-race-A", IfNoneMatch = null, IfMatch = '"' + created.Version + '"', Body = "" };
        async Task<NotificationSupervisorResult> Execute(NotificationSupervisorRequest input)
        {
            await using var db = sql.CreateRetryingDbContext();
            return await new NotificationSupervisorRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ExecuteAsync(input, default);
        }
        var results = await Task.WhenAll(Execute(request), Execute(request with { Key = sameKey ? request.Key : "delete-race-B" }));
        Assert.Equal(sameKey ? new[] { 204, 204 } : new[] { 204, 412 }, results.Select(x => x.Status).Order());
        if (sameKey) Assert.Equal(results[0].Version, results[1].Version);
        await using var inspect = sql.CreateDbContext();
        var head = await inspect.Set<NotificationSupervisorHead>().AsNoTracking().SingleAsync(x => x.ItemId == state.Item);
        Assert.NotNull(head.RevokedAt);
        Assert.Equal(created.Value!.AssignmentId, head.AssignmentId);
        Assert.Equal(1, await inspect.Set<NotificationSupervisorGeneration>().CountAsync(x => x.ItemId == state.Item));
        Assert.Equal(2, await inspect.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == state.Item));
        var audit = await inspect.Set<NotificationSupervisorAudit>().SingleAsync(x => x.ItemId == state.Item && x.Action == "REVOKED");
        Assert.Equal(source.Pm, audit.ActorId);
        Assert.Equal(head.AssignmentId, audit.AssignmentId);
        Assert.Equal(head.RevokedAt, audit.At);
        var operation = "b2.assignment." + state.Item.ToString("N");
        Assert.Equal(2, await inspect.IdempotencyRecords.CountAsync(x => x.ActorUserId == source.Pm && x.ProjectId == source.Project && x.Operation == operation));
    }

    [Fact]
    public async Task DeleteAuditFailureRollsBackHeadVersionReceiptAndRetryCommitsOnce()
    {
        await using var seed = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(seed, sql);
        var state = await ND01OriginalWindowFixture.Prepare(seed, new(source.Pm, source.Crew, source.Supervisor, source.Project, source.Road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        var create = new NotificationSupervisorRequest(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item, "PUT", "delete-rollback-create", IfNoneMatch: "*", Body: JsonSerializer.Serialize(new { supervisorId = source.Supervisor }));
        var created = await new NotificationSupervisorRepository(seed, new IdempotencyOperationService(seed), TimeProvider.System).ExecuteAsync(create, default);
        Assert.Equal(201, created.Status);
        var request = create with { Method = "DELETE", Key = "delete-rollback", IfNoneMatch = null, IfMatch = '"' + created.Version + '"', Body = "" };
        await using (var failing = sql.CreateDbContext(new FailRevocationAudit()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => new NotificationSupervisorRepository(failing, new IdempotencyOperationService(failing), TimeProvider.System).ExecuteAsync(request, default));
        await using (var inspect = sql.CreateDbContext())
        {
            var head = await inspect.Set<NotificationSupervisorHead>().AsNoTracking().SingleAsync(x => x.ItemId == state.Item);
            Assert.Null(head.RevokedAt);
            Assert.Equal(created.Version, Convert.ToBase64String(head.RowVersion));
            Assert.Equal(1, await inspect.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == state.Item));
            Assert.False(await inspect.IdempotencyRecords.AnyAsync(x => x.IdempotencyKey == request.Key));
        }
        await using var recovery = sql.CreateDbContext();
        var repo = new NotificationSupervisorRepository(recovery, new IdempotencyOperationService(recovery), TimeProvider.System);
        var committed = await repo.ExecuteAsync(request, default);
        var replay = await repo.ExecuteAsync(request, default);
        Assert.Equal(204, committed.Status); Assert.Equal(204, replay.Status);
        Assert.Equal(committed.Version, replay.Version);
        await using var final = sql.CreateDbContext();
        Assert.NotNull((await final.Set<NotificationSupervisorHead>().SingleAsync(x => x.ItemId == state.Item)).RevokedAt);
        Assert.Equal(1, await final.Set<NotificationSupervisorGeneration>().CountAsync(x => x.ItemId == state.Item));
        Assert.Equal(1, await final.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == state.Item && x.Action == "REVOKED"));
        Assert.Equal(1, await final.IdempotencyRecords.CountAsync(x => x.IdempotencyKey == request.Key));
    }

    private sealed class FailRevocationAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<NotificationSupervisorAudit>().Any(x => x.State == EntityState.Added && x.Entity.Action == "REVOKED"))
                throw new InvalidOperationException("Controlled revocation audit save failure");
            return ValueTask.FromResult(result);
        }
    }
}
