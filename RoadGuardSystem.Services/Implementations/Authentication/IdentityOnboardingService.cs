using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Authentication;

public sealed class IdentityOnboardingService : IIdentityOnboardingService
{
    private readonly IIdentityOnboardingRepository _onboardingRepository;
    private readonly IIdentityRepository _identityRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IIdentityMessageSender _messageSender;
    private readonly AccessTokenFactory _accessTokenFactory;
    private readonly JwtOptions _jwtOptions;
    private readonly IdentityOnboardingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly byte[] _secret;

    public IdentityOnboardingService(
        IIdentityOnboardingRepository onboardingRepository,
        IIdentityRepository identityRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IIdentityMessageSender messageSender,
        AccessTokenFactory accessTokenFactory,
        IOptions<JwtOptions> jwtOptions,
        IOptions<IdentityOnboardingOptions> options,
        TimeProvider timeProvider)
    {
        _onboardingRepository = onboardingRepository;
        _identityRepository = identityRepository;
        _passwordHasher = passwordHasher;
        _messageSender = messageSender;
        _accessTokenFactory = accessTokenFactory;
        _jwtOptions = jwtOptions.Value;
        _options = options.Value;
        _timeProvider = timeProvider;
        _secret = DecodeSecret(_options.Secret);
    }

    public async Task<IdentityOnboardingResult> RegisterReporterAsync(
        string email,
        string password,
        string displayName,
        string reporterType,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(idempotencyKey) ||
            !TryParseReporterType(reporterType, out var parsedReporterType))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.InvalidInput);
        }

        var operationId = OperationId("RegisterReporter", idempotencyKey);
        var userId = Guid.NewGuid();
        var intentId = Guid.NewGuid();
        var otp = NumericSecret("reporter-otp", operationId);
        var now = _timeProvider.GetUtcNow();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = email.Trim(),
            NormalizedUserName = normalizedEmail,
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = false,
            DisplayName = displayName.Trim(),
            RoleCode = UserRoleCode.Reporter,
            Status = UserStatus.Pending,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = now
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        var intent = new ReporterRegistrationIntent
        {
            Id = intentId,
            UserId = userId,
            NormalizedEmail = normalizedEmail,
            ReporterType = parsedReporterType,
            OtpHash = HashSecret(otp),
            OtpGeneration = 1,
            ExpiresAt = now.AddMinutes(_options.OtpLifetimeMinutes),
            ResendAvailableAt = now.AddSeconds(_options.OtpResendCooldownSeconds),
            CreatedAt = now
        };
        var fingerprint = HashSecret($"{normalizedEmail}|{displayName.Trim()}|{parsedReporterType}|{password}");
        var persisted = await _onboardingRepository.RegisterReporterAsync(
            user,
            intent,
            idempotencyKey.Trim(),
            fingerprint,
            operationId,
            cancellationToken);
        if (persisted.Status is IdentityOnboardingPersistenceStatus.Success or IdentityOnboardingPersistenceStatus.IdempotentReplay &&
            persisted.Intent is not null)
        {
            if (!await _messageSender.SendReporterOtpAsync(email.Trim(), otp, cancellationToken))
            {
                return new IdentityOnboardingResult(IdentityOnboardingStatus.DeliveryUnavailable, ToView(persisted.Intent));
            }

            return new IdentityOnboardingResult(Map(persisted.Status), ToView(persisted.Intent));
        }

        return new IdentityOnboardingResult(Map(persisted.Status));
    }

    public async Task<IdentityOnboardingResult> VerifyReporterOtpAsync(
        Guid intentId,
        string otp,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (intentId == Guid.Empty || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.InvalidInput);
        }

        var intent = await _onboardingRepository.GetReporterRegistrationIntentAsync(intentId, cancellationToken);
        if (intent?.UserId is null)
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.NotFound);
        }

        var operationId = OperationId("VerifyReporterOtp", idempotencyKey);
        var credentials = CreateCredentials(intent.UserId.Value, operationId, "reporter-verify");
        var fingerprint = HashSecret($"{intentId:N}|{otp}");
        var persisted = await _onboardingRepository.VerifyReporterOtpAsync(
            intentId,
            HashSecret(otp),
            _options.OtpMaxAttempts,
            credentials.Session,
            credentials.RefreshToken,
            idempotencyKey.Trim(),
            fingerprint,
            operationId,
            credentials.IssuedAt,
            cancellationToken);
        return persisted.User is not null && persisted.SessionId is not null &&
               persisted.Status is IdentityOnboardingPersistenceStatus.Success or IdentityOnboardingPersistenceStatus.IdempotentReplay
            ? new IdentityOnboardingResult(
                Map(persisted.Status),
                ToView(persisted.Intent),
                Tokens: CreateTokens(persisted.User, persisted.SessionId.Value, operationId, "reporter-verify"))
            : new IdentityOnboardingResult(Map(persisted.Status), ToView(persisted.Intent));
    }

    public async Task<IdentityOnboardingResult> ResendReporterOtpAsync(
        Guid intentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (intentId == Guid.Empty || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.InvalidInput);
        }

        var operationId = OperationId("ResendReporterOtp", idempotencyKey);
        var otp = NumericSecret("reporter-resend", operationId);
        var now = _timeProvider.GetUtcNow();
        var persisted = await _onboardingRepository.ResendReporterOtpAsync(
            intentId,
            HashSecret(otp),
            now.AddMinutes(_options.OtpLifetimeMinutes),
            now.AddSeconds(_options.OtpResendCooldownSeconds),
            _options.OtpMaxResendsPerWindow,
            TimeSpan.FromMinutes(_options.OtpResendWindowMinutes),
            idempotencyKey.Trim(),
            HashSecret(intentId.ToString("N")),
            operationId,
            now,
            cancellationToken);
        if (persisted.Status is IdentityOnboardingPersistenceStatus.Success or IdentityOnboardingPersistenceStatus.IdempotentReplay &&
            persisted.Intent is not null &&
            !await _messageSender.SendReporterOtpAsync(persisted.Intent.NormalizedEmail, otp, cancellationToken))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.DeliveryUnavailable, ToView(persisted.Intent));
        }

        return new IdentityOnboardingResult(Map(persisted.Status), ToView(persisted.Intent));
    }

    public async Task<IdentityOnboardingResult> CreateInvitationAsync(
        Guid actorUserId,
        string email,
        string role,
        IReadOnlyList<Guid> projectIds,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var actor = await _identityRepository.GetUserSecurityStateAsync(actorUserId, cancellationToken);
        if (actor is not { Status: UserStatus.Active, RoleCode: UserRoleCode.Supervisor })
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.Forbidden);
        }

        if (!TryNormalizeEmail(email, out var normalizedEmail) ||
            !TryParseStaffRole(role, out var roleCode) ||
            projectIds is null || projectIds.Distinct().Count() != projectIds.Count ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.InvalidInput);
        }

        var operationId = OperationId("CreateInvitation", $"{actorUserId:N}:{idempotencyKey}");
        var invitationId = Guid.NewGuid();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var now = _timeProvider.GetUtcNow();
        var invitation = new StaffInvitation
        {
            Id = invitationId,
            DisplayName = email.Trim(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            RoleCode = roleCode,
            TokenHash = HashSecret(token),
            CreatedByUserId = actorUserId,
            CreatedAt = now,
            ExpiresAt = now.AddHours(_options.InvitationLifetimeHours)
        };
        var fingerprint = HashSecret($"{normalizedEmail}|{roleCode}|{string.Join(',', projectIds.Order())}");
        var persisted = await _onboardingRepository.CreateInvitationAsync(
            invitation,
            projectIds,
            idempotencyKey.Trim(),
            fingerprint,
            operationId,
            cancellationToken);
        if (persisted.Status is IdentityOnboardingPersistenceStatus.Success &&
             persisted.Invitation is not null &&
             !await _messageSender.SendInvitationAsync(
                email.Trim(),
                persisted.Invitation.DisplayName,
                DisplayRoleName(persisted.Invitation.RoleCode),
                persisted.DeliveryToken ?? token,
                persisted.Invitation.ExpiresAt,
                cancellationToken))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.DeliveryUnavailable, Invitation: ToView(persisted.Invitation));
        }

        return new IdentityOnboardingResult(Map(persisted.Status), Invitation: ToView(persisted.Invitation));
    }

    public async Task<IdentityOnboardingResult> AcceptInvitationAsync(
        string invitationToken,
        string displayName,
        string password,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invitationToken) || string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.InvalidInput);
        }

        var tokenHash = HashSecret(invitationToken);
        var invitation = await _onboardingRepository.GetInvitationByTokenHashAsync(tokenHash, cancellationToken);
        if (invitation is null)
        {
            return new IdentityOnboardingResult(IdentityOnboardingStatus.NotFound);
        }

        var operationId = OperationId("AcceptInvitation", idempotencyKey);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName.Trim(),
            Status = UserStatus.Active,
            EmailConfirmed = true,
            MustChangePassword = false,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = _timeProvider.GetUtcNow()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        var credentials = CreateCredentials(user.Id, operationId, "invitation-accept");
        var persisted = await _onboardingRepository.AcceptInvitationAsync(
            tokenHash,
            user,
            credentials.Session,
            credentials.RefreshToken,
            idempotencyKey.Trim(),
            HashSecret($"{tokenHash}|{displayName.Trim()}|{password}"),
            operationId,
            credentials.IssuedAt,
            cancellationToken);
        return persisted.User is not null && persisted.SessionId is not null &&
               persisted.Status is IdentityOnboardingPersistenceStatus.Success or IdentityOnboardingPersistenceStatus.IdempotentReplay
            ? new IdentityOnboardingResult(
                Map(persisted.Status),
                Invitation: ToView(persisted.Invitation),
                Tokens: CreateTokens(persisted.User, persisted.SessionId.Value, operationId, "invitation-accept"))
            : new IdentityOnboardingResult(Map(persisted.Status), Invitation: ToView(persisted.Invitation));
    }

    private (UserSession Session, RefreshToken RefreshToken, DateTimeOffset IssuedAt) CreateCredentials(
        Guid userId,
        Guid operationId,
        string purpose)
    {
        var issuedAt = _timeProvider.GetUtcNow();
        var sessionId = OperationId($"{purpose}:session", operationId.ToString("N"));
        var refreshPlaintext = TokenSecret($"{purpose}:refresh", operationId);
        return (
            new UserSession
            {
                Id = sessionId,
                UserId = userId,
                IssuedAt = issuedAt,
                ExpiresAt = issuedAt.AddHours(_jwtOptions.SessionLifetimeHours)
            },
            new RefreshToken
            {
                Id = OperationId($"{purpose}:refresh-id", operationId.ToString("N")),
                SessionId = sessionId,
                TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshPlaintext))).ToLowerInvariant(),
                ExpiresAt = issuedAt.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
            },
            issuedAt);
    }

    private AuthTokens CreateTokens(UserSecurityState user, Guid sessionId, Guid operationId, string purpose)
    {
        var now = _timeProvider.GetUtcNow();
        return new AuthTokens(
            _accessTokenFactory.Create(user.Id, sessionId, user.RoleCode, now),
            TokenSecret($"{purpose}:refresh", operationId),
            now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes),
            now.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            checked(_jwtOptions.AccessTokenLifetimeMinutes * 60),
            user);
    }

    private string NumericSecret(string purpose, Guid id)
    {
        var hash = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes($"{purpose}:{id:N}"));
        var value = BitConverter.ToUInt32(hash, 0) % 1_000_000;
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }

    private string TokenSecret(string purpose, Guid id) =>
        Convert.ToBase64String(HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes($"{purpose}:{id:N}")))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private string HashSecret(string value) =>
        Convert.ToHexString(HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private Guid OperationId(string operation, string key)
    {
        var hash = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes($"{operation}:{key.Trim()}"));
        return new Guid(hash[..16]);
    }

    private static byte[] DecodeSecret(string value)
    {
        try
        {
            var decoded = Convert.FromBase64String(value);
            if (decoded.Length >= 32)
            {
                return decoded;
            }
        }
        catch (FormatException)
        {
        }

        throw new OptionsValidationException(
            IdentityOnboardingOptions.SectionName,
            typeof(IdentityOnboardingOptions),
            ["IdentityOnboarding:Secret must be valid Base64 containing at least 32 bytes."]);
    }

    private static bool TryNormalizeEmail(string value, out string normalized)
    {
        normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length is > 3 and <= 254 && normalized.Count(character => character == '@') == 1;
    }

    private static bool TryParseReporterType(string value, out ReporterType reporterType)
    {
        reporterType = value?.Trim().ToUpperInvariant() switch
        {
            "CITIZEN" => ReporterType.Citizen,
            "INVESTOR_REPRESENTATIVE" => ReporterType.InvestorRepresentative,
            _ => ReporterType.Unknown
        };
        return reporterType != ReporterType.Unknown;
    }

    private static bool TryParseStaffRole(string value, out UserRoleCode role)
    {
        role = value?.Trim().ToUpperInvariant() switch
        {
            "SUPERVISOR" => UserRoleCode.Supervisor,
            "PM" => UserRoleCode.ProjectManager,
            "OPERATOR" => UserRoleCode.DroneOperator,
            "CREW" => UserRoleCode.RepairCrew,
            _ => UserRoleCode.Unknown
        };
        return role != UserRoleCode.Unknown;
    }

    private static string DisplayRoleName(UserRoleCode role) => role switch
    {
        UserRoleCode.Supervisor => "Supervisor",
        UserRoleCode.ProjectManager => "Project Manager",
        UserRoleCode.DroneOperator => "Drone Operator",
        UserRoleCode.RepairCrew => "Repair Crew",
        _ => role.ToString()
    };

    private static IdentityOnboardingStatus Map(IdentityOnboardingPersistenceStatus status) => status switch
    {
        IdentityOnboardingPersistenceStatus.Success => IdentityOnboardingStatus.Success,
        IdentityOnboardingPersistenceStatus.IdempotentReplay => IdentityOnboardingStatus.IdempotentReplay,
        IdentityOnboardingPersistenceStatus.IdempotentConflict => IdentityOnboardingStatus.IdempotencyConflict,
        IdentityOnboardingPersistenceStatus.Forbidden => IdentityOnboardingStatus.Forbidden,
        IdentityOnboardingPersistenceStatus.NotFound => IdentityOnboardingStatus.NotFound,
        IdentityOnboardingPersistenceStatus.Conflict => IdentityOnboardingStatus.Conflict,
        IdentityOnboardingPersistenceStatus.TooManyRequests => IdentityOnboardingStatus.TooManyRequests,
        _ => IdentityOnboardingStatus.InvalidInput
    };

    private RegistrationIntentView? ToView(ReporterRegistrationIntent? intent) => intent is null
        ? null
        : new RegistrationIntentView(
            intent.Id,
            Math.Max(0, (int)Math.Ceiling((intent.ResendAvailableAt - _timeProvider.GetUtcNow()).TotalSeconds)));

    private static InvitationView? ToView(StaffInvitation? invitation) => invitation is null
        ? null
        : new InvitationView(
            invitation.Id,
            invitation.AcceptedAt is null ? "PENDING" : "ACCEPTED",
            invitation.ExpiresAt,
            invitation.RowVersion);
}
