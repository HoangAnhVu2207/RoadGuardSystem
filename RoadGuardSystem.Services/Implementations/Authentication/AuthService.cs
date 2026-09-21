using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Generators;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityRepository _identityRepository;
    private readonly ICredentialVerifier _credentialVerifier;
    private readonly AccessTokenFactory _accessTokenFactory;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly PasswordChangeFingerprintFactory _passwordChangeFingerprintFactory;
    private readonly SessionDeviceMetadataOptions _sessionMetadataOptions;

    public AuthService(
        IIdentityRepository identityRepository,
        ICredentialVerifier credentialVerifier,
        AccessTokenFactory accessTokenFactory,
        JwtOptions options,
        TimeProvider timeProvider,
        PasswordChangeFingerprintFactory passwordChangeFingerprintFactory,
        SessionDeviceMetadataOptions sessionMetadataOptions)
    {
        ArgumentNullException.ThrowIfNull(identityRepository);
        ArgumentNullException.ThrowIfNull(credentialVerifier);
        ArgumentNullException.ThrowIfNull(accessTokenFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(passwordChangeFingerprintFactory);
        ArgumentNullException.ThrowIfNull(sessionMetadataOptions);

        _identityRepository = identityRepository;
        _credentialVerifier = credentialVerifier;
        _accessTokenFactory = accessTokenFactory;
        _options = options;
        _timeProvider = timeProvider;
        _passwordChangeFingerprintFactory = passwordChangeFingerprintFactory;
        _sessionMetadataOptions = sessionMetadataOptions;
    }

    public async Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null ||
            string.IsNullOrWhiteSpace(command.Username) ||
            string.IsNullOrWhiteSpace(command.Password))
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var metadataValidation = SessionDeviceMetadataValidator.Validate(
            command.DeviceMetadataJson,
            _sessionMetadataOptions);
        if (!metadataValidation.IsValid)
        {
            return new AuthResult(AuthStatus.InvalidInput, ErrorMessage: metadataValidation.ErrorMessage);
        }

        var verification = await _credentialVerifier.VerifyAsync(
            command.Username.Trim(),
            command.Password,
            cancellationToken);
        var user = verification.User;
        if (verification.Status != CredentialVerificationStatus.Success ||
            user is null ||
            user.Status != UserStatus.Active)
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        if (user.MustChangePassword)
        {
            return new AuthResult(AuthStatus.PasswordChangeRequired);
        }

        var now = _timeProvider.GetUtcNow();
        var material = RefreshTokenGenerator.Generate();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            DeviceMetadataJson = command.DeviceMetadataJson,
            ExpiresAt = now.AddHours(_options.SessionLifetimeHours)
        };
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = material.HashHex,
            ExpiresAt = now.AddDays(_options.RefreshTokenLifetimeDays)
        };

        var issue = await _identityRepository.IssueSessionWithRefreshTokenAsync(
            user.Id,
            user.RowVersion,
            session,
            refreshToken,
            now,
            cancellationToken);
        return issue.Status switch
        {
            IssueSessionStatus.Success => Success(user, session.Id, material, refreshToken.ExpiresAt, now),
            IssueSessionStatus.StaleConcurrency or IssueSessionStatus.DuplicateCredential =>
                new AuthResult(AuthStatus.Conflict),
            IssueSessionStatus.InvalidInput => new AuthResult(AuthStatus.InvalidInput),
            _ => new AuthResult(AuthStatus.InvalidCredentials)
        };
    }

    public async Task<AuthResult> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null || string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var now = _timeProvider.GetUtcNow();
        var tokenHash = RefreshTokenGenerator.Hash(command.RefreshToken);
        var token = await _identityRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        if (token.RevokedAt is not null)
        {
            await _identityRepository.RevokeRefreshTokenFamilyForReplayAsync(
                token.Id,
                token.RowVersion,
                command.CorrelationId,
                cancellationToken);
            return new AuthResult(AuthStatus.SessionRevoked);
        }

        if (token.ExpiresAt <= now)
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        var session = await _identityRepository.GetSessionSecurityStateAsync(token.SessionId, cancellationToken);
        if (session is null || session.UserId != token.UserId || session.RevokedAt is not null || session.ExpiresAt <= now)
        {
            return new AuthResult(AuthStatus.SessionRevoked);
        }

        var user = await _identityRepository.GetUserSecurityStateAsync(token.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword)
        {
            return new AuthResult(AuthStatus.Unauthorized);
        }

        var material = RefreshTokenGenerator.Generate();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = token.SessionId,
            TokenHash = material.HashHex,
            ExpiresAt = now.AddDays(_options.RefreshTokenLifetimeDays)
        };
        var rotation = await _identityRepository.RotateRefreshTokenAsync(
            token.Id,
            token.RowVersion,
            replacement,
            cancellationToken);
        if (rotation.Status == RotateRefreshTokenStatus.Success)
        {
            return Success(user, token.SessionId, material, replacement.ExpiresAt, now);
        }

        if (rotation.Status is RotateRefreshTokenStatus.AlreadyRevoked or RotateRefreshTokenStatus.StaleConcurrency)
        {
            var current = await _identityRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
            if (current is not null)
            {
                await _identityRepository.RevokeRefreshTokenFamilyForReplayAsync(
                    current.Id,
                    current.RowVersion,
                    command.CorrelationId,
                    cancellationToken);
            }

            return new AuthResult(AuthStatus.SessionRevoked);
        }

        return rotation.Status == RotateRefreshTokenStatus.InvalidToken
            ? new AuthResult(AuthStatus.InvalidInput)
            : new AuthResult(AuthStatus.InvalidCredentials);
    }

    public async Task<AuthResult> LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        await _identityRepository.RevokeSessionAndFamilyAsync(sessionId, cancellationToken);
        return new AuthResult(AuthStatus.Success);
    }

    public async Task<AuthResult> CompleteForcedPasswordChangeAsync(
        ForcedPasswordChangeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null ||
            string.IsNullOrWhiteSpace(command.Username) ||
            string.IsNullOrWhiteSpace(command.CurrentPassword) ||
            string.IsNullOrWhiteSpace(command.NewPassword) ||
            command.NewPassword != command.ConfirmPassword ||
            command.OperationId == Guid.Empty)
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var preparation = await _credentialVerifier.PrepareForcedPasswordChangeAsync(
            command.Username.Trim(),
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);
        if (preparation.Status == PasswordChangePreparationStatus.PolicyRejected ||
            preparation.Status == PasswordChangePreparationStatus.ReusedPassword)
        {
            return new AuthResult(AuthStatus.PasswordPolicyRejected, ErrorMessage: preparation.ErrorMessage);
        }

        var user = preparation.User;
        if (preparation.Status != PasswordChangePreparationStatus.Success ||
            user is null ||
            string.IsNullOrWhiteSpace(preparation.NewPasswordHash) ||
            string.IsNullOrWhiteSpace(preparation.NewSecurityStamp))
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        var result = await _identityRepository.CompleteForcedPasswordChangeAtomicAsync(
            user.Id,
            user.RowVersion,
            preparation.NewPasswordHash,
            preparation.NewSecurityStamp,
            _passwordChangeFingerprintFactory.Create(user.Id, command.NewPassword),
            command.OperationId,
            command.CorrelationId,
            cancellationToken);
        return result.Status switch
        {
            ForcedPasswordChangeStatus.Success or ForcedPasswordChangeStatus.IdempotentReplay =>
                new AuthResult(AuthStatus.Success),
            ForcedPasswordChangeStatus.IdempotentConflict or ForcedPasswordChangeStatus.StaleConcurrency =>
                new AuthResult(AuthStatus.Conflict),
            ForcedPasswordChangeStatus.NotRequired => new AuthResult(AuthStatus.NotRequired),
            ForcedPasswordChangeStatus.InvalidInput => new AuthResult(AuthStatus.InvalidInput),
            _ => new AuthResult(AuthStatus.InvalidCredentials)
        };
    }

    private AuthResult Success(
        UserSecurityState user,
        Guid sessionId,
        RefreshTokenMaterial material,
        DateTimeOffset refreshTokenExpiresAt,
        DateTimeOffset issuedAt)
    {
        var accessToken = _accessTokenFactory.Create(user.Id, sessionId, user.RoleCode, issuedAt);
        return new AuthResult(
            AuthStatus.Success,
            new AuthTokens(
                accessToken,
                material.Plaintext,
                issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes),
                refreshTokenExpiresAt));
    }
}
