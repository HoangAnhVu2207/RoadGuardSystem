using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Identity;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("TaskId", "ANH-01")]
public sealed class IdentityOnboardingPersistenceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public IdentityOnboardingPersistenceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExistingReporterEmail_CreatesNoSecondUser_AndReplaysWithoutDisclosure()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var now = DateTimeOffset.UtcNow;
        var email = $"reporter.{Guid.NewGuid():N}@example.test";
        var normalizedEmail = email.ToUpperInvariant();
        var existing = CreateUser(email, UserRoleCode.Reporter, UserStatus.Active, now);
        existing.EmailConfirmed = true;
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var pending = CreateUser(email, UserRoleCode.Reporter, UserStatus.Pending, now);
        var intent = CreateIntent(pending.Id, email, now);
        var repository = new IdentityOnboardingRepository(context);
        var key = Guid.NewGuid().ToString("N");
        var fingerprint = new string('a', 64);

        var result = await repository.RegisterReporterAsync(
            pending, intent, key, fingerprint, Guid.NewGuid());
        var replay = await repository.RegisterReporterAsync(
            pending, intent, key, fingerprint, Guid.NewGuid());
        var conflict = await repository.RegisterReporterAsync(
            pending, intent, key, new string('b', 64), Guid.NewGuid());

        result.Status.Should().Be(IdentityOnboardingPersistenceStatus.Success);
        replay.Status.Should().Be(IdentityOnboardingPersistenceStatus.IdempotentReplay);
        conflict.Status.Should().Be(IdentityOnboardingPersistenceStatus.IdempotentConflict);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Users.CountAsync(user => user.NormalizedEmail == normalizedEmail))
            .Should().Be(1);
        (await verification.ReporterRegistrationIntents.AsNoTracking()
            .SingleAsync(item => item.Id == intent.Id)).UserId.Should().BeNull();
    }

    [Fact]
    public async Task ReporterOtp_ResendAndAttemptLimits_PreserveUnverifiedAccount()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var now = DateTimeOffset.UtcNow;
        var email = $"otp.{Guid.NewGuid():N}@example.test";
        var pending = CreateUser(email, UserRoleCode.Reporter, UserStatus.Pending, now);
        var intent = CreateIntent(pending.Id, email, now);
        context.Users.Add(pending);
        context.ReporterRegistrationIntents.Add(intent);
        await context.SaveChangesAsync();
        var repository = new IdentityOnboardingRepository(context);
        var fingerprint = new string('c', 64);

        for (var index = 0; index < 2; index++)
        {
            var resend = await repository.ResendReporterOtpAsync(
                intent.Id, new string((char)('d' + index), 64), now.AddMinutes(10),
                now, 2, TimeSpan.FromMinutes(15), Guid.NewGuid().ToString("N"),
                fingerprint, Guid.NewGuid(), now);
            resend.Status.Should().Be(IdentityOnboardingPersistenceStatus.Success);
        }

        var limited = await repository.ResendReporterOtpAsync(
            intent.Id, new string('f', 64), now.AddMinutes(10), now, 2,
            TimeSpan.FromMinutes(15), Guid.NewGuid().ToString("N"),
            fingerprint, Guid.NewGuid(), now);
        limited.Status.Should().Be(IdentityOnboardingPersistenceStatus.TooManyRequests);

        var session = new UserSession
        {
            Id = Guid.NewGuid(), UserId = pending.Id, IssuedAt = now,
            ExpiresAt = now.AddHours(1)
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), SessionId = session.Id,
            TokenHash = new string('1', 64), ExpiresAt = now.AddDays(1)
        };
        for (var index = 0; index < 2; index++)
        {
            var wrong = await repository.VerifyReporterOtpAsync(
                intent.Id, new string('0', 64), 2, session, token,
                Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"),
                Guid.NewGuid(), now.AddMinutes(1));
            wrong.Status.Should().Be(IdentityOnboardingPersistenceStatus.InvalidInput);
        }

        var locked = await repository.VerifyReporterOtpAsync(
            intent.Id, new string('e', 64), 2, session, token,
            Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"),
            Guid.NewGuid(), now.AddMinutes(1));
        locked.Status.Should().Be(IdentityOnboardingPersistenceStatus.TooManyRequests);
        var expired = await repository.VerifyReporterOtpAsync(
            intent.Id, new string('e', 64), 2, session, token,
            Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"),
            Guid.NewGuid(), now.AddMinutes(11));
        expired.Status.Should().Be(IdentityOnboardingPersistenceStatus.NotFound);

        await using var verification = _fixture.CreateDbContext();
        var persistedIntent = await verification.ReporterRegistrationIntents.AsNoTracking()
            .SingleAsync(item => item.Id == intent.Id);
        persistedIntent.FailedAttempts.Should().Be(2);
        persistedIntent.ConsumedAt.Should().BeNull();
        (await verification.Users.AsNoTracking().SingleAsync(user => user.Id == pending.Id))
            .Status.Should().Be(UserStatus.Pending);
        (await verification.Sessions.AnyAsync(item => item.Id == session.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Invitation_ExpiresOrReplaysWithoutCreatingAnotherAccount()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var now = DateTimeOffset.UtcNow;
        var supervisor = CreateUser(
            $"supervisor.{Guid.NewGuid():N}@example.test",
            UserRoleCode.Supervisor, UserStatus.Active, now);
        context.Users.Add(supervisor);
        await context.SaveChangesAsync();

        var email = $"invitee.{Guid.NewGuid():N}@example.test";
        var normalizedEmail = email.ToUpperInvariant();
        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(), DisplayName = "Invited Operator", Email = email,
            NormalizedEmail = email.ToUpperInvariant(), RoleCode = UserRoleCode.DroneOperator,
            TokenHash = new string('2', 64), CreatedByUserId = supervisor.Id,
            CreatedAt = now, ExpiresAt = now.AddHours(1)
        };
        var repository = new IdentityOnboardingRepository(context);
        var created = await repository.CreateInvitationAsync(
            invitation, [], Guid.NewGuid().ToString("N"), new string('3', 64), Guid.NewGuid());
        created.Status.Should().Be(IdentityOnboardingPersistenceStatus.Success);

        var user = CreateUser(email, UserRoleCode.DroneOperator, UserStatus.Active, now);
        var session = new UserSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, IssuedAt = now,
            ExpiresAt = now.AddHours(1)
        };
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(), SessionId = session.Id,
            TokenHash = new string('4', 64), ExpiresAt = now.AddDays(1)
        };
        var key = Guid.NewGuid().ToString("N");
        var fingerprint = new string('5', 64);
        var accepted = await repository.AcceptInvitationAsync(
            invitation.TokenHash, user, session, token, key, fingerprint, Guid.NewGuid(), now);
        var replay = await repository.AcceptInvitationAsync(
            invitation.TokenHash, user, session, token, key, fingerprint, Guid.NewGuid(), now);
        var secondAcceptance = await repository.AcceptInvitationAsync(
            invitation.TokenHash, user, session, token,
            Guid.NewGuid().ToString("N"), fingerprint, Guid.NewGuid(), now);

        accepted.Status.Should().Be(IdentityOnboardingPersistenceStatus.Success);
        replay.Status.Should().Be(IdentityOnboardingPersistenceStatus.IdempotentReplay);
        secondAcceptance.Status.Should().Be(IdentityOnboardingPersistenceStatus.Conflict);

        var expiredEmail = $"expired.{Guid.NewGuid():N}@example.test";
        var expired = new StaffInvitation
        {
            Id = Guid.NewGuid(), DisplayName = "Expired Invite",
            Email = expiredEmail, NormalizedEmail = expiredEmail.ToUpperInvariant(),
            RoleCode = UserRoleCode.DroneOperator, TokenHash = new string('6', 64),
            CreatedByUserId = supervisor.Id, CreatedAt = now.AddHours(-2),
            ExpiresAt = now.AddHours(-1)
        };
        context.StaffInvitations.Add(expired);
        await context.SaveChangesAsync();
        var expiredResult = await repository.AcceptInvitationAsync(
            expired.TokenHash, CreateUser(expired.Email, UserRoleCode.DroneOperator, UserStatus.Active, now),
            session, token, Guid.NewGuid().ToString("N"), fingerprint, Guid.NewGuid(), now);
        expiredResult.Status.Should().Be(IdentityOnboardingPersistenceStatus.NotFound);

        await using var verification = _fixture.CreateDbContext();
        (await verification.Users.CountAsync(item => item.NormalizedEmail == normalizedEmail))
            .Should().Be(1);
        (await verification.Sessions.CountAsync(item => item.UserId == user.Id)).Should().Be(1);
        (await verification.RefreshTokens.CountAsync(item => item.SessionId == session.Id)).Should().Be(1);
        (await verification.StaffInvitations.AsNoTracking()
            .SingleAsync(item => item.Id == invitation.Id)).AcceptedAt.Should().NotBeNull();
    }

    private static ApplicationUser CreateUser(
        string email, UserRoleCode role, UserStatus status, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), UserName = email, NormalizedUserName = email.ToUpperInvariant(),
        Email = email, NormalizedEmail = email.ToUpperInvariant(),
        DisplayName = email, PasswordHash = "test-password-hash",
        RoleCode = role, Status = status, CreatedAt = now
    };

    private static ReporterRegistrationIntent CreateIntent(
        Guid userId, string email, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, NormalizedEmail = email.ToUpperInvariant(),
        ReporterType = ReporterType.Citizen, OtpHash = new string('9', 64),
        OtpGeneration = 1, ExpiresAt = now.AddMinutes(10),
        ResendAvailableAt = now, CreatedAt = now
    };
}
