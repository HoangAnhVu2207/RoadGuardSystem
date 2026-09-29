using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.Services.Identity;

public sealed class IdentityV2Service : IIdentityV2Service
{
    private readonly IIdentityRepository _repository;
    private readonly IIdentityV2Repository _v2Repository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public IdentityV2Service(
        IIdentityRepository repository,
        IIdentityV2Repository v2Repository,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _repository = repository;
        _v2Repository = v2Repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IdentityV2Result> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _repository.GetUserProfileAsync(userId, cancellationToken);
        return profile is null
            ? new IdentityV2Result(IdentityV2Status.NotFound)
            : Success(profile);
    }

    public async Task<IdentityV2Result> UpdateMeAsync(
        Guid userId,
        string displayName,
        string? ifMatch,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityV2Result(IdentityV2Status.InvalidInput);
        }

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new IdentityV2Result(IdentityV2Status.PreconditionRequired);
        }

        if (!TryDecodeStrongEtag(ifMatch, out var expectedVersion))
        {
            return new IdentityV2Result(IdentityV2Status.InvalidInput);
        }

        var current = await _repository.GetUserProfileAsync(userId, cancellationToken);
        if (current is null)
        {
            return new IdentityV2Result(IdentityV2Status.NotFound);
        }

        var result = await _repository.UpdateUserProfileAtomicAsync(
            userId,
            displayName,
            current.Email,
            expectedVersion,
            OperationId("UpdateMe", $"{userId:N}:{idempotencyKey}"),
            correlationId,
            cancellationToken);

        return result.Status switch
        {
            UserProfileUpdateStatus.Success => Success(result.Profile!, IdentityV2Status.Success),
            UserProfileUpdateStatus.IdempotentReplay => Success(result.Profile!, IdentityV2Status.IdempotentReplay),
            UserProfileUpdateStatus.IdempotentConflict => new IdentityV2Result(IdentityV2Status.IdempotencyConflict),
            UserProfileUpdateStatus.StaleConcurrency => new IdentityV2Result(IdentityV2Status.PreconditionFailed),
            UserProfileUpdateStatus.UserNotFound => new IdentityV2Result(IdentityV2Status.NotFound),
            _ => new IdentityV2Result(IdentityV2Status.InvalidInput)
        };
    }

    public async Task<IdentityV2Result> GetAccountAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await IsSupervisorAsync(actorUserId, cancellationToken))
        {
            return new IdentityV2Result(IdentityV2Status.Forbidden);
        }

        var profile = await _repository.GetUserProfileAsync(targetUserId, cancellationToken);
        return profile is null
            ? new IdentityV2Result(IdentityV2Status.NotFound)
            : Success(profile);
    }

    public async Task<IdentityV2Result> ResetPasswordAsync(
        Guid actorUserId,
        Guid targetUserId,
        string temporaryPassword,
        string reason,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!await IsSupervisorAsync(actorUserId, cancellationToken))
        {
            return new IdentityV2Result(IdentityV2Status.Forbidden);
        }

        if (targetUserId == Guid.Empty || string.IsNullOrWhiteSpace(temporaryPassword) ||
            string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityV2Result(IdentityV2Status.InvalidInput);
        }

        var target = await _repository.GetUserSecurityStateAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return new IdentityV2Result(IdentityV2Status.NotFound);
        }

        if (target.Status != UserStatus.Active)
        {
            return new IdentityV2Result(IdentityV2Status.Conflict);
        }

        var passwordHash = _passwordHasher.HashPassword(new ApplicationUser(), temporaryPassword);
        var fingerprint = Hash($"actor:{actorUserId:N};target:{targetUserId:N};password:{temporaryPassword};reason:{reason.Trim()}");
        var result = await _repository.ResetUserPasswordAtomicAsync(
            actorUserId,
            targetUserId,
            passwordHash,
            Guid.NewGuid().ToString("N"),
            target.RowVersion,
            fingerprint,
            OperationId("AdminResetPasswordV2", $"{actorUserId:N}:{targetUserId:N}:{idempotencyKey}"),
            correlationId,
            cancellationToken);

        return result.Status switch
        {
            AdminPasswordResetStatus.Success => new IdentityV2Result(IdentityV2Status.Success),
            AdminPasswordResetStatus.IdempotentReplay => new IdentityV2Result(IdentityV2Status.IdempotentReplay),
            AdminPasswordResetStatus.IdempotentConflict => new IdentityV2Result(IdentityV2Status.IdempotencyConflict),
            AdminPasswordResetStatus.StaleConcurrency => new IdentityV2Result(IdentityV2Status.Conflict),
            AdminPasswordResetStatus.UserNotFound => new IdentityV2Result(IdentityV2Status.NotFound),
            AdminPasswordResetStatus.TargetNotActive => new IdentityV2Result(IdentityV2Status.Conflict),
            _ => new IdentityV2Result(IdentityV2Status.InvalidInput)
        };
    }

    public async Task<IdentityV2Result> UpdateAccountAsync(
        Guid actorUserId,
        Guid targetUserId,
        string status,
        string role,
        string reason,
        string? ifMatch,
        string idempotencyKey,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!await IsSupervisorAsync(actorUserId, cancellationToken))
        {
            return new IdentityV2Result(IdentityV2Status.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return new IdentityV2Result(IdentityV2Status.PreconditionRequired);
        }

        if (!TryDecodeStrongEtag(ifMatch, out var expectedVersion) ||
            !TryParseStatus(status, out var parsedStatus) ||
            !TryParseRole(role, out var parsedRole) ||
            string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdentityV2Result(IdentityV2Status.InvalidInput);
        }

        var result = await _v2Repository.UpdateAccountV2AtomicAsync(
            actorUserId,
            targetUserId,
            parsedStatus,
            parsedRole,
            reason.Trim(),
            expectedVersion,
            idempotencyKey.Trim(),
            OperationId("UpdateAccountV2", $"{actorUserId:N}:{targetUserId:N}:{idempotencyKey}"),
            correlationId,
            cancellationToken);
        return result.Status switch
        {
            V2AccountUpdatePersistenceStatus.Success => Success(result.Profile!, IdentityV2Status.Success),
            V2AccountUpdatePersistenceStatus.IdempotentReplay => Success(result.Profile!, IdentityV2Status.IdempotentReplay),
            V2AccountUpdatePersistenceStatus.IdempotentConflict => new IdentityV2Result(IdentityV2Status.IdempotencyConflict),
            V2AccountUpdatePersistenceStatus.NotFound => new IdentityV2Result(IdentityV2Status.NotFound),
            V2AccountUpdatePersistenceStatus.StaleConcurrency => new IdentityV2Result(IdentityV2Status.PreconditionFailed),
            _ => new IdentityV2Result(IdentityV2Status.InvalidInput)
        };
    }

    private async Task<bool> IsSupervisorAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actor = await _repository.GetUserSecurityStateAsync(actorUserId, cancellationToken);
        return actor is { Status: UserStatus.Active, RoleCode: UserRoleCode.Supervisor };
    }

    private static IdentityV2Result Success(
        UserProfileState profile,
        IdentityV2Status status = IdentityV2Status.Success) =>
        new(status, new IdentityV2Actor(
            profile.Id,
            profile.DisplayName,
            profile.RoleCode,
            profile.RowVersion));

    private static bool TryDecodeStrongEtag(string value, out byte[] version)
    {
        version = [];
        var trimmed = value.Trim();
        if (trimmed.Length < 3 || trimmed[0] != '"' || trimmed[^1] != '"' ||
            trimmed.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            version = Convert.FromBase64String(trimmed[1..^1]);
            return version.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static Guid OperationId(string operation, string idempotencyKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{operation}:{idempotencyKey.Trim()}"));
        return new Guid(hash[..16]);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool TryParseStatus(string value, out UserStatus status)
    {
        status = value.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => UserStatus.Active,
            "SUSPENDED" => UserStatus.Suspended,
            _ => UserStatus.Unknown
        };
        return status != UserStatus.Unknown;
    }

    private static bool TryParseRole(string value, out UserRoleCode role)
    {
        role = value.Trim().ToUpperInvariant() switch
        {
            "SUPERVISOR" => UserRoleCode.Supervisor,
            "PM" => UserRoleCode.ProjectManager,
            "OPERATOR" => UserRoleCode.DroneOperator,
            "CREW" => UserRoleCode.RepairCrew,
            "REPORTER" => UserRoleCode.Reporter,
            _ => UserRoleCode.Unknown
        };
        return role != UserRoleCode.Unknown;
    }
}
