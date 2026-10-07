using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

public sealed class H1PersistentIdentitySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotationCommitAckLoss_RecoveryChecksCurrentAuthority(bool revokeAfterCommit)
    {
        var now = DateTimeOffset.UtcNow; Guid userId, sessionId, oldId; var originalHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        await using (var db = sql.CreateDbContext())
        {
            await sql.SeedRolesAsync(db);
            var user = NewUser(now); db.Users.Add(user); await db.SaveChangesAsync(); userId = user.Id;
            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                IssuedAt = now,
                Lifecycle = SessionLifecycle.PersistentRenewable,
                IssuedRole = user.RoleCode,
                ExpiresAt = null,
                Transport = SessionTransport.Android
            }; sessionId = session.Id;
            var token = new RefreshToken { Id = Guid.NewGuid(), SessionId = session.Id, TokenHash = originalHash, ExpiresAt = null }; oldId = token.Id;
            db.AddRange(session, token); await db.SaveChangesAsync();
        }
        var interceptor = new CommitAckLoss(async () =>
        {
            if (revokeAfterCommit)
            {
                await using var other = sql.CreateDbContext();
                await other.Sessions.Where(s => s.Id == sessionId).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
            }
        });
        await using var context = sql.CreateRetryingDbContext(interceptor);
        var repository = new IdentityRepository(context); var old = await repository.FindRefreshTokenByHashAsync(originalHash);
        var result = await repository.RotateRefreshTokenWithReceiptAsync(oldId, old!.RowVersion,
            new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), ExpiresAt = null },
            Guid.NewGuid().ToString("N"), originalTokenHash: originalHash, protectedCredential: "protected-fixture");
        Assert.Equal(revokeAfterCommit ? RotateRefreshTokenStatus.SessionRevoked : RotateRefreshTokenStatus.Success, result.Status);
        Assert.Equal(revokeAfterCommit ? null : "protected-fixture", result.ProtectedCredential);
        await using var verification = sql.CreateDbContext();
        Assert.Equal(2, await verification.RefreshTokens.CountAsync(t => t.SessionId == sessionId));
        Assert.Equal(1, await verification.IdempotencyRecords.CountAsync(r => r.ActorUserId == userId && r.Operation == "RefreshRotation"));
    }

    [Fact]
    public async Task AdditiveMigration_PreservesExpiredAndRevokedLegacy_RejectsPromotionAndDestructiveRollback()
    {
        var owned = new SqlServerTestFixture(createSpatialProbeSchema: false); await owned.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(owned.ConnectionString, x => x.UseNetTopologySuite()).Options;
            await using var db = new RoadGuardDbContext(options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261006015156_H0RetentionIntegration");
            await new IdentityRoleSeedStep().SeedAsync(db, CancellationToken.None);
            var now = DateTimeOffset.UtcNow; var user = NewUser(now); db.Add(user); await db.SaveChangesAsync();
            var expiredId = Guid.NewGuid(); var revokedId = Guid.NewGuid(); var issued = now.AddDays(-5); var expired = now.AddDays(-1); var revoked = now.AddDays(-2);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Sessions(Id,UserId,IssuedAt,ExpiresAt,Transport) VALUES({expiredId},{user.Id},{issued},{expired},0)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Sessions(Id,UserId,IssuedAt,ExpiresAt,RevokedAt,Transport) VALUES({revokedId},{user.Id},{issued},{now.AddDays(5)},{revoked},0)");
            await migrator.MigrateAsync(); db.ChangeTracker.Clear();
            var expiredRow = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == expiredId);
            Assert.Equal(SessionLifecycle.LegacyBounded, expiredRow.Lifecycle); Assert.Null(expiredRow.IssuedRole);
            Assert.Equal(expired, expiredRow.ExpiresAt); Assert.False(expiredRow.IsActiveAt(now));
            var revokedRow = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == revokedId);
            Assert.Equal(revoked, revokedRow.RevokedAt); Assert.False(revokedRow.IsActiveAt(now));
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Sessions SET Lifecycle=1,IssuedRole=N'DroneOperator',ExpiresAt=NULL WHERE Id={expiredId}"));
            db.Add(new UserSession { Id = Guid.NewGuid(), UserId = user.Id, IssuedAt = now, Lifecycle = SessionLifecycle.PersistentRenewable, IssuedRole = user.RoleCode }); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => migrator.MigrateAsync("20261006015156_H0RetentionIntegration"));
            Assert.Equal(3, await db.Sessions.CountAsync(s => s.UserId == user.Id));
        }
        finally { await owned.DisposeAsync(); }
    }

    private sealed class CommitAckLoss(Func<Task> afterCommit) : DbTransactionInterceptor
    {
        private bool fired;
        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { if (fired) return; fired = true; await afterCommit(); throw new TestTransientException("Injected committed ACK loss."); }
    }
    private static ApplicationUser NewUser(DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        UserName = $"h1-sql-{Guid.NewGuid():N}",
        DisplayName = "H1 fixture",
        PasswordHash = "fixture",
        RoleCode = UserRoleCode.DroneOperator,
        Status = UserStatus.Active,
        CreatedAt = now
    };
}
