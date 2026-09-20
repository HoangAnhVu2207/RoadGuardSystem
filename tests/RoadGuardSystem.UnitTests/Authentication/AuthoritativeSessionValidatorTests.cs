using FluentAssertions;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class AuthoritativeSessionValidatorTests
{
    [Theory(DisplayName = "P1-10 Negative: invalid identity claims fail closed before persistence reads")]
    [InlineData(true, false, UserRoleCode.DroneOperator)]
    [InlineData(false, true, UserRoleCode.DroneOperator)]
    [InlineData(false, false, UserRoleCode.Unknown)]
    public async Task Validate_InvalidIdentityClaims_IsUnauthorized(
        bool emptyUserId,
        bool emptySessionId,
        UserRoleCode roleSnapshot)
    {
        var repository = new StubIdentityRepository();

        var result = await new AuthoritativeSessionValidator(repository).ValidateAsync(
            emptyUserId ? Guid.Empty : Guid.NewGuid(),
            emptySessionId ? Guid.Empty : Guid.NewGuid(),
            roleSnapshot,
            DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.Unauthorized);
    }

    [Fact(DisplayName = "P1-10 Negative: missing session fails closed as revoked")]
    public async Task Validate_MissingSession_IsSessionRevoked()
    {
        var repository = new StubIdentityRepository();

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.DroneOperator, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.SessionRevoked);
    }

    [Fact(DisplayName = "P1-10 Negative: a session owned by another user fails closed as revoked")]
    public async Task Validate_WrongSessionOwner_IsSessionRevoked()
    {
        var sessionId = Guid.NewGuid();
        var sessionOwnerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var callerId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var repository = new StubIdentityRepository
        {
            Session = ActiveSession(sessionId, sessionOwnerId)
        };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(callerId, sessionId, UserRoleCode.DroneOperator, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.SessionRevoked);
    }

    [Theory(DisplayName = "P1-10 Negative: revoked or boundary-expired sessions fail closed")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_RevokedOrExpiredSession_IsSessionRevoked(bool revoked)
    {
        var now = new DateTimeOffset(2026, 9, 19, 3, 30, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            Session = ActiveSession(sessionId, userId, now) with
            {
                ExpiresAt = revoked ? now.AddHours(1) : now,
                RevokedAt = revoked ? now.AddMinutes(-1) : null,
                IsActive = false
            }
        };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(userId, sessionId, UserRoleCode.DroneOperator, now);

        result.Should().Be(AuthoritativeSessionValidation.SessionRevoked);
    }

    [Fact(DisplayName = "P1-10 Negative: missing authoritative user fails closed")]
    public async Task Validate_MissingUser_IsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            Session = ActiveSession(sessionId, userId)
        };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(userId, sessionId, UserRoleCode.DroneOperator, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.Unauthorized);
    }

    [Fact(DisplayName = "P1-10 Negative: stale role fails closed and revokes the token family")]
    public async Task Validate_StaleRole_RevokesSession()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            Session = ActiveSession(sessionId, userId),
            User = ActiveUser(userId, UserRoleCode.ProjectManager)
        };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(userId, sessionId, UserRoleCode.Supervisor, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.SessionRevoked);
        repository.RevokedSessionId.Should().Be(sessionId);
    }

    [Theory(DisplayName = "P1-10 Negative: inactive account states fail closed")]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Pending)]
    [InlineData(UserStatus.Unknown)]
    public async Task Validate_InactiveUser_IsUnauthorized(UserStatus status)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            Session = ActiveSession(sessionId, userId),
            User = ActiveUser(userId, UserRoleCode.RepairCrew) with { Status = status }
        };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(userId, sessionId, UserRoleCode.RepairCrew, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.Unauthorized);
    }

    [Fact(DisplayName = "P1-10 Negative: persistence failure fails closed")]
    public async Task Validate_StoreFailure_IsUnauthorized()
    {
        var repository = new StubIdentityRepository { ThrowOnRead = true };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.DroneOperator, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.Unauthorized);
    }

    [Fact(DisplayName = "P1-10 Positive: current active session and matching authoritative role pass")]
    public async Task Validate_CurrentMatchingState_Succeeds()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            Session = ActiveSession(sessionId, userId),
            User = ActiveUser(userId, UserRoleCode.DroneOperator)
        };

        var result = await new AuthoritativeSessionValidator(repository)
            .ValidateAsync(userId, sessionId, UserRoleCode.DroneOperator, DateTimeOffset.UtcNow);

        result.Should().Be(AuthoritativeSessionValidation.Success);
        repository.RevokedSessionId.Should().BeNull();
    }

    private static SessionSecurityState ActiveSession(
        Guid sessionId,
        Guid userId,
        DateTimeOffset? now = null)
    {
        var current = now ?? DateTimeOffset.UtcNow;
        return new(sessionId, userId, current.AddMinutes(-1), current.AddHours(1), null, true, [1]);
    }

    private static UserSecurityState ActiveUser(Guid userId, UserRoleCode role) =>
        new(userId, "user", "User", role, UserStatus.Active, false, [1]);

    private sealed class StubIdentityRepository : IIdentityRepository
    {
        public UserSecurityState? User { get; init; }
        public SessionSecurityState? Session { get; init; }
        public bool ThrowOnRead { get; init; }
        public Guid? RevokedSessionId { get; private set; }

        public Task<UserProfileState?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileState?>(null);

        public Task<UserProfileUpdateResult> UpdateUserProfileAtomicAsync(
            Guid userId,
            string displayName,
            string? email,
            byte[] expectedRowVersion,
            Guid operationId,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminPasswordResetResult> ResetUserPasswordAtomicAsync(
            Guid actorUserId,
            Guid targetUserId,
            string newPasswordHash,
            string newSecurityStamp,
            byte[] expectedTargetRowVersion,
            string requestFingerprint,
            Guid operationId,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UserSecurityState?> GetUserSecurityStateAsync(Guid userId, CancellationToken cancellationToken = default) =>
            ThrowOnRead ? throw new InvalidOperationException("store unavailable") : Task.FromResult(User);

        public Task<SessionSecurityState?> GetSessionSecurityStateAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            ThrowOnRead ? throw new InvalidOperationException("store unavailable") : Task.FromResult(Session);

        public Task RevokeSessionAndFamilyAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            RevokedSessionId = sessionId;
            return Task.CompletedTask;
        }

        public Task<UserSecurityState?> GetUserSecurityStateByUsernameAsync(string username, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RefreshTokenSecurityState?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RotateRefreshTokenResult> RotateRefreshTokenAsync(Guid oldTokenId, byte[] expectedRowVersion, RoadGuardSystem.BusinessObjects.Identity.RefreshToken newToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IssueSessionResult> IssueSessionWithRefreshTokenAsync(Guid userId, byte[] expectedUserRowVersion, RoadGuardSystem.BusinessObjects.Identity.UserSession session, RoadGuardSystem.BusinessObjects.Identity.RefreshToken initialRefreshToken, DateTimeOffset successfulLoginAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(Guid userId, byte[] expectedUserRowVersion, string newPasswordHash, string newSecurityStamp, Guid operationId, Guid? correlationId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(Guid userId, byte[] expectedUserRowVersion, string newPasswordHash, string newSecurityStamp, string requestFingerprint, Guid operationId, Guid? correlationId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ReplayRevocationResult> RevokeRefreshTokenFamilyForReplayAsync(Guid refreshTokenId, byte[] expectedTokenRowVersion, Guid? correlationId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UserRoleChangeResult> ChangeUserRoleAtomicAsync(Guid userId, UserRoleCode newRoleCode, byte[] expectedRowVersion, Guid actorUserId, Guid operationId, Guid? correlationId = null, string? reason = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
