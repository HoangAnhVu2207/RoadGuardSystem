using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Clocks;

public sealed class HuyFinalClockPersistenceTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture _fixture = new(createSpatialProbeSchema: false);
    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        await using var db = Db();
        await db.Database.MigrateAsync();
        await new IdentityRoleSeedStep().SeedAsync(db, default);
    }
    public Task DisposeAsync() => _fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(_fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);

    [Fact]
    public async Task SqlOriginsCompletionAndHistories_CannotBeRewrittenOrDeleted()
    {
        var (project, actor) = await SeedAsync();
        var now = DateTimeOffset.UtcNow;
        var clock = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.ProjectManagerReview,
            Guid.NewGuid(), Guid.NewGuid(), now.AddDays(-2));
        clock.Extend(Guid.NewGuid(), actor, now.AddDays(1), "Authorized review extension", now);
        await using var db = Db();
        db.Add(clock); await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var current = await db.Set<DeadlineClock>().Include(x => x.Extensions).Include(x => x.Breaches).SingleAsync(x => x.Id == clock.Id);
        Assert.Single(current.Extensions); Assert.Single(current.Breaches);
        Assert.Equal(clock.OriginalDueAt, current.Breaches.Single().DueAt);
        var originError = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DeadlineClocks SET OriginAt=DATEADD(hour,1,OriginAt) WHERE Id={clock.Id}"));
        Assert.Contains("immutable", originError.Message, StringComparison.OrdinalIgnoreCase);
        current.Complete(now.AddHours(1)); await db.SaveChangesAsync();
        var completionError = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DeadlineClocks SET CompletedAt=DATEADD(hour,1,CompletedAt) WHERE Id={clock.Id}"));
        Assert.Contains("immutable", completionError.Message, StringComparison.OrdinalIgnoreCase);
        var historyError = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DeadlineExtensions SET Reason='replacement' WHERE ClockId={clock.Id}"));
        Assert.Contains("immutable", historyError.Message, StringComparison.OrdinalIgnoreCase);
        var breachError = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DeadlineBreaches WHERE ClockId={clock.Id}"));
        Assert.Contains("immutable", breachError.Message, StringComparison.OrdinalIgnoreCase);
        var deleteError = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DeadlineClocks WHERE Id={clock.Id}"));
        Assert.Equal(547, deleteError.Number); // Referenced histories also independently prevent deletion.
        var bare = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.ProjectManagerReview,
            Guid.NewGuid(), Guid.NewGuid(), now);
        db.Add(bare); await db.SaveChangesAsync();
        var bareDeleteError = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM DeadlineClocks WHERE Id={bare.Id}"));
        Assert.Contains("immutable", bareDeleteError.Message, StringComparison.OrdinalIgnoreCase);
        var danger = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.DangerAcknowledgment,
            Guid.NewGuid(), Guid.NewGuid(), now);
        db.Add(danger); await db.SaveChangesAsync();
        var invalidAck = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DeadlineClocks SET AcknowledgedAt={now}, AcknowledgedByUserId={actor}, AcknowledgmentEventId={Guid.NewGuid()} WHERE Id={danger.Id}"));
        Assert.Equal(547, invalidAck.Number);
        Assert.Equal(clock.OriginAt, (await db.Set<DeadlineClock>().AsNoTracking().SingleAsync(x => x.Id == clock.Id)).OriginAt);
    }

    [Fact]
    public async Task StaleExtension_RollsBackItsHistoryAndScopeCannotDuplicateClock()
    {
        var (project, actor) = await SeedAsync(); var now = DateTimeOffset.UtcNow;
        var clock = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.CrewSupplement,
            Guid.NewGuid(), Guid.NewGuid(), now);
        await using (var seed = Db()) { seed.Add(clock); await seed.SaveChangesAsync(); }
        await using var first = Db(); await using var second = Db();
        var a = await first.Set<DeadlineClock>().Include(x => x.Extensions).Include(x => x.Breaches).SingleAsync(x => x.Id == clock.Id);
        var b = await second.Set<DeadlineClock>().Include(x => x.Extensions).Include(x => x.Breaches).SingleAsync(x => x.Id == clock.Id);
        a.Extend(Guid.NewGuid(), actor, a.CurrentDueAt.AddHours(1), "Current extension", now.AddMinutes(1));
        b.Extend(Guid.NewGuid(), actor, b.CurrentDueAt.AddHours(2), "Stale extension", now.AddMinutes(2));
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var verify = Db();
        Assert.Single(await verify.Set<DeadlineExtension>().Where(x => x.ClockId == clock.Id).ToArrayAsync());
        Assert.Equal(a.CurrentDueAt, (await verify.Set<DeadlineClock>().AsNoTracking().SingleAsync(x => x.Id == clock.Id)).CurrentDueAt);
        verify.Add(DeadlineClock.Create(Guid.NewGuid(), project, clock.Kind, clock.TargetId, Guid.NewGuid(), now.AddHours(1)));
        await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync());
    }

    [Fact]
    public async Task CallerTransactionRollback_PreservesNoAdmittedClockOrHistory()
    {
        var (project, actor) = await SeedAsync(); var now = DateTimeOffset.UtcNow;
        var clock = DeadlineClock.Create(Guid.NewGuid(), project, DeadlineClockKind.FirstSafetyCheck,
            Guid.NewGuid(), Guid.NewGuid(), now.AddDays(-2));
        clock.Extend(Guid.NewGuid(), actor, now.AddDays(1), "Pending extension", now);
        await using (var db = Db())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            db.Add(clock); await db.SaveChangesAsync(); await tx.RollbackAsync();
        }
        await using var verify = Db();
        Assert.False(await verify.Set<DeadlineClock>().AnyAsync(x => x.Id == clock.Id));
        Assert.False(await verify.Set<DeadlineExtension>().AnyAsync(x => x.ClockId == clock.Id));
        Assert.False(await verify.Set<DeadlineBreach>().AnyAsync(x => x.ClockId == clock.Id));
    }

    private async Task<(Guid Project, Guid Actor)> SeedAsync()
    {
        var project = Guid.NewGuid(); var actor = Guid.NewGuid();
        await using var db = Db();
        db.Add(Project.Create(project, project.ToString(), "Isolated clock fixture", null, null, null, null, DateTimeOffset.UtcNow));
        db.Add(new ApplicationUser
        {
            Id = actor,
            UserName = actor.ToString(),
            NormalizedUserName = actor.ToString().ToUpperInvariant(),
            DisplayName = "Clock fixture",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            PasswordHash = "fixture-no-login"
        });
        await db.SaveChangesAsync(); return (project, actor);
    }
}
