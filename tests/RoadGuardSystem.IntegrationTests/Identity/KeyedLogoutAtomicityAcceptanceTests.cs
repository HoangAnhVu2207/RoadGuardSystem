using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class KeyedLogoutAtomicityAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyedLogoutPrecommitFailureRollsBackOrRetriesOneReceiptAndAudit(bool retry)
    {
        var owned = new IdentitySqlServerFixture();
        await owned.InitializeAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"logout-{Guid.NewGuid():N}",
                DisplayName = "Logout fixture",
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
                Lifecycle = SessionLifecycle.PersistentRenewable,
                IssuedRole = user.RoleCode
            };
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = null
            };
            await using (var seed = owned.CreateDbContext())
            {
                await owned.SeedRolesAsync(seed); seed.AddRange(user, session, token); await seed.SaveChangesAsync();
            }
            var sessionVersion = session.RowVersion; var tokenVersion = token.RowVersion;
            var key = Guid.NewGuid().ToString("N"); var failure = new FailFirstCommitInterceptor();
            var fingerprint = new string('a', 64);
            await using (var execution = retry ? owned.CreateRetryingDbContext(failure) : owned.CreateDbContext(failure))
            {
                var repo = new IdentityRepository(execution);
                if (retry)
                {
                    var result = await repo.RevokeSessionAndFamilyAtomicAsync(user.Id, session.Id, key, fingerprint);
                    Assert.False(result.IdempotentConflict);
                }
                else await Assert.ThrowsAsync<TestTransientException>(() => repo.RevokeSessionAndFamilyAtomicAsync(user.Id, session.Id, key, fingerprint));
            }
            Assert.Equal(1, failure.FailureCount);
            await using var verify = owned.CreateDbContext();
            var stored = await verify.Sessions.AsNoTracking().SingleAsync(x => x.Id == session.Id);
            var credential = await verify.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id);
            Assert.Equal(retry, stored.RevokedAt.HasValue); Assert.Equal(stored.RevokedAt, credential.RevokedAt);
            Assert.Equal(retry ? 1 : 0, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == user.Id && x.Operation == "Logout"));
            Assert.Equal(retry ? 1 : 0, await verify.AuditLogs.CountAsync(x => x.ActorUserId == user.Id && x.EventType == "auth_logout"));
            Assert.Equal(user.RowVersion, (await verify.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).RowVersion);
            Assert.Equal(token.TokenHash, credential.TokenHash);
            if (!retry)
            {
                Assert.Equal(sessionVersion, stored.RowVersion); Assert.Equal(tokenVersion, credential.RowVersion);
            }
            else
            {
                var repo = new IdentityRepository(verify);
                Assert.True((await repo.RevokeSessionAndFamilyAtomicAsync(user.Id, session.Id, key, fingerprint)).IdempotentReplay);
                Assert.True((await repo.RevokeSessionAndFamilyAtomicAsync(user.Id, session.Id, key, new string('b', 64))).IdempotentConflict);
                Assert.Equal(1, await verify.IdempotencyRecords.CountAsync(x => x.ActorUserId == user.Id && x.Operation == "Logout"));
                Assert.Equal(1, await verify.AuditLogs.CountAsync(x => x.ActorUserId == user.Id && x.EventType == "auth_logout"));
                Assert.Equal(stored.RowVersion, (await verify.Sessions.AsNoTracking().SingleAsync(x => x.Id == session.Id)).RowVersion);
                Assert.Equal(credential.RowVersion, (await verify.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id)).RowVersion);
            }
        }
        finally { await owned.DisposeAsync(); }
    }
}
