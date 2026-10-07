using System.Security.Cryptography;
using System.Text;
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
            string.IsNullOrWhiteSpace(command.Email) ||
            string.IsNullOrWhiteSpace(command.Password))
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var metadataValidation = SessionDeviceMetadataValidator.Validate(
            null,
            _sessionMetadataOptions);
        if (!metadataValidation.IsValid)
        {
            return new AuthResult(AuthStatus.InvalidInput, ErrorMessage: metadataValidation.ErrorMessage);
        }

        var verification = await _credentialVerifier.VerifyAsync(
            command.Email.Trim(),
            command.Password,
            cancellationToken);
        var user = verification.User;
        if (verification.Status != CredentialVerificationStatus.Success ||
            user is null ||
            user.Status != UserStatus.Active ||
            !await _identityRepository.IsRoleActiveAsync(user.RoleCode, cancellationToken))
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        var now = _timeProvider.GetUtcNow();
        var material = RefreshTokenGenerator.Generate();
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuedAt = now,
            DeviceMetadataJson = null,
            ExpiresAt = null,
            Lifecycle = SessionLifecycle.PersistentRenewable,
            IssuedRole = user.RoleCode,
            Transport = command.Transport,
            LastActivityAt = command.Transport == SessionTransport.Web ? now : null
        };
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            TokenHash = material.HashHex,
            ExpiresAt = null
        };

        var issue = await _identityRepository.IssueSessionWithRefreshTokenAsync(
            user.Id,
            user.RowVersion,
            session,
            refreshToken,
            now,
            cancellationToken);
        if (issue.Status == IssueSessionStatus.Success)
        {
            var authoritativeUser = await _identityRepository.GetUserSecurityStateAsync(user.Id, cancellationToken);
            return authoritativeUser is null
                ? new AuthResult(AuthStatus.Conflict)
                : Success(authoritativeUser, session.Id, material, refreshToken.ExpiresAt, now,
                    command.Transport, session.ExpiresAt);
        }

        return issue.Status switch
        {
            IssueSessionStatus.StaleConcurrency or IssueSessionStatus.DuplicateCredential =>
                new AuthResult(AuthStatus.Conflict),
            IssueSessionStatus.InvalidInput => new AuthResult(AuthStatus.InvalidInput),
            _ => new AuthResult(AuthStatus.InvalidCredentials)
        };
    }

    public async Task<AuthResult> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null || string.IsNullOrWhiteSpace(command.RefreshToken) ||
            command.OperationKey is not null && (command.OperationKey.Length is < 1 or > 150 ||
                command.OperationKey.Any(c => c is < '!' or > '~')))
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var now = _timeProvider.GetUtcNow();
        var tokenHash = RefreshTokenGenerator.Hash(command.RefreshToken);
        var token = await _identityRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (token is null)
        {
            return new AuthResult(AuthStatus.RefreshTokenInvalid);
        }

        if (token.RevokedAt is not null && string.IsNullOrWhiteSpace(command.OperationKey))
        {
            await _identityRepository.RevokeRefreshTokenFamilyForReplayAsync(
                token.Id,
                token.RowVersion,
                command.CorrelationId,
                cancellationToken);
            return new AuthResult(AuthStatus.SessionRevoked);
        }

        if (token.ExpiresAt is { } expiry && expiry <= now)
        {
            return new AuthResult(AuthStatus.RefreshTokenExpired);
        }

        var session = await _identityRepository.GetSessionSecurityStateAsync(token.SessionId, cancellationToken);
        if (session is null || session.UserId != token.UserId || session.RevokedAt is not null || !SessionIsActive(session, now) ||
            command.RequiredTransport is { } required && session.Transport != required)
        {
            return new AuthResult(AuthStatus.SessionRevoked);
        }

        var user = await _identityRepository.GetUserSecurityStateAsync(token.UserId, cancellationToken);
        if (user is null || session.Lifecycle == SessionLifecycle.LegacyBounded && token.ExpiresAt is null || user.Status != UserStatus.Active || user.MustChangePassword ||
            session.IssuedRole is { } issuedRole && user.RoleCode != issuedRole ||
            !await _identityRepository.IsRoleActiveAsync(user.RoleCode, cancellationToken))
        {
            return new AuthResult(AuthStatus.Unauthorized);
        }

        var material = RefreshTokenGenerator.Generate();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = token.SessionId,
            TokenHash = material.HashHex,
            ExpiresAt = session.Lifecycle == SessionLifecycle.PersistentRenewable ? null : Min(now.AddDays(session.Transport == SessionTransport.Android ? 30 : _options.RefreshTokenLifetimeDays), session.ExpiresAt!.Value)
        };
        var protector = new RefreshCredentialProtection(_options);
        var rotation = string.IsNullOrWhiteSpace(command.OperationKey)
            ? await _identityRepository.RotateRefreshTokenAsync(token.Id, token.RowVersion, replacement, cancellationToken)
            : await _identityRepository.RotateRefreshTokenWithReceiptAsync(token.Id, token.RowVersion, replacement,
                command.OperationKey, tokenHash, protector.Protect(material.Plaintext), cancellationToken);
        if (rotation.Status == RotateRefreshTokenStatus.Success)
        {
            RefreshTokenMaterial returnedMaterial;
            try
            {
                returnedMaterial = rotation.ProtectedCredential is null ? material :
                new RefreshTokenMaterial(protector.Unprotect(rotation.ProtectedCredential), rotation.NewToken!.TokenHash);
            }
            catch (Exception error) when (error is CryptographicException or FormatException or KeyNotFoundException)
            { return new AuthResult(AuthStatus.RefreshTokenInvalid); }
            if (RefreshTokenGenerator.Hash(returnedMaterial.Plaintext) != rotation.NewToken?.TokenHash)
                return new AuthResult(AuthStatus.RefreshTokenInvalid);
            return Success(user, token.SessionId, returnedMaterial, rotation.NewToken?.ExpiresAt ?? replacement.ExpiresAt, now,
                session.Transport, session.ExpiresAt);
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

        if (rotation.Status == RotateRefreshTokenStatus.SessionRevoked)
        {
            return new AuthResult(AuthStatus.SessionRevoked);
        }

        return rotation.Status == RotateRefreshTokenStatus.InvalidToken
            ? new AuthResult(AuthStatus.RefreshTokenInvalid)
            : rotation.Status == RotateRefreshTokenStatus.Expired
                ? new AuthResult(AuthStatus.RefreshTokenExpired)
                : new AuthResult(AuthStatus.RefreshTokenInvalid);
    }

    private static bool SessionIsActive(SessionSecurityState session, DateTimeOffset now) =>
        new UserSession
        {
            IssuedAt = session.IssuedAt,
            ExpiresAt = session.ExpiresAt,
            RevokedAt = session.RevokedAt,
            Transport = session.Transport,
            LastActivityAt = session.LastActivityAt,
            Lifecycle = session.Lifecycle,
            IssuedRole = session.IssuedRole
        }.IsActiveAt(now);

    private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) =>
        first <= second ? first : second;

    public async Task<AuthResult> LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        await _identityRepository.RevokeSessionAndFamilyAsync(sessionId, cancellationToken);
        return new AuthResult(AuthStatus.Success);
    }

    public async Task<AuthResult> LogoutAsync(
        Guid userId,
        Guid sessionId,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var fingerprint = HashSha256($"user:{userId:N};session:{sessionId:N}");
        var result = await _identityRepository.RevokeSessionAndFamilyAtomicAsync(
            userId,
            sessionId,
            idempotencyKey.Trim(),
            fingerprint,
            correlationId,
            cancellationToken);
        return result.IdempotentConflict
            ? new AuthResult(AuthStatus.IdempotentConflict)
            : new AuthResult(AuthStatus.Success);
    }

    public async Task<PasswordRecoveryResult> RequestPasswordRecoveryAsync(
        PasswordRecoveryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var requestId = Guid.NewGuid();
        await _identityRepository.CreatePasswordRecoveryRequestAsync(
            requestId,
            command.Email.Trim().ToUpperInvariant(),
            _timeProvider.GetUtcNow(),
            command.CorrelationId,
            cancellationToken);
        return new PasswordRecoveryResult(requestId);
    }

    public async Task<AuthResult> ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null || command.UserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.CurrentPassword) ||
            string.IsNullOrWhiteSpace(command.NewPassword) ||
            string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return new AuthResult(AuthStatus.InvalidInput);
        }

        var requestFingerprint = _passwordChangeFingerprintFactory.Create(command.UserId, command.NewPassword);
        var durableFingerprint = await _identityRepository.GetIdempotencyFingerprintAsync(
            command.UserId,
            "ChangePassword",
            command.IdempotencyKey.Trim(),
            cancellationToken);
        if (durableFingerprint is not null)
        {
            return new AuthResult(durableFingerprint == requestFingerprint
                ? AuthStatus.Success
                : AuthStatus.IdempotentConflict);
        }

        var preparation = await _credentialVerifier.PreparePasswordChangeAsync(
            command.UserId,
            command.CurrentPassword,
            command.NewPassword,
            cancellationToken);
        if (preparation.Status is PasswordChangePreparationStatus.PolicyRejected or PasswordChangePreparationStatus.ReusedPassword)
        {
            return new AuthResult(AuthStatus.PasswordPolicyRejected, ErrorMessage: preparation.ErrorMessage);
        }

        if (preparation.Status != PasswordChangePreparationStatus.Success || preparation.User is null ||
            string.IsNullOrWhiteSpace(preparation.NewPasswordHash) ||
            string.IsNullOrWhiteSpace(preparation.NewSecurityStamp))
        {
            return new AuthResult(AuthStatus.InvalidCredentials);
        }

        var persisted = await _identityRepository.ChangePasswordAtomicAsync(
            command.UserId,
            preparation.User.RowVersion,
            preparation.NewPasswordHash,
            preparation.NewSecurityStamp,
            command.IdempotencyKey.Trim(),
            requestFingerprint,
            command.CorrelationId,
            cancellationToken);
        if (persisted.IdempotentConflict)
        {
            return new AuthResult(AuthStatus.IdempotentConflict);
        }

        if (persisted.StaleConcurrency)
        {
            return new AuthResult(AuthStatus.Conflict);
        }

        return persisted.Succeeded || persisted.IdempotentReplay
            ? new AuthResult(AuthStatus.Success)
            : new AuthResult(AuthStatus.InvalidCredentials);
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
        DateTimeOffset? refreshTokenExpiresAt,
        DateTimeOffset issuedAt,
        SessionTransport transport = SessionTransport.LegacyBearer,
        DateTimeOffset? sessionExpiresAt = default)
    {
        var lifetimeMinutes = transport == SessionTransport.Android ? 15 : _options.AccessTokenLifetimeMinutes;
        var accessToken = _accessTokenFactory.Create(user.Id, sessionId, user.RoleCode, issuedAt, lifetimeMinutes);
        return new AuthResult(
            AuthStatus.Success,
            new AuthTokens(
                accessToken,
                material.Plaintext,
                issuedAt.AddMinutes(lifetimeMinutes),
                refreshTokenExpiresAt,
                checked(lifetimeMinutes * 60),
                user,
                sessionId,
                sessionExpiresAt,
                issuedAt));
    }

    private static string HashSha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
