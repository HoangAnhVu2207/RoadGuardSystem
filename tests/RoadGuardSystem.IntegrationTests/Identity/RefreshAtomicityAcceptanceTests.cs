using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class RefreshAtomicityAcceptanceTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task RotationCommitFaultRollsBackOrRetriesExactlyOneSuccessor(bool keyed, bool retry, bool bounded)
    {
        var owned = new IdentitySqlServerFixture();
        await owned.InitializeAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var (actor, session, predecessor) = NewFamily(now, bounded);
            var (other, otherSession, otherToken) = NewFamily(now, false);
            await using (var seed = owned.CreateDbContext())
            {
                await owned.SeedRolesAsync(seed);
                seed.AddRange(actor, session, predecessor, other, otherSession, otherToken);
                await seed.SaveChangesAsync();
            }
            await using var verify = owned.CreateDbContext();
            var unrelated = await Snapshot(verify, other.Id);
            var original = await Snapshot(verify, actor.Id);
            var replacement = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TokenHash = RandomHash(),
                ExpiresAt = now.AddDays(30)
            };
            var key = Guid.NewGuid().ToString("N");
            const string protectedValue = "test-only-protected-credential";
            var failure = new FailFirstCommitInterceptor();
            await using (var execution = retry ? owned.CreateRetryingDbContext(failure) : owned.CreateDbContext(failure))
            {
                var repo = new IdentityRepository(execution, new FixedClock(now));
                Task<RotateRefreshTokenResult> Invoke() => keyed
                    ? repo.RotateRefreshTokenWithReceiptAsync(predecessor.Id, predecessor.RowVersion,
                        replacement, key, predecessor.TokenHash, protectedValue)
                    : repo.RotateRefreshTokenAsync(predecessor.Id, predecessor.RowVersion, replacement);
                if (retry) Assert.Equal(RotateRefreshTokenStatus.Success, (await Invoke()).Status);
                else await Assert.ThrowsAsync<TestTransientException>(Invoke);
            }
            Assert.Equal(1, failure.FailureCount);
            Assert.Equal(unrelated, await Snapshot(verify, other.Id));
            Assert.Empty(await verify.AuditLogs.ToArrayAsync());
            if (!retry)
            {
                Assert.Equal(original, await Snapshot(verify, actor.Id));
                Assert.Empty(await verify.IdempotencyRecords.ToArrayAsync());
                return;
            }
            var tokens = await verify.RefreshTokens.AsNoTracking().Where(t => t.SessionId == session.Id).ToArrayAsync();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(now, Assert.Single(tokens, t => t.Id == predecessor.Id).RevokedAt);
            var successor = Assert.Single(tokens, t => t.Id == replacement.Id);
            Assert.Equal(replacement.TokenHash, successor.TokenHash); Assert.Null(successor.RevokedAt);
            Assert.Equal(bounded ? session.ExpiresAt : null, successor.ExpiresAt);
            var storedSession = await verify.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
            Assert.Equal(session.RowVersion, storedSession.RowVersion); Assert.Null(storedSession.RevokedAt);
            Assert.Equal(actor.RowVersion, (await verify.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
            var receipts = await verify.IdempotencyRecords.AsNoTracking().ToArrayAsync();
            Assert.Equal(keyed ? 1 : 0, receipts.Length);
            if (keyed)
            {
                var receipt = Assert.Single(receipts);
                Assert.Equal(actor.Id, receipt.ActorUserId); Assert.Null(receipt.ProjectId);
                Assert.Equal("RefreshRotation", receipt.Operation); Assert.Equal($"{predecessor.Id:N}:{key}", receipt.IdempotencyKey);
                Assert.Equal(replacement.Id, receipt.OperationId); Assert.Equal(now, receipt.CreatedAtUtc);
                var outcome = JsonSerializer.Deserialize<JsonElement>(receipt.OutcomeJson);
                Assert.Equal(replacement.Id, outcome.GetProperty("SuccessorId").GetGuid());
                Assert.Equal(protectedValue, outcome.GetProperty("ProtectedCredential").GetString());
                Assert.Equal(now.AddMinutes(2), outcome.GetProperty("ReplayUntil").GetDateTimeOffset());
                var snapshot = await Snapshot(verify, actor.Id);
                await using var replay = owned.CreateDbContext();
                var result = await new IdentityRepository(replay, new FixedClock(now)).RotateRefreshTokenWithReceiptAsync(
                    predecessor.Id, predecessor.RowVersion, new RefreshToken { Id = Guid.NewGuid(), TokenHash = RandomHash() },
                    key, predecessor.TokenHash, "different-test-protected-value");
                Assert.Equal(RotateRefreshTokenStatus.Success, result.Status);
                Assert.Equal(replacement.Id, result.NewToken!.Id); Assert.Equal(protectedValue, result.ProtectedCredential);
                Assert.Equal(snapshot, await Snapshot(verify, actor.Id));
                Assert.Single(await verify.IdempotencyRecords.ToArrayAsync()); Assert.Empty(await verify.AuditLogs.ToArrayAsync());
            }
        }
        finally { await owned.DisposeAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplayRevocationCommitFaultPreservesFamilyOrCommitsOneAuditAndReceipt(bool retry)
    {
        var owned = new IdentitySqlServerFixture();
        await owned.InitializeAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var (actor, session, predecessor) = NewFamily(now, false);
            predecessor.RevokedAt = now.AddSeconds(-5);
            var active = new RefreshToken { Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = RandomHash() };
            var (other, otherSession, otherToken) = NewFamily(now, false);
            await using (var seed = owned.CreateDbContext())
            {
                await owned.SeedRolesAsync(seed);
                seed.AddRange(actor, session, predecessor, active, other, otherSession, otherToken);
                await seed.SaveChangesAsync();
            }
            await using var verify = owned.CreateDbContext();
            var original = await Snapshot(verify, actor.Id); var unrelated = await Snapshot(verify, other.Id);
            var failure = new FailFirstCommitInterceptor(); var correlation = Guid.NewGuid();
            await using (var execution = retry ? owned.CreateRetryingDbContext(failure) : owned.CreateDbContext(failure))
            {
                var repo = new IdentityRepository(execution, new FixedClock(now));
                Task<ReplayRevocationResult> Invoke() => repo.RevokeRefreshTokenFamilyForReplayAsync(predecessor.Id, predecessor.RowVersion, correlation);
                if (retry) Assert.Equal(ReplayRevocationStatus.Success, (await Invoke()).Status);
                else await Assert.ThrowsAsync<TestTransientException>(Invoke);
            }
            Assert.Equal(1, failure.FailureCount); Assert.Equal(unrelated, await Snapshot(verify, other.Id));
            if (!retry)
            {
                Assert.Equal(original, await Snapshot(verify, actor.Id));
                Assert.Empty(await verify.AuditLogs.ToArrayAsync()); Assert.Empty(await verify.IdempotencyRecords.ToArrayAsync());
                return;
            }
            Assert.Equal(now, (await verify.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id)).RevokedAt);
            var tokens = await verify.RefreshTokens.AsNoTracking().Where(t => t.SessionId == session.Id).ToArrayAsync();
            Assert.Equal(2, tokens.Length);
            Assert.Equal(predecessor.RevokedAt, Assert.Single(tokens, t => t.Id == predecessor.Id).RevokedAt);
            Assert.Equal(now, Assert.Single(tokens, t => t.Id == active.Id).RevokedAt);
            var audit = Assert.Single(await verify.AuditLogs.AsNoTracking().ToArrayAsync());
            Assert.Null(audit.ActorUserId); Assert.Equal("auth_token_replay_detected", audit.EventType);
            Assert.Equal("Session", audit.EntityType); Assert.Equal(session.Id, audit.EntityId);
            Assert.Equal(now, audit.OccurredAtUtc); Assert.Equal(correlation, audit.CorrelationId);
            Assert.Equal("IdentityRepository", audit.Source);
            Assert.Null(audit.BeforeSnapshot); Assert.Null(audit.AfterSnapshot); Assert.Null(audit.Reason);
            var receipt = Assert.Single(await verify.IdempotencyRecords.AsNoTracking().ToArrayAsync());
            Assert.Null(receipt.ActorUserId); Assert.Null(receipt.ProjectId);
            Assert.Equal("RefreshTokenReplay", receipt.Operation); Assert.Equal(predecessor.Id.ToString("N"), receipt.IdempotencyKey);
            var outcome = JsonSerializer.Deserialize<JsonElement>(receipt.OutcomeJson);
            Assert.Equal("family_revoked", outcome.GetProperty("status").GetString());
            Assert.Equal(session.Id.ToString("N"), outcome.GetProperty("session_id").GetString());
            var committed = await Snapshot(verify, actor.Id);
            await using var replay = owned.CreateDbContext();
            Assert.Equal(ReplayRevocationStatus.IdempotentReplay,
                (await new IdentityRepository(replay).RevokeRefreshTokenFamilyForReplayAsync(predecessor.Id, predecessor.RowVersion, Guid.NewGuid())).Status);
            Assert.Equal(committed, await Snapshot(verify, actor.Id));
            Assert.Equal(audit.Id, Assert.Single(await verify.AuditLogs.AsNoTracking().ToArrayAsync()).Id);
            Assert.Equal(receipt.Id, Assert.Single(await verify.IdempotencyRecords.AsNoTracking().ToArrayAsync()).Id);
        }
        finally { await owned.DisposeAsync(); }
    }

    private static (ApplicationUser, UserSession, RefreshToken) NewFamily(DateTimeOffset now, bool bounded)
    {
        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"refresh-{Guid.NewGuid():N}",
            DisplayName = "Refresh fixture",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Reporter,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = actor.Id,
            IssuedAt = now,
            Lifecycle = bounded ? SessionLifecycle.LegacyBounded : SessionLifecycle.PersistentRenewable,
            ExpiresAt = bounded ? now.AddDays(1) : null,
            IssuedRole = actor.RoleCode,
            Transport = SessionTransport.Android
        };
        var token = new RefreshToken { Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = RandomHash(), ExpiresAt = session.ExpiresAt };
        return (actor, session, token);
    }

    private static async Task<string> Snapshot(RoadGuardDbContext db, Guid userId) => JsonSerializer.Serialize(new
    {
        users = await db.Users.AsNoTracking().Where(u => u.Id == userId).ToArrayAsync(),
        sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == userId).OrderBy(s => s.Id).ToArrayAsync(),
        tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == userId).OrderBy(t => t.Id).ToArrayAsync()
    });

    private static string RandomHash() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
}
