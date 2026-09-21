using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Extensions;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Options;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Identity;

[Trait("TaskId", "P1-10")]
public sealed class P110AuthenticationPersistenceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P110AuthenticationPersistenceTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P1-10 Positive: persistence DI resolves the authoritative identity repository")]
    public async Task PersistenceDi_ValidConfiguration_ResolvesIdentityRepository()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RoadGuardDatabaseOptions.SectionName}:ConnectionString"] = _fixture.ConnectionString,
                [$"{RoadGuardDatabaseOptions.SectionName}:EnableSensitiveDataLogging"] = "false"
            })
            .Build();
        services.AddRoadGuardPersistence(configuration, isProduction: false);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIdentityRepository>()
            .Should().BeOfType<IdentityRepository>();
    }

    [Fact(DisplayName = "P1-10 Negative: stale user version rejects initial credential issuance without writes")]
    public async Task InitialIssuance_StaleUserVersion_RejectsWithoutWrites()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context);
        var issuedAt = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, issuedAt);
        var token = CreateRefreshToken(session.Id, issuedAt, UniqueHash("stale"));
        var staleVersion = user.RowVersion.ToArray();

        await context.Database.ExecuteSqlAsync(
            $"UPDATE [Users] SET [LastLoginAt] = {issuedAt.AddMinutes(-1)} WHERE [Id] = {user.Id}");

        var result = await _fixture.CreateRepository(context).IssueSessionWithRefreshTokenAsync(
            user.Id,
            staleVersion,
            session,
            token,
            issuedAt);

        result.Status.Should().Be(IssueSessionStatus.StaleConcurrency);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AnyAsync(item => item.Id == session.Id)).Should().BeFalse();
        (await verification.RefreshTokens.AnyAsync(item => item.Id == token.Id)).Should().BeFalse();
    }

    [Fact(DisplayName = "P1-10 Negative: duplicate refresh hash rolls back session and successful-login update")]
    public async Task InitialIssuance_DuplicateRefreshHash_RollsBackAllEffects()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context);
        var issuedAt = DateTimeOffset.UtcNow;
        var duplicateHash = UniqueHash("duplicate");

        var existingSession = CreateSession(user.Id, issuedAt.AddMinutes(-2));
        var existingToken = CreateRefreshToken(existingSession.Id, issuedAt.AddMinutes(-2), duplicateHash);
        context.Sessions.Add(existingSession);
        context.RefreshTokens.Add(existingToken);
        await context.SaveChangesAsync();
        await context.Entry(user).ReloadAsync();

        var attemptedSession = CreateSession(user.Id, issuedAt);
        var attemptedToken = CreateRefreshToken(attemptedSession.Id, issuedAt, duplicateHash);
        var result = await _fixture.CreateRepository(context).IssueSessionWithRefreshTokenAsync(
            user.Id,
            user.RowVersion.ToArray(),
            attemptedSession,
            attemptedToken,
            issuedAt);

        result.Status.Should().Be(IssueSessionStatus.DuplicateCredential);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AnyAsync(item => item.Id == attemptedSession.Id)).Should().BeFalse();
        (await verification.RefreshTokens.AnyAsync(item => item.Id == attemptedToken.Id)).Should().BeFalse();
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
            .LastLoginAt.Should().BeNull();
    }

    [Fact(DisplayName = "P1-10 Positive: initial session and refresh hash commit atomically")]
    public async Task InitialIssuance_ValidInput_CommitsSessionTokenAndLoginState()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context);
        var issuedAt = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, issuedAt);
        var token = CreateRefreshToken(session.Id, issuedAt, UniqueHash("accepted"));

        var result = await _fixture.CreateRepository(context).IssueSessionWithRefreshTokenAsync(
            user.Id,
            user.RowVersion.ToArray(),
            session,
            token,
            issuedAt);

        result.Status.Should().Be(IssueSessionStatus.Success);
        result.SessionId.Should().Be(session.Id);

        await using var verification = _fixture.CreateDbContext();
        var persistedSession = await verification.Sessions.AsNoTracking()
            .SingleAsync(item => item.Id == session.Id);
        var persistedToken = await verification.RefreshTokens.AsNoTracking()
            .SingleAsync(item => item.Id == token.Id);
        var persistedUser = await verification.Users.AsNoTracking()
            .SingleAsync(item => item.Id == user.Id);

        persistedSession.UserId.Should().Be(user.Id);
        persistedToken.SessionId.Should().Be(session.Id);
        persistedToken.TokenHash.Should().Be(token.TokenHash);
        persistedUser.LastLoginAt.Should().Be(issuedAt);
    }

    [Fact(DisplayName = "P1-10 F-01: initial issuance rebuilds tracked state after a rolled-back retry")]
    public async Task InitialIssuance_TransientCommitFailure_RetryPersistsAllEffects()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup);
        var expectedVersion = user.RowVersion.ToArray();
        var issuedAt = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, issuedAt);
        var token = CreateRefreshToken(session.Id, issuedAt, UniqueHash("issuance-retry"));
        var interceptor = new FailFirstCommitInterceptor();

        await using var context = _fixture.CreateRetryingDbContext(interceptor);
        var result = await _fixture.CreateRepository(context).IssueSessionWithRefreshTokenAsync(
            user.Id,
            expectedVersion,
            session,
            token,
            issuedAt);

        result.Status.Should().Be(IssueSessionStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.CountAsync(item => item.Id == session.Id)).Should().Be(1);
        (await verification.RefreshTokens.CountAsync(item => item.Id == token.Id)).Should().Be(1);
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
            .LastLoginAt.Should().Be(issuedAt);
    }

    [Fact(DisplayName = "P1-10 F-01: initial issuance verifies a durable unknown commit")]
    public async Task InitialIssuance_PostCommitAcknowledgmentFailure_ReturnsDurableSuccess()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup);
        var issuedAt = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, issuedAt);
        var token = CreateRefreshToken(session.Id, issuedAt, UniqueHash("issuance-unknown-commit"));
        var interceptor = new FailFirstCommittedInterceptor();

        await using var context = _fixture.CreateRetryingDbContext(interceptor);
        var result = await _fixture.CreateRepository(context).IssueSessionWithRefreshTokenAsync(
            user.Id, user.RowVersion.ToArray(), session, token, issuedAt);

        result.Status.Should().Be(IssueSessionStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.CountAsync(item => item.Id == session.Id)).Should().Be(1);
        (await verification.RefreshTokens.CountAsync(item => item.Id == token.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 Negative: stale forced password change preserves password credentials and audit")]
    public async Task ForcedPasswordChange_StaleUserVersion_RollsBackAllEffects()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context, mustChangePassword: true);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("forced-stale"));
        context.Sessions.Add(session);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        await context.Entry(user).ReloadAsync();
        var staleVersion = user.RowVersion.ToArray();

        await context.Database.ExecuteSqlAsync(
            $"UPDATE [Users] SET [LastLoginAt] = {now.AddMinutes(-2)} WHERE [Id] = {user.Id}");

        var result = await _fixture.CreateRepository(context).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            staleVersion,
            "new-identity-password-hash",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Status.Should().Be(ForcedPasswordChangeStatus.StaleConcurrency);
        await using var verification = _fixture.CreateDbContext();
        var persistedUser = await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        var persistedSession = await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id);
        var persistedToken = await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id);
        persistedUser.PasswordHash.Should().Be("identity-password-hash");
        persistedUser.MustChangePassword.Should().BeTrue();
        persistedSession.RevokedAt.Should().BeNull();
        persistedToken.RevokedAt.Should().BeNull();
        (await verification.AuditLogs.CountAsync(item => item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 Positive: forced password change revokes credentials audits once and replays idempotently")]
    public async Task ForcedPasswordChange_ValidInput_CommitsAndDuplicateRetryIsIdempotent()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context, mustChangePassword: true);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("forced-ok"));
        context.Sessions.Add(session);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        await context.Entry(user).ReloadAsync();

        const string newPasswordHash = "new-identity-password-hash";
        var newSecurityStamp = Guid.NewGuid().ToString();
        var operationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var repository = _fixture.CreateRepository(context);

        var result = await repository.CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            user.RowVersion.ToArray(),
            newPasswordHash,
            newSecurityStamp,
            operationId,
            correlationId);

        result.Status.Should().Be(ForcedPasswordChangeStatus.Success);

        var replay = await repository.CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            [0],
            newPasswordHash,
            newSecurityStamp,
            operationId,
            Guid.NewGuid());
        replay.Status.Should().Be(ForcedPasswordChangeStatus.IdempotentReplay);

        await using var verification = _fixture.CreateDbContext();
        var persistedUser = await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        var persistedSession = await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id);
        var persistedToken = await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id);
        var audits = await verification.AuditLogs.AsNoTracking()
            .Where(item => item.EntityId == user.Id && item.EventType == "auth_password_changed")
            .ToListAsync();

        persistedUser.PasswordHash.Should().Be(newPasswordHash);
        persistedUser.SecurityStamp.Should().Be(newSecurityStamp);
        persistedUser.MustChangePassword.Should().BeFalse();
        persistedSession.RevokedAt.Should().NotBeNull();
        persistedToken.RevokedAt.Should().NotBeNull();
        audits.Should().ContainSingle();
        audits[0].CorrelationId.Should().Be(correlationId);
        audits[0].BeforeSnapshot.Should().Be("{\"must_change_password\":true}");
        audits[0].AfterSnapshot.Should().Be("{\"must_change_password\":false}");
        string.Join('|', audits[0].BeforeSnapshot, audits[0].AfterSnapshot, audits[0].Reason, audits[0].Source)
            .Should().NotContain(newPasswordHash);
    }

    [Fact(DisplayName = "P1-10 Negative: forced password change retry with a changed payload conflicts")]
    public async Task ForcedPasswordChange_ChangedPayloadRetry_IsIdempotentConflict()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context, mustChangePassword: true);
        var operationId = Guid.NewGuid();
        var repository = _fixture.CreateRepository(context);

        var first = await repository.CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            user.RowVersion.ToArray(),
            "first-password-hash",
            "first-security-stamp",
            operationId);
        first.Status.Should().Be(ForcedPasswordChangeStatus.Success);

        var conflict = await repository.CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            [0],
            "different-password-hash",
            "different-security-stamp",
            operationId);

        conflict.Status.Should().Be(ForcedPasswordChangeStatus.IdempotentConflict);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
            .PasswordHash.Should().Be("first-password-hash");
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 Negative: concurrent forced password change commits and audits exactly once")]
    public async Task ForcedPasswordChange_ConcurrentDuplicate_OneSuccessOneReplayAndOneAudit()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup, mustChangePassword: true);
        var expectedVersion = user.RowVersion.ToArray();
        var operationId = Guid.NewGuid();
        var barrier = new SqlCommandBarrierInterceptor(
            commandText => commandText.Contains("UPDATE [Users]", StringComparison.OrdinalIgnoreCase));

        await using var firstContext = _fixture.CreateDbContext(barrier);
        await using var secondContext = _fixture.CreateDbContext(barrier);
        var first = _fixture.CreateRepository(firstContext).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            expectedVersion,
            "concurrent-password-hash",
            "concurrent-security-stamp",
            operationId,
            Guid.NewGuid());
        var second = _fixture.CreateRepository(secondContext).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            expectedVersion,
            "concurrent-password-hash",
            "concurrent-security-stamp",
            operationId,
            Guid.NewGuid());

        var results = await Task.WhenAll(first, second);

        barrier.ArrivalCount.Should().Be(2);
        results.Select(item => item.Status).Should().ContainSingle(status => status == ForcedPasswordChangeStatus.Success);
        results.Select(item => item.Status).Should().ContainSingle(status => status == ForcedPasswordChangeStatus.IdempotentReplay);
        await using var verification = _fixture.CreateDbContext();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-01: forced password change rebuilds tracked state after a rolled-back retry")]
    public async Task ForcedPasswordChange_TransientCommitFailure_RetryPersistsAllEffects()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup, mustChangePassword: true);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("forced-retry"));
        setup.Sessions.Add(session);
        setup.RefreshTokens.Add(token);
        await setup.SaveChangesAsync();
        await setup.Entry(user).ReloadAsync();

        var interceptor = new FailFirstCommitInterceptor();
        await using var context = _fixture.CreateRetryingDbContext(interceptor);
        var result = await _fixture.CreateRepository(context).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            user.RowVersion.ToArray(),
            "retry-password-hash",
            "retry-security-stamp",
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Status.Should().Be(ForcedPasswordChangeStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        var persistedUser = await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        persistedUser.MustChangePassword.Should().BeFalse();
        persistedUser.PasswordHash.Should().Be("retry-password-hash");
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-01: forced password change verifies a durable unknown commit")]
    public async Task ForcedPasswordChange_PostCommitAcknowledgmentFailure_ReturnsDurableSuccess()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup, mustChangePassword: true);
        var interceptor = new FailFirstCommittedInterceptor();
        await using var context = _fixture.CreateRetryingDbContext(interceptor);

        var result = await _fixture.CreateRepository(context).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            user.RowVersion.ToArray(),
            "unknown-commit-password-hash",
            "unknown-commit-security-stamp",
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Status.Should().Be(ForcedPasswordChangeStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id))
            .MustChangePassword.Should().BeFalse();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-01: forced password change rebuilds state after a pre-save query retry")]
    public async Task ForcedPasswordChange_TransientSessionQueryFailure_RetryPersistsAllEffects()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup, mustChangePassword: true);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("forced-query-retry"));
        setup.Sessions.Add(session);
        setup.RefreshTokens.Add(token);
        await setup.SaveChangesAsync();
        await setup.Entry(user).ReloadAsync();
        var interceptor = new ThrowOnceCommandInterceptor(
            commandText => commandText.Contains("FROM [Sessions]", StringComparison.OrdinalIgnoreCase));

        await using var context = _fixture.CreateRetryingDbContext(interceptor);
        var result = await _fixture.CreateRepository(context).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            user.RowVersion.ToArray(),
            "query-retry-password-hash",
            "query-retry-security-stamp",
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Status.Should().Be(ForcedPasswordChangeStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        var persistedUser = await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        persistedUser.MustChangePassword.Should().BeFalse();
        persistedUser.PasswordHash.Should().Be("query-retry-password-hash");
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 AC-08: duplicate logout is a no-op that preserves revocation state")]
    public async Task Logout_DuplicateDelivery_DoesNotRewriteSessionOrToken()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("logout-idempotent"));
        setup.Sessions.Add(session);
        setup.RefreshTokens.Add(token);
        await setup.SaveChangesAsync();

        await using (var firstContext = _fixture.CreateDbContext())
        {
            await _fixture.CreateRepository(firstContext).RevokeSessionAndFamilyAsync(session.Id);
        }

        DateTimeOffset firstRevokedAt;
        byte[] firstSessionVersion;
        byte[] firstTokenVersion;
        await using (var snapshot = _fixture.CreateDbContext())
        {
            var persistedSession = await snapshot.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id);
            var persistedToken = await snapshot.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id);
            firstRevokedAt = persistedSession.RevokedAt!.Value;
            firstSessionVersion = persistedSession.RowVersion.ToArray();
            firstTokenVersion = persistedToken.RowVersion.ToArray();
        }

        await using (var duplicateContext = _fixture.CreateDbContext())
        {
            await _fixture.CreateRepository(duplicateContext).RevokeSessionAndFamilyAsync(session.Id);
        }

        await using var verification = _fixture.CreateDbContext();
        var finalSession = await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id);
        var finalToken = await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id);
        finalSession.RevokedAt.Should().Be(firstRevokedAt);
        finalSession.RowVersion.Should().Equal(firstSessionVersion);
        finalToken.RowVersion.Should().Equal(firstTokenVersion);
    }

    [Fact(DisplayName = "P1-10 VG-04: concurrent logout writers converge on one durable revocation")]
    public async Task Logout_ConcurrentDelivery_ConvergesOnDurableRevocation()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("logout-concurrent"));
        setup.Sessions.Add(session);
        setup.RefreshTokens.Add(token);
        await setup.SaveChangesAsync();

        var pauseFirstUpdate = new PauseOnceCommandInterceptor(
            commandText => commandText.Contains("UPDATE [Sessions]", StringComparison.OrdinalIgnoreCase));
        await using var firstContext = _fixture.CreateDbContext(pauseFirstUpdate);
        var firstLogout = _fixture.CreateRepository(firstContext).RevokeSessionAndFamilyAsync(session.Id);

        await pauseFirstUpdate.Reached.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await using var secondContext = _fixture.CreateDbContext();
            await _fixture.CreateRepository(secondContext).RevokeSessionAndFamilyAsync(session.Id);
        }
        finally
        {
            pauseFirstUpdate.Release();
        }

        await firstLogout;
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id))
            .RevokedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "P1-10 VG-04: logout recovers after an ambiguous durable commit")]
    public async Task Logout_PostCommitAcknowledgmentFailure_ReturnsDurableSuccess()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var token = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("logout-unknown-commit"));
        setup.Sessions.Add(session);
        setup.RefreshTokens.Add(token);
        await setup.SaveChangesAsync();
        var interceptor = new FailFirstCommittedInterceptor();
        await using var context = _fixture.CreateRetryingDbContext(interceptor);

        await _fixture.CreateRepository(context).RevokeSessionAndFamilyAsync(session.Id);

        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == session.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == token.Id))
            .RevokedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "P1-10 F-02: duplicate after idempotency miss resolves the committed winner")]
    public async Task ForcedPasswordChange_SecondReadsUserAfterWinnerCommits_ReturnsReplay()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup, mustChangePassword: true);
        var expectedVersion = user.RowVersion.ToArray();
        var operationId = Guid.NewGuid();
        var pauseSecondUserRead = new PauseOnceCommandInterceptor(
            commandText => commandText.Contains("FROM [Users]", StringComparison.OrdinalIgnoreCase));

        await using var secondContext = _fixture.CreateDbContext(pauseSecondUserRead);
        var secondTask = _fixture.CreateRepository(secondContext).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            expectedVersion,
            "ordered-password-hash",
            "ordered-security-stamp",
            operationId,
            Guid.NewGuid());

        await pauseSecondUserRead.Reached.WaitAsync(TimeSpan.FromSeconds(20));
        await using var firstContext = _fixture.CreateDbContext();
        var first = await _fixture.CreateRepository(firstContext).CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            expectedVersion,
            "ordered-password-hash",
            "ordered-security-stamp",
            operationId,
            Guid.NewGuid());
        pauseSecondUserRead.Release();
        var second = await secondTask;

        first.Status.Should().Be(ForcedPasswordChangeStatus.Success);
        second.Status.Should().Be(ForcedPasswordChangeStatus.IdempotentReplay);
        await using var verification = _fixture.CreateDbContext();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "auth_password_changed"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 Negative: stale replay token version preserves the active family and audit history")]
    public async Task ReplayRevocation_StaleTokenVersion_RejectsWithoutEffects()
    {
        await using var context = _fixture.CreateDbContext();
        var family = await CreateReplayFamilyAsync(context, "replay-stale");
        var staleVersion = family.ReplayedToken.RowVersion.ToArray();

        await context.Database.ExecuteSqlAsync(
            $"UPDATE [RefreshTokens] SET [RevokedAt] = {DateTimeOffset.UtcNow.AddMinutes(-2)} WHERE [Id] = {family.ReplayedToken.Id}");

        var result = await _fixture.CreateRepository(context).RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            staleVersion,
            Guid.NewGuid());

        result.Status.Should().Be(ReplayRevocationStatus.StaleConcurrency);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == family.Session.Id))
            .RevokedAt.Should().BeNull();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == family.ActiveSibling.Id))
            .RevokedAt.Should().BeNull();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == family.Session.Id && item.EventType == "auth_token_replay_detected"))
            .Should().Be(0);
    }

    [Fact(DisplayName = "P1-10 Positive: replay revokes the family and duplicate retry appends one sanitized audit")]
    public async Task ReplayRevocation_ValidReplay_RevokesFamilyAndAuditsExactlyOnce()
    {
        await using var context = _fixture.CreateDbContext();
        var family = await CreateReplayFamilyAsync(context, "replay-once");
        var correlationId = Guid.NewGuid();
        var repository = _fixture.CreateRepository(context);

        var result = await repository.RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            family.ReplayedToken.RowVersion.ToArray(),
            correlationId);
        result.Status.Should().Be(ReplayRevocationStatus.Success);

        var replay = await repository.RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            [0],
            Guid.NewGuid());
        replay.Status.Should().Be(ReplayRevocationStatus.IdempotentReplay);

        await using var verification = _fixture.CreateDbContext();
        var session = await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == family.Session.Id);
        var sibling = await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == family.ActiveSibling.Id);
        var audits = await verification.AuditLogs.AsNoTracking()
            .Where(item => item.EntityId == family.Session.Id && item.EventType == "auth_token_replay_detected")
            .ToListAsync();

        session.RevokedAt.Should().NotBeNull();
        sibling.RevokedAt.Should().NotBeNull();
        audits.Should().ContainSingle();
        audits[0].CorrelationId.Should().Be(correlationId);
        audits[0].BeforeSnapshot.Should().BeNull();
        audits[0].AfterSnapshot.Should().BeNull();
        string.Join('|', audits[0].Reason, audits[0].Source)
            .Should().NotContain(family.ReplayedToken.TokenHash);
    }

    [Fact(DisplayName = "P1-10 Negative: concurrent replay handling revokes once and appends exactly one audit")]
    public async Task ReplayRevocation_ConcurrentDuplicate_OneSuccessOneReplayAndOneAudit()
    {
        await using var setup = _fixture.CreateDbContext();
        var family = await CreateReplayFamilyAsync(setup, "replay-race");
        var expectedVersion = family.ReplayedToken.RowVersion.ToArray();
        var barrier = new SqlCommandBarrierInterceptor(
            commandText => commandText.Contains("UPDATE [Sessions]", StringComparison.OrdinalIgnoreCase));

        await using var firstContext = _fixture.CreateDbContext(barrier);
        await using var secondContext = _fixture.CreateDbContext(barrier);
        var first = _fixture.CreateRepository(firstContext).RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            expectedVersion,
            Guid.NewGuid());
        var second = _fixture.CreateRepository(secondContext).RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            expectedVersion,
            Guid.NewGuid());

        var results = await Task.WhenAll(first, second);

        barrier.ArrivalCount.Should().Be(2);
        results.Select(item => item.Status).Should().ContainSingle(status => status == ReplayRevocationStatus.Success);
        results.Select(item => item.Status).Should().ContainSingle(status => status == ReplayRevocationStatus.IdempotentReplay);
        await using var verification = _fixture.CreateDbContext();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == family.Session.Id && item.EventType == "auth_token_replay_detected"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-01: replay revocation rebuilds tracked state after a rolled-back retry")]
    public async Task ReplayRevocation_TransientCommitFailure_RetryPersistsFamilyRevocationAndOneAudit()
    {
        await using var setup = _fixture.CreateDbContext();
        var family = await CreateReplayFamilyAsync(setup, "replay-retry");
        var interceptor = new FailFirstCommitInterceptor();

        await using var context = _fixture.CreateRetryingDbContext(interceptor);
        var result = await _fixture.CreateRepository(context).RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            family.ReplayedToken.RowVersion.ToArray(),
            Guid.NewGuid());

        result.Status.Should().Be(ReplayRevocationStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == family.Session.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == family.ActiveSibling.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == family.Session.Id && item.EventType == "auth_token_replay_detected"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-01: replay revocation verifies a durable unknown commit")]
    public async Task ReplayRevocation_PostCommitAcknowledgmentFailure_ReturnsDurableSuccess()
    {
        await using var setup = _fixture.CreateDbContext();
        var family = await CreateReplayFamilyAsync(setup, "replay-unknown-commit");
        var interceptor = new FailFirstCommittedInterceptor();
        await using var context = _fixture.CreateRetryingDbContext(interceptor);

        var result = await _fixture.CreateRepository(context).RevokeRefreshTokenFamilyForReplayAsync(
            family.ReplayedToken.Id,
            family.ReplayedToken.RowVersion.ToArray(),
            Guid.NewGuid());

        result.Status.Should().Be(ReplayRevocationStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.Sessions.AsNoTracking().SingleAsync(item => item.Id == family.Session.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == family.Session.Id && item.EventType == "auth_token_replay_detected"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-01: refresh rotation verifies a durable unknown commit")]
    public async Task RefreshRotation_PostCommitAcknowledgmentFailure_ReturnsDurableSuccess()
    {
        await using var setup = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(setup);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var oldToken = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("rotate-old"));
        setup.Sessions.Add(session);
        setup.RefreshTokens.Add(oldToken);
        await setup.SaveChangesAsync();
        await setup.Entry(oldToken).ReloadAsync();
        var replacement = CreateRefreshToken(session.Id, now, UniqueHash("rotate-new"));
        var interceptor = new FailFirstCommittedInterceptor();
        await using var context = _fixture.CreateRetryingDbContext(interceptor);

        var result = await _fixture.CreateRepository(context).RotateRefreshTokenAsync(
            oldToken.Id,
            oldToken.RowVersion.ToArray(),
            replacement);

        result.Status.Should().Be(RotateRefreshTokenStatus.Success);
        interceptor.FailureCount.Should().Be(1);
        await using var verification = _fixture.CreateDbContext();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == oldToken.Id))
            .RevokedAt.Should().NotBeNull();
        (await verification.RefreshTokens.CountAsync(item => item.Id == replacement.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "P1-10 F-06: refresh rotation result uses the persisted parent session")]
    public async Task RefreshRotation_MismatchedReplacementSession_ReturnsPersistedSessionId()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await CreateActiveUserAsync(context);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-1));
        var oldToken = CreateRefreshToken(session.Id, now.AddMinutes(-1), UniqueHash("normalize-old"));
        context.Sessions.Add(session);
        context.RefreshTokens.Add(oldToken);
        await context.SaveChangesAsync();
        await context.Entry(oldToken).ReloadAsync();
        var replacement = CreateRefreshToken(Guid.NewGuid(), now, UniqueHash("normalize-new"));

        var result = await _fixture.CreateRepository(context).RotateRefreshTokenAsync(
            oldToken.Id,
            oldToken.RowVersion.ToArray(),
            replacement);

        result.Status.Should().Be(RotateRefreshTokenStatus.Success);
        result.NewToken!.SessionId.Should().Be(session.Id);
        await using var verification = _fixture.CreateDbContext();
        (await verification.RefreshTokens.AsNoTracking().SingleAsync(item => item.Id == replacement.Id))
            .SessionId.Should().Be(session.Id);
    }

    private async Task<ApplicationUser> CreateActiveUserAsync(
        RoadGuardSystem.Repositories.RoadGuardDbContext context,
        bool mustChangePassword = false,
        UserStatus status = UserStatus.Active)
    {
        await _fixture.SeedRolesAsync(context);
        var username = $"p110_auth_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = "P1-10 authentication user",
            PasswordHash = "identity-password-hash",
            RoleCode = UserRoleCode.DroneOperator,
            Status = status,
            MustChangePassword = mustChangePassword,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static UserSession CreateSession(Guid userId, DateTimeOffset issuedAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        IssuedAt = issuedAt,
        ExpiresAt = issuedAt.AddHours(8),
        DeviceMetadataJson = "{\"schema_version\":1,\"platform\":\"Web\"}"
    };

    private static RefreshToken CreateRefreshToken(
        Guid sessionId,
        DateTimeOffset issuedAt,
        string tokenHash) => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            ExpiresAt = issuedAt.AddDays(30)
        };

    private static string UniqueHash(string prefix) =>
        $"{prefix}_{Guid.NewGuid():N}_000000000000000000000000";

    private async Task<ReplayFamily> CreateReplayFamilyAsync(
        RoadGuardSystem.Repositories.RoadGuardDbContext context,
        string hashPrefix)
    {
        var user = await CreateActiveUserAsync(context);
        var now = DateTimeOffset.UtcNow;
        var session = CreateSession(user.Id, now.AddMinutes(-5));
        var replayedToken = CreateRefreshToken(session.Id, now.AddMinutes(-5), UniqueHash(hashPrefix));
        replayedToken.RevokedAt = now.AddMinutes(-1);
        var activeSibling = CreateRefreshToken(session.Id, now.AddMinutes(-4), UniqueHash($"{hashPrefix}-sibling"));
        context.Sessions.Add(session);
        context.RefreshTokens.AddRange(replayedToken, activeSibling);
        await context.SaveChangesAsync();
        return new ReplayFamily(session, replayedToken, activeSibling);
    }

    private sealed record ReplayFamily(
        UserSession Session,
        RefreshToken ReplayedToken,
        RefreshToken ActiveSibling);
}
