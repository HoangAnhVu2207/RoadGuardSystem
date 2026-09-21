using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("TaskId", "P1-11")]
public sealed class P111PasswordResetPersistenceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P111PasswordResetPersistenceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P1-11 S4 SQL: admin reset atomically changes credential, revokes active credentials and replays idempotently")]
    public async Task AdminPasswordReset_SuccessAndReplay_AreAtomicAndSecretFree()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var actor = await CreateUserAsync(context, UserRoleCode.Supervisor);
        var target = await CreateUserAsync(context, UserRoleCode.DroneOperator);
        var session = await CreateSessionAsync(context, target.Id);
        var token = await CreateRefreshTokenAsync(context, session.Id);
        var repository = new IdentityRepository(context);
        var operationId = Guid.NewGuid();
        var fingerprint = new string('a', 64);
        var expectedVersion = target.RowVersion.ToArray();

        var first = await repository.ResetUserPasswordAtomicAsync(
            actor.Id,
            target.Id,
            "hashed-temporary-password",
            Guid.NewGuid().ToString(),
            expectedVersion,
            fingerprint,
            operationId);
        var replay = await repository.ResetUserPasswordAtomicAsync(
            actor.Id,
            target.Id,
            "different-hash-must-not-apply",
            Guid.NewGuid().ToString(),
            expectedVersion,
            fingerprint,
            operationId);

        first.Status.Should().Be(AdminPasswordResetStatus.Success);
        replay.Status.Should().Be(AdminPasswordResetStatus.IdempotentReplay);

        var persistedUser = await context.Users.AsNoTracking().SingleAsync(user => user.Id == target.Id);
        persistedUser.PasswordHash.Should().Be("hashed-temporary-password");
        persistedUser.MustChangePassword.Should().BeTrue();
        (await context.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id)).RevokedAt.Should().NotBeNull();
        (await context.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id)).RevokedAt.Should().NotBeNull();
        (await context.PasswordResetLogs.CountAsync(item => item.TargetUserId == target.Id)).Should().Be(1);
        (await context.AuditLogs.CountAsync(item => item.EntityId == target.Id && item.EventType == "user_password_reset"))
            .Should().Be(1);
        (await context.IdempotencyRecords.CountAsync(item => item.Operation == "AdminPasswordReset"))
            .Should().Be(1);
        (await context.AuditLogs.AsNoTracking().Where(item => item.EntityId == target.Id)
            .Select(item => item.BeforeSnapshot + item.AfterSnapshot + item.Reason)
            .ToListAsync())
            .Should().NotContain(value => value!.Contains("hashed-temporary-password", StringComparison.Ordinal));
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        RoadGuardSystem.Repositories.RoadGuardDbContext context,
        UserRoleCode role,
        UserStatus status = UserStatus.Active)
    {
        var username = $"p111_reset_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = username,
            PasswordHash = $"original-hash-{Guid.NewGuid():N}",
            SecurityStamp = Guid.NewGuid().ToString(),
            RoleCode = role,
            Status = status,
            MustChangePassword = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<UserSession> CreateSessionAsync(
        RoadGuardSystem.Repositories.RoadGuardDbContext context,
        Guid userId)
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            DeviceMetadataJson = null
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();
        return session;
    }

    private static async Task<RefreshToken> CreateRefreshTokenAsync(
        RoadGuardSystem.Repositories.RoadGuardDbContext context,
        Guid sessionId)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = new string('c', 64),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        return token;
    }
}
