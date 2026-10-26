using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class AdminResetAtomicityAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitFaultRollsBackOrRetriesOneCredentialChangeAndOneSanitizedHistory(bool retry)
    {
        var owned = new IdentitySqlServerFixture(); await owned.InitializeAsync();
        try
        {
            var now = DateTimeOffset.UtcNow; var actor = User(now, UserRoleCode.Supervisor);
            var target = User(now, UserRoleCode.Reporter); var other = User(now, UserRoleCode.DroneOperator);
            UserSession Session(ApplicationUser user) => new() { Id = Guid.NewGuid(), UserId = user.Id, IssuedAt = now, Lifecycle = SessionLifecycle.PersistentRenewable, Transport = SessionTransport.Android, IssuedRole = user.RoleCode };
            var active = Session(target); var prior = Session(target); prior.IssuedAt = now.AddMinutes(-1); prior.RevokedAt = now.AddSeconds(-1); var unrelated = Session(other);
            RefreshToken Token(UserSession s) => new() { Id = Guid.NewGuid(), SessionId = s.Id, TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") };
            var activeToken = Token(active); var priorToken = Token(prior); priorToken.RevokedAt = prior.RevokedAt; var otherToken = Token(unrelated);
            await using (var seed = owned.CreateDbContext())
            { await owned.SeedRolesAsync(seed); seed.AddRange(actor, target, other, active, prior, unrelated, activeToken, priorToken, otherToken); await seed.SaveChangesAsync(); }
            await using var verify = owned.CreateDbContext();
            var before = await Snapshot(verify, target.Id); var otherBefore = await Snapshot(verify, other.Id);
            var actorBefore = await Snapshot(verify, actor.Id); var operation = Guid.NewGuid(); var correlation = Guid.NewGuid();
            var failure = new FailFirstCommitInterceptor();
            await using (var execution = retry ? owned.CreateRetryingDbContext(failure) : owned.CreateDbContext(failure))
            {
                Task<AdminPasswordResetResult> Invoke() => new IdentityRepository(execution, new FixedClock(now)).ResetUserPasswordAtomicAsync(
                    actor.Id, target.Id, "test-only-replacement-hash", "test-only-replacement-stamp", target.RowVersion,
                    new string('a', 64), operation, correlation);
                if (retry) Assert.Equal(AdminPasswordResetStatus.Success, (await Invoke()).Status);
                else await Assert.ThrowsAsync<TestTransientException>(Invoke);
            }
            Assert.Equal(1, failure.FailureCount); Assert.Equal(otherBefore, await Snapshot(verify, other.Id)); Assert.Equal(actorBefore, await Snapshot(verify, actor.Id));
            if (!retry)
            {
                Assert.Equal(before, await Snapshot(verify, target.Id));
                Assert.Empty(await verify.PasswordResetLogs.ToArrayAsync()); Assert.Empty(await verify.AuditLogs.ToArrayAsync()); Assert.Empty(await verify.IdempotencyRecords.ToArrayAsync());
                return;
            }
            var changed = await verify.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id);
            Assert.Equal("test-only-replacement-hash", changed.PasswordHash); Assert.Equal("test-only-replacement-stamp", changed.SecurityStamp); Assert.True(changed.MustChangePassword);
            var sessions = await verify.Sessions.AsNoTracking().Where(s => s.UserId == target.Id).ToListAsync();
            Assert.Equal(now, sessions.Single(s => s.Id == active.Id).RevokedAt); Assert.Equal(prior.RevokedAt, sessions.Single(s => s.Id == prior.Id).RevokedAt);
            var tokens = await verify.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == target.Id).ToListAsync();
            Assert.Equal(now, tokens.Single(t => t.Id == activeToken.Id).RevokedAt); Assert.Equal(priorToken.RevokedAt, tokens.Single(t => t.Id == priorToken.Id).RevokedAt);
            var reset = Assert.Single(await verify.PasswordResetLogs.AsNoTracking().ToListAsync());
            Assert.Equal(actor.Id, reset.PerformedByUserId); Assert.Equal(target.Id, reset.TargetUserId); Assert.Equal(now, reset.OccurredAt); Assert.Equal(correlation, reset.CorrelationId);
            var audit = Assert.Single(await verify.AuditLogs.AsNoTracking().ToListAsync());
            Assert.Equal(actor.Id, audit.ActorUserId); Assert.Equal(target.Id, audit.EntityId); Assert.Equal("User", audit.EntityType);
            Assert.Equal("user_password_reset", audit.EventType); Assert.Equal("IdentityRepository", audit.Source); Assert.Equal(correlation, audit.CorrelationId); Assert.Equal(now, audit.OccurredAtUtc);
            Assert.Equal("{\"must_change_password\":false}", audit.BeforeSnapshot); Assert.Equal("{\"must_change_password\":true}", audit.AfterSnapshot); Assert.Null(audit.Reason);
            var receipt = Assert.Single(await verify.IdempotencyRecords.AsNoTracking().ToListAsync());
            Assert.Equal(actor.Id, receipt.ActorUserId); Assert.Null(receipt.ProjectId); Assert.Equal("AdminPasswordReset", receipt.Operation);
            Assert.Equal(operation.ToString("N"), receipt.IdempotencyKey); Assert.Equal("{\"status\":\"success\"}", receipt.OutcomeJson);
            var committed = await Snapshot(verify, target.Id);
            await using var replay = owned.CreateDbContext();
            var result = await new IdentityRepository(replay).ResetUserPasswordAtomicAsync(actor.Id, target.Id,
                "different-test-hash", "different-test-stamp", target.RowVersion, new string('a', 64), operation, Guid.NewGuid());
            Assert.Equal(AdminPasswordResetStatus.IdempotentReplay, result.Status); Assert.Equal(committed, await Snapshot(verify, target.Id));
            Assert.Equal(audit.Id, Assert.Single(await verify.AuditLogs.AsNoTracking().ToListAsync()).Id);
            Assert.Equal(reset.Id, Assert.Single(await verify.PasswordResetLogs.AsNoTracking().ToListAsync()).Id);
            Assert.Equal(receipt.Id, Assert.Single(await verify.IdempotencyRecords.AsNoTracking().ToListAsync()).Id);
        }
        finally { await owned.DisposeAsync(); }
    }
    private static ApplicationUser User(DateTimeOffset now, UserRoleCode role) => new() { Id = Guid.NewGuid(), UserName = "reset-sql-" + Guid.NewGuid().ToString("N"), DisplayName = "Reset SQL fixture", PasswordHash = "initial-test-hash", SecurityStamp = "initial-test-stamp", RoleCode = role, Status = UserStatus.Active, CreatedAt = now };
    private static async Task<string> Snapshot(RoadGuardDbContext db, Guid id) => JsonSerializer.Serialize(new
    {
        user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == id),
        sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == id).OrderBy(s => s.Id).ToListAsync(),
        tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == id).OrderBy(t => t.Id).ToListAsync()
    });
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
