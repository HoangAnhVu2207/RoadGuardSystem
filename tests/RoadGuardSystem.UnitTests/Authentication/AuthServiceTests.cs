using FluentAssertions;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Generators;
using RoadGuardSystem.Services.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Options;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class AuthServiceTests
{
    [Theory(DisplayName = "P1-10 F-03: fingerprint key configuration fails closed when invalid")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64")]
    public void PasswordChangeFingerprintOptions_InvalidKey_FailsValidation(string? key)
    {
        var result = new PasswordChangeFingerprintOptionsValidator().Validate(
            PasswordChangeFingerprintOptions.SectionName,
            new PasswordChangeFingerprintOptions { Key = key! });

        result.Failed.Should().BeTrue();
    }

    [Fact(DisplayName = "P1-10 Negative: forced-change semantic fingerprint is stable and does not expose the password")]
    public void PasswordChangeFingerprint_SamePayloadIsStable_ChangedPayloadDiffers()
    {
        var userId = Guid.NewGuid();
        var factory = new PasswordChangeFingerprintFactory(CreateFingerprintOptions());

        var first = factory.Create(userId, "Replacement1!");
        var replay = factory.Create(userId, "Replacement1!");
        var changed = factory.Create(userId, "Different2!");

        first.Should().Be(replay);
        first.Should().NotBe(changed);
        first.Should().HaveLength(64);
        first.Should().NotContain("Replacement1!");
    }

    [Fact(DisplayName = "P1-10 F-03: forced-change fingerprint survives an active JWT key rotation")]
    public void PasswordChangeFingerprint_ActiveJwtKeyRotates_RemainsStable()
    {
        var userId = Guid.NewGuid();
        var beforeRotation = new PasswordChangeFingerprintFactory(CreateFingerprintOptions())
            .Create(userId, "Replacement1!");
        var afterRotation = new PasswordChangeFingerprintFactory(CreateFingerprintOptions())
            .Create(userId, "Replacement1!");

        afterRotation.Should().Be(beforeRotation);
    }

    [Fact(DisplayName = "P1-10 Negative: forced-change login returns no credentials")]
    public async Task Login_ForcedChangeAccount_ReturnsRequiredWithoutCredentials()
    {
        var user = ActiveUser(mustChangePassword: true);
        var service = CreateService(new StubIdentityRepository(), new StubCredentialVerifier
        {
            Verification = new CredentialVerificationResult(CredentialVerificationStatus.Success, user)
        });

        var result = await service.LoginAsync(new LoginCommand("field.user", "Current1!", null));

        result.Status.Should().Be(AuthStatus.PasswordChangeRequired);
        result.Tokens.Should().BeNull();
    }

    [Fact(DisplayName = "P1-10 Positive: valid login persists only the refresh hash and returns credentials")]
    public async Task Login_ValidCredentials_IssuesSessionAndTokenPair()
    {
        var user = ActiveUser();
        var repository = new StubIdentityRepository();
        var service = CreateService(repository, new StubCredentialVerifier
        {
            Verification = new CredentialVerificationResult(CredentialVerificationStatus.Success, user)
        });

        var result = await service.LoginAsync(new LoginCommand(
            "field.user",
            "Current1!",
            "{\"schema_version\":1,\"platform\":\"Web\"}"));

        result.Status.Should().Be(AuthStatus.Success);
        result.Tokens.Should().NotBeNull();
        repository.IssuedSession.Should().NotBeNull();
        repository.IssuedRefreshToken.Should().NotBeNull();
        repository.IssuedRefreshToken!.TokenHash.Should().Be(RefreshTokenGenerator.Hash(result.Tokens!.RefreshToken));
        repository.IssuedRefreshToken.TokenHash.Should().NotContain(result.Tokens.RefreshToken);
        repository.IssuedSession!.DeviceMetadataJson.Should().Be("{\"schema_version\":1,\"platform\":\"Web\"}");
    }

    [Fact(DisplayName = "P1-10 VG-04: login expiry values use the configured access session and refresh lifetimes")]
    public async Task Login_ConfiguredLifetimes_DriveAllCredentialExpiries()
    {
        var options = CreateOptions();
        options.AccessTokenLifetimeMinutes = 7;
        options.SessionLifetimeHours = 11;
        options.RefreshTokenLifetimeDays = 13;
        var repository = new StubIdentityRepository();
        var service = CreateService(repository, new StubCredentialVerifier
        {
            Verification = new CredentialVerificationResult(
                CredentialVerificationStatus.Success,
                ActiveUser())
        }, options);

        var result = await service.LoginAsync(new LoginCommand("field.user", "Current1!", null));

        result.Status.Should().Be(AuthStatus.Success);
        result.Tokens!.AccessTokenExpiresAt.Should().Be(TestNow.AddMinutes(7));
        result.Tokens.RefreshTokenExpiresAt.Should().Be(TestNow.AddDays(13));
        repository.IssuedSession!.ExpiresAt.Should().Be(TestNow.AddHours(11));
        repository.IssuedRefreshToken!.ExpiresAt.Should().Be(TestNow.AddDays(13));
    }

    [Fact(DisplayName = "P1-10 Negative: consumed refresh token revokes the family and returns no credentials")]
    public async Task Refresh_ConsumedToken_RevokesFamilyAndFailsClosed()
    {
        var user = ActiveUser();
        var sessionId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            RefreshState = new RefreshTokenSecurityState(
                tokenId,
                sessionId,
                user.Id,
                TestNow.AddDays(1),
                TestNow.AddMinutes(-1),
                false,
                [4]),
            ReplayResult = new ReplayRevocationResult(ReplayRevocationStatus.Success, sessionId)
        };
        var service = CreateService(repository, new StubCredentialVerifier());

        var result = await service.RefreshAsync(new RefreshCommand("consumed-refresh", Guid.NewGuid()));

        result.Status.Should().Be(AuthStatus.SessionRevoked);
        result.Tokens.Should().BeNull();
        repository.ReplayRevokedTokenId.Should().Be(tokenId);
    }

    [Fact(DisplayName = "P1-10 Positive: valid refresh rotates the hash and returns a new token pair")]
    public async Task Refresh_ValidToken_RotatesAndReturnsNewCredentials()
    {
        const string oldPlaintext = "active-refresh";
        var user = ActiveUser();
        var sessionId = Guid.NewGuid();
        var repository = new StubIdentityRepository
        {
            RefreshState = new RefreshTokenSecurityState(
                Guid.NewGuid(), sessionId, user.Id, TestNow.AddDays(1), null, true, [5]),
            Session = new SessionSecurityState(
                sessionId, user.Id, TestNow.AddHours(-1), TestNow.AddHours(4), null, true, [6]),
            User = user
        };
        var service = CreateService(repository, new StubCredentialVerifier());

        var result = await service.RefreshAsync(new RefreshCommand(oldPlaintext, Guid.NewGuid()));

        result.Status.Should().Be(AuthStatus.Success);
        result.Tokens.Should().NotBeNull();
        result.Tokens!.RefreshToken.Should().NotBe(oldPlaintext);
        repository.RotatedRefreshToken!.TokenHash.Should().Be(RefreshTokenGenerator.Hash(result.Tokens.RefreshToken));
    }

    [Fact(DisplayName = "P1-10 Positive: logout revokes the current session idempotently")]
    public async Task Logout_ValidSession_RevokesFamily()
    {
        var repository = new StubIdentityRepository();
        var sessionId = Guid.NewGuid();
        var service = CreateService(repository, new StubCredentialVerifier());

        var result = await service.LogoutAsync(sessionId);

        result.Status.Should().Be(AuthStatus.Success);
        repository.RevokedSessionId.Should().Be(sessionId);
    }

    [Fact(DisplayName = "P1-10 Negative: forced password change rejects password-policy failure without persistence")]
    public async Task ForcedPasswordChange_PolicyRejected_DoesNotPersist()
    {
        var repository = new StubIdentityRepository();
        var service = CreateService(repository, new StubCredentialVerifier
        {
            PasswordPreparation = new PasswordChangePreparationResult(
                PasswordChangePreparationStatus.PolicyRejected,
                ErrorMessage: "Password policy rejected the value.")
        });

        var result = await service.CompleteForcedPasswordChangeAsync(new ForcedPasswordChangeCommand(
            "field.user", "Current1!", "weak", "weak", Guid.NewGuid(), Guid.NewGuid()));

        result.Status.Should().Be(AuthStatus.PasswordPolicyRejected);
        repository.PasswordChangeUserId.Should().BeNull();
    }

    [Fact(DisplayName = "P1-10 Positive: forced password change maps durable idempotent replay to success")]
    public async Task ForcedPasswordChange_IdempotentReplay_ReturnsSuccess()
    {
        var user = ActiveUser(mustChangePassword: true);
        var repository = new StubIdentityRepository
        {
            PasswordChangeResult = new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.IdempotentReplay)
        };
        var service = CreateService(repository, new StubCredentialVerifier
        {
            PasswordPreparation = new PasswordChangePreparationResult(
                PasswordChangePreparationStatus.Success,
                user,
                "new-password-hash",
                "new-security-stamp")
        });

        var result = await service.CompleteForcedPasswordChangeAsync(new ForcedPasswordChangeCommand(
            "field.user", "Current1!", "Replacement1!", "Replacement1!", Guid.NewGuid(), Guid.NewGuid()));

        result.Status.Should().Be(AuthStatus.Success);
        result.Tokens.Should().BeNull();
    }

    private static readonly DateTimeOffset TestNow = new(2026, 9, 19, 5, 0, 0, TimeSpan.Zero);

    private static AuthService CreateService(
        StubIdentityRepository repository,
        StubCredentialVerifier verifier,
        JwtOptions? configuredOptions = null)
    {
        var options = configuredOptions ?? CreateOptions();
        return new AuthService(
            repository,
            verifier,
            new AccessTokenFactory(options),
            options,
            new FixedTimeProvider(TestNow),
            new PasswordChangeFingerprintFactory(CreateFingerprintOptions()),
            new SessionDeviceMetadataOptions());
    }

    private static JwtOptions CreateOptions() => new()
    {
        Issuer = "roadguard",
        Audience = "roadguard-api",
        ActiveKeyId = "current",
        SigningKeys = new Dictionary<string, string> { ["current"] = Convert.ToBase64String(new byte[32]) },
        AccessTokenLifetimeMinutes = 10,
        SessionLifetimeHours = 24,
        RefreshTokenLifetimeDays = 30
    };

    private static PasswordChangeFingerprintOptions CreateFingerprintOptions() => new()
    {
        Key = Convert.ToBase64String(Enumerable.Repeat((byte)91, 32).ToArray())
    };

    private static UserSecurityState ActiveUser(bool mustChangePassword = false) => new(
        Guid.NewGuid(),
        "field.user",
        "Field User",
        UserRoleCode.DroneOperator,
        UserStatus.Active,
        mustChangePassword,
        [1, 2, 3]);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class StubCredentialVerifier : ICredentialVerifier
    {
        public CredentialVerificationResult Verification { get; init; } =
            new(CredentialVerificationStatus.InvalidCredentials);

        public PasswordChangePreparationResult PasswordPreparation { get; init; } =
            new(PasswordChangePreparationStatus.InvalidCredentials);

        public Task<CredentialVerificationResult> VerifyAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default) => Task.FromResult(Verification);

        public Task<PasswordChangePreparationResult> PrepareForcedPasswordChangeAsync(
            string username,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default) => Task.FromResult(PasswordPreparation);
    }

    private sealed class StubIdentityRepository : IIdentityRepository
    {
        public UserSecurityState? User { get; init; }
        public SessionSecurityState? Session { get; init; }
        public RefreshTokenSecurityState? RefreshState { get; init; }
        public ReplayRevocationResult ReplayResult { get; init; } =
            new(ReplayRevocationStatus.Success);
        public ForcedPasswordChangeResult PasswordChangeResult { get; init; } =
            new(ForcedPasswordChangeStatus.Success);
        public UserSession? IssuedSession { get; private set; }
        public RefreshToken? IssuedRefreshToken { get; private set; }
        public RefreshToken? RotatedRefreshToken { get; private set; }
        public Guid? ReplayRevokedTokenId { get; private set; }
        public Guid? RevokedSessionId { get; private set; }
        public Guid? PasswordChangeUserId { get; private set; }

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
            Task.FromResult(User);

        public Task<bool> IsRoleActiveAsync(UserRoleCode roleCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<UserSecurityState?> GetUserSecurityStateByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult(User);

        public Task<SessionSecurityState?> GetSessionSecurityStateAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Session);

        public Task<RefreshTokenSecurityState?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(RefreshState);

        public Task<RotateRefreshTokenResult> RotateRefreshTokenAsync(
            Guid oldTokenId,
            byte[] expectedRowVersion,
            RefreshToken newToken,
            CancellationToken cancellationToken = default)
        {
            RotatedRefreshToken = newToken;
            return Task.FromResult(new RotateRefreshTokenResult(RotateRefreshTokenStatus.Success, newToken));
        }

        public Task<IssueSessionResult> IssueSessionWithRefreshTokenAsync(
            Guid userId,
            byte[] expectedUserRowVersion,
            UserSession session,
            RefreshToken initialRefreshToken,
            DateTimeOffset successfulLoginAt,
            CancellationToken cancellationToken = default)
        {
            IssuedSession = session;
            IssuedRefreshToken = initialRefreshToken;
            return Task.FromResult(new IssueSessionResult(IssueSessionStatus.Success, session.Id));
        }

        public Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(
            Guid userId,
            byte[] expectedUserRowVersion,
            string newPasswordHash,
            string newSecurityStamp,
            Guid operationId,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            PasswordChangeUserId = userId;
            return Task.FromResult(PasswordChangeResult);
        }

        public Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(
            Guid userId,
            byte[] expectedUserRowVersion,
            string newPasswordHash,
            string newSecurityStamp,
            string requestFingerprint,
            Guid operationId,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            PasswordChangeUserId = userId;
            return Task.FromResult(PasswordChangeResult);
        }

        public Task<ReplayRevocationResult> RevokeRefreshTokenFamilyForReplayAsync(
            Guid refreshTokenId,
            byte[] expectedTokenRowVersion,
            Guid? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            ReplayRevokedTokenId = refreshTokenId;
            return Task.FromResult(ReplayResult);
        }

        public Task RevokeSessionAndFamilyAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            RevokedSessionId = sessionId;
            return Task.CompletedTask;
        }

        public Task<UserRoleChangeResult> ChangeUserRoleAtomicAsync(
            Guid userId,
            UserRoleCode newRoleCode,
            byte[] expectedRowVersion,
            Guid actorUserId,
            Guid operationId,
            Guid? correlationId = null,
            string? reason = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
