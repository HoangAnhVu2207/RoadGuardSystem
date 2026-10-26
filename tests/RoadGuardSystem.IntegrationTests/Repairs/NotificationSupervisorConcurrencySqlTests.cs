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

public sealed class NotificationSupervisorConcurrencySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentCreatesSerializeAndRetainExactlyOneGeneration(bool sameKey)
    {
        await using var seed = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(seed, sql);
        Assert.False(seed.Database.HasPendingModelChanges());
        var state = await ND01OriginalWindowFixture.Prepare(seed, new(source.Pm, source.Crew, source.Supervisor, source.Project, source.Road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        var first = new NotificationSupervisorRequest(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
            "PUT", "race-A", IfNoneMatch: "*", Body: JsonSerializer.Serialize(new { supervisorId = source.Supervisor }));
        async Task<NotificationSupervisorResult> Execute(NotificationSupervisorRequest request)
        {
            await using var db = sql.CreateRetryingDbContext();
            return await new NotificationSupervisorRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ExecuteAsync(request, default);
        }
        var results = await Task.WhenAll(Execute(first), Execute(first with { Key = sameKey ? "race-A" : "race-B" }));
        Assert.Equal(sameKey ? new[] { 201, 201 } : new[] { 201, 412 }, results.Select(x => x.Status).Order());
        Assert.Equal(1, await seed.Set<NotificationSupervisorHead>().CountAsync(x => x.ItemId == state.Item));
        Assert.Equal(1, await seed.Set<NotificationSupervisorGeneration>().CountAsync(x => x.ItemId == state.Item));
        Assert.Equal(1, await seed.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == state.Item));
        if (sameKey) Assert.Equal(results[0].Version, results[1].Version);
    }

    [Fact]
    public async Task AuditFailureRollsBackHeadGenerationReceiptAndDatabaseEnforcesImmutableHistory()
    {
        await using var seed = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(seed, sql);
        var state = await ND01OriginalWindowFixture.Prepare(seed, new(source.Pm, source.Crew, source.Supervisor, source.Project, source.Road, source.Route, source.Set, source.Defect));
        using var keys = state.Keys;
        var request = new NotificationSupervisorRequest(source.Pm, UserRoleCode.ProjectManager, source.Project, state.Package, state.Item,
            "PUT", "rollback-A", IfNoneMatch: "*", Body: JsonSerializer.Serialize(new { supervisorId = source.Supervisor }));
        await using (var failing = sql.CreateDbContext(new FailAssignmentAudit()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => new NotificationSupervisorRepository(failing, new IdempotencyOperationService(failing), TimeProvider.System).ExecuteAsync(request, default));
        Assert.False(await seed.Set<NotificationSupervisorHead>().AnyAsync(x => x.ItemId == state.Item));
        Assert.False(await seed.Set<NotificationSupervisorGeneration>().AnyAsync(x => x.ItemId == state.Item));
        Assert.False(await seed.Set<NotificationSupervisorAudit>().AnyAsync(x => x.ItemId == state.Item));
        Assert.False(await seed.IdempotencyRecords.AnyAsync(x => x.IdempotencyKey == "rollback-A"));
        var repo = new NotificationSupervisorRepository(seed, new IdempotencyOperationService(seed), TimeProvider.System);
        var created = await repo.ExecuteAsync(request, default); Assert.Equal(201, created.Status);
        var generation = created.Value!.AssignmentId;
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => seed.Database.ExecuteSqlInterpolatedAsync($"UPDATE NotificationSupervisorGenerations SET ActorId={Guid.NewGuid()} WHERE Id={generation}"));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => seed.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM NotificationSupervisorAudits WHERE ItemId={state.Item}"));
        Assert.Equal(1, await seed.Set<NotificationSupervisorAudit>().CountAsync(x => x.ItemId == state.Item));
    }
    private sealed class FailAssignmentAudit : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<NotificationSupervisorAudit>().Any(x => x.State == EntityState.Added))
                throw new InvalidOperationException("Controlled assignment audit failure");
            return ValueTask.FromResult(result);
        }
    }
}
