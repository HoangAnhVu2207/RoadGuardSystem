using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class WebSessionAtomicityAcceptanceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CommitFailureRollsBackTouchOrRetryCommitsOnlyExistingSession(bool renew, bool retry)
    {
        await using var owned = new OwnedDatabase();
        await owned.Sql.InitializeAsync();
        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"atomic-{Guid.NewGuid():N}",
            DisplayName = "Atomic fixture",
            PasswordHash = "fixture",
            RoleCode = UserRoleCode.Reporter,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            LastActivityAt = now,
            Lifecycle = SessionLifecycle.PersistentRenewable,
            IssuedRole = user.RoleCode,
            Transport = SessionTransport.Web
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = null
        };
        await using (var seed = owned.Sql.CreateDbContext())
        {
            await owned.Sql.SeedRolesAsync(seed);
            seed.AddRange(user, session, token); await seed.SaveChangesAsync();
        }
        await using var before = owned.Sql.CreateDbContext();
        var original = await before.Sessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
        var userVersion = (await before.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).RowVersion;
        var tokenVersion = (await before.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id)).RowVersion;
        var auditCount = await before.AuditLogs.CountAsync();
        var receiptCount = await before.IdempotencyRecords.CountAsync();
        var failure = new FailFirstCommitInterceptor();
        await using (var execution = retry ? owned.Sql.CreateRetryingDbContext(failure) : owned.Sql.CreateDbContext(failure))
        {
            var repo = new IdentityRepository(execution, new FixedClock(now.AddMinutes(2)));
            async Task<WebSessionState?> Invoke() => renew
                ? await repo.RenewWebSessionAsync(token.TokenHash, now)
                : await repo.TouchWebSessionAsync(user.Id, session.Id, user.RoleCode, now);
            if (retry) Assert.NotNull(await Invoke());
            else await Assert.ThrowsAsync<TestTransientException>(Invoke);
        }
        Assert.Equal(1, failure.FailureCount);
        await using var verify = owned.Sql.CreateDbContext();
        var stored = await verify.Sessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
        if (retry)
        {
            Assert.Equal(now.AddMinutes(2), stored.LastActivityAt);
            Assert.NotEqual(original.RowVersion, stored.RowVersion);
        }
        else
        {
            Assert.Equal(original.LastActivityAt, stored.LastActivityAt);
            Assert.Equal(original.RowVersion, stored.RowVersion);
        }
        Assert.Equal(original.IssuedAt, stored.IssuedAt); Assert.Null(stored.RevokedAt);
        Assert.Equal(1, await verify.Sessions.CountAsync(x => x.UserId == user.Id));
        var credential = await verify.RefreshTokens.AsNoTracking().SingleAsync(x => x.SessionId == session.Id);
        Assert.Equal(token.Id, credential.Id); Assert.Equal(token.TokenHash, credential.TokenHash);
        Assert.Equal(tokenVersion, credential.RowVersion); Assert.Null(credential.RevokedAt);
        Assert.Equal(userVersion, (await verify.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).RowVersion);
        Assert.Equal(auditCount, await verify.AuditLogs.CountAsync());
        Assert.Equal(receiptCount, await verify.IdempotencyRecords.CountAsync());
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }

    private sealed class OwnedDatabase : IAsyncDisposable
    {
        public IdentitySqlServerFixture Sql { get; } = new();
        public ValueTask DisposeAsync() => new(Sql.DisposeAsync());
    }
}
