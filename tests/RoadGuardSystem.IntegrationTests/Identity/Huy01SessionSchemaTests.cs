using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using System.Security.Cryptography;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("Package", "HUY-01")]
public sealed class Huy01SessionSchemaTests(IdentitySqlServerFixture sql)
    : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task SessionSchema_PersistsTransportAndLastActivityWithValidTransportConstraint()
    {
        await using var db = sql.CreateDbContext();

        var columnCount = await db.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.columns
            WHERE [object_id] = OBJECT_ID(N'dbo.Sessions')
              AND [name] IN (N'Transport', N'LastActivityAt')
            """).SingleAsync();
        Assert.Equal(2, columnCount);

        var constraintCount = await db.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.check_constraints
            WHERE [parent_object_id] = OBJECT_ID(N'dbo.Sessions')
              AND [name] = N'CK_Sessions_Transport'
            """).SingleAsync();
        Assert.Equal(1, constraintCount);
    }

    [Fact]
    public async Task SessionIssuance_PreservesTransportAndLastActivity()
    {
        await using var db = sql.CreateDbContext();
        await sql.SeedRolesAsync(db);

        var now = DateTimeOffset.UtcNow;
        var userName = $"huy_session_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            DisplayName = "HUY-01 session transport user",
            PasswordHash = "identity-password-hash",
            RoleCode = UserRoleCode.Reporter,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            ExpiresAt = now.AddHours(1),
            Transport = SessionTransport.Web,
            LastActivityAt = now
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())),
            ExpiresAt = now.AddHours(1)
        };

        var result = await new IdentityRepository(db).IssueSessionWithRefreshTokenAsync(
            user.Id,
            user.RowVersion.ToArray(),
            session,
            token,
            now);

        Assert.Equal(IssueSessionStatus.Success, result.Status);

        await using var verification = sql.CreateDbContext();
        var persisted = await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id);
        Assert.Equal(SessionTransport.Web, persisted.Transport);
        Assert.Equal(now, persisted.LastActivityAt);
    }
}
