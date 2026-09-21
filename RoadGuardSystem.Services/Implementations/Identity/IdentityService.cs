using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Generators;

namespace RoadGuardSystem.Services.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly IIdentityRepository _identityRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public IdentityService(
        IIdentityRepository identityRepository,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _identityRepository = identityRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ProfileView?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var profile = await _identityRepository.GetUserProfileAsync(userId, cancellationToken);
        return profile is null
            ? null
            : new ProfileView(
                profile.Id,
                profile.UserName,
                profile.DisplayName,
                profile.Email,
                profile.RoleCode,
                profile.Status,
                profile.RowVersion);
    }

    public async Task<ProfileUpdateResult> UpdateProfileAsync(
        Guid userId,
        UpdateProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || command.HasUnknownFields ||
            string.IsNullOrWhiteSpace(command.DisplayName) ||
            command.OperationId == Guid.Empty ||
            !TryDecodeRowVersion(command.ExpectedRowVersion, out var expectedRowVersion))
        {
            return new ProfileUpdateResult(ProfileUpdateStatus.InvalidInput);
        }

        var result = await _identityRepository.UpdateUserProfileAtomicAsync(
            userId,
            command.DisplayName,
            command.Email,
            expectedRowVersion,
            command.OperationId,
            command.CorrelationId,
            cancellationToken);

        return new ProfileUpdateResult(
            result.Status switch
            {
                UserProfileUpdateStatus.Success => ProfileUpdateStatus.Success,
                UserProfileUpdateStatus.IdempotentReplay => ProfileUpdateStatus.IdempotentReplay,
                UserProfileUpdateStatus.IdempotentConflict => ProfileUpdateStatus.IdempotentConflict,
                UserProfileUpdateStatus.UserNotFound => ProfileUpdateStatus.UserNotFound,
                UserProfileUpdateStatus.StaleConcurrency => ProfileUpdateStatus.StaleConcurrency,
                UserProfileUpdateStatus.EmailConflict => ProfileUpdateStatus.EmailConflict,
                _ => ProfileUpdateStatus.InvalidInput
            },
            result.Profile is null
                ? null
                : new ProfileView(
                    result.Profile.Id,
                    result.Profile.UserName,
                    result.Profile.DisplayName,
                    result.Profile.Email,
                    result.Profile.RoleCode,
                    result.Profile.Status,
                    result.Profile.RowVersion),
            result.Message);
    }

    public async Task<AdminPasswordResetServiceResult> ResetPasswordAsync(
        Guid actorUserId,
        Guid targetUserId,
        AdminPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || targetUserId == Guid.Empty || command.OperationId == Guid.Empty ||
            !TryDecodeRowVersion(command.ExpectedTargetRowVersion, out var expectedRowVersion))
        {
            return new AdminPasswordResetServiceResult(
                AdminPasswordResetServiceStatus.InvalidInput,
                targetUserId);
        }

        var actor = await _identityRepository.GetUserSecurityStateAsync(actorUserId, cancellationToken);
        if (actor is null || actor.Status != UserStatus.Active || actor.RoleCode != UserRoleCode.Supervisor)
        {
            return new AdminPasswordResetServiceResult(
                AdminPasswordResetServiceStatus.ActorNotAuthorized,
                targetUserId);
        }

        var target = await _identityRepository.GetUserSecurityStateAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return new AdminPasswordResetServiceResult(
                AdminPasswordResetServiceStatus.UserNotFound,
                targetUserId);
        }

        if (target.Status != UserStatus.Active)
        {
            return new AdminPasswordResetServiceResult(
                AdminPasswordResetServiceStatus.TargetNotActive,
                targetUserId);
        }

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        var passwordHash = _passwordHasher.HashPassword(new ApplicationUser(), temporaryPassword);
        var securityStamp = Guid.NewGuid().ToString();
        var requestFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"actor:{actorUserId:N};target:{targetUserId:N};rowVersion:{Convert.ToBase64String(expectedRowVersion)}")))
            .ToLowerInvariant();

        var result = await _identityRepository.ResetUserPasswordAtomicAsync(
            actorUserId,
            targetUserId,
            passwordHash,
            securityStamp,
            expectedRowVersion,
            requestFingerprint,
            command.OperationId,
            command.CorrelationId,
            cancellationToken);

        var status = result.Status switch
        {
            AdminPasswordResetStatus.Success => AdminPasswordResetServiceStatus.Success,
            AdminPasswordResetStatus.IdempotentReplay => AdminPasswordResetServiceStatus.IdempotentReplay,
            AdminPasswordResetStatus.IdempotentConflict => AdminPasswordResetServiceStatus.IdempotentConflict,
            AdminPasswordResetStatus.ActorNotAuthorized => AdminPasswordResetServiceStatus.ActorNotAuthorized,
            AdminPasswordResetStatus.UserNotFound => AdminPasswordResetServiceStatus.UserNotFound,
            AdminPasswordResetStatus.TargetNotActive => AdminPasswordResetServiceStatus.TargetNotActive,
            AdminPasswordResetStatus.StaleConcurrency => AdminPasswordResetServiceStatus.StaleConcurrency,
            _ => AdminPasswordResetServiceStatus.InvalidInput
        };

        return new AdminPasswordResetServiceResult(
            status,
            targetUserId,
            result.Target?.MustChangePassword ?? target.MustChangePassword,
            status == AdminPasswordResetServiceStatus.Success ? temporaryPassword : null,
            result.Message);
    }

    public async Task<RoleChangeServiceResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || targetUserId == Guid.Empty || command.OperationId == Guid.Empty ||
            command.NewRoleCode == UserRoleCode.Unknown ||
            !TryDecodeRowVersion(command.ExpectedRowVersion, out var expectedRowVersion))
        {
            return new RoleChangeServiceResult(RoleChangeServiceStatus.InvalidInput, command.NewRoleCode);
        }

        var actor = await _identityRepository.GetUserSecurityStateAsync(actorUserId, cancellationToken);
        if (actor is null || actor.Status != UserStatus.Active || actor.RoleCode != UserRoleCode.Supervisor)
        {
            return new RoleChangeServiceResult(RoleChangeServiceStatus.ActorNotAuthorized, command.NewRoleCode);
        }

        var target = await _identityRepository.GetUserSecurityStateAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return new RoleChangeServiceResult(RoleChangeServiceStatus.UserNotFound, command.NewRoleCode);
        }

        if (!await _identityRepository.IsRoleActiveAsync(command.NewRoleCode, cancellationToken))
        {
            return new RoleChangeServiceResult(RoleChangeServiceStatus.RoleNotFound, command.NewRoleCode);
        }

        var result = await _identityRepository.ChangeUserRoleAtomicAsync(
            targetUserId,
            command.NewRoleCode,
            expectedRowVersion,
            actorUserId,
            command.OperationId,
            command.CorrelationId,
            command.Reason,
            cancellationToken);
        return new RoleChangeServiceResult(
            result.Status switch
            {
                UserRoleChangeStatus.Success => RoleChangeServiceStatus.Success,
                UserRoleChangeStatus.IdempotentReplay => RoleChangeServiceStatus.IdempotentReplay,
                UserRoleChangeStatus.IdempotentConflict => RoleChangeServiceStatus.IdempotentConflict,
                UserRoleChangeStatus.UserNotFound => RoleChangeServiceStatus.UserNotFound,
                UserRoleChangeStatus.RoleNotFound => RoleChangeServiceStatus.RoleNotFound,
                UserRoleChangeStatus.StaleConcurrency => RoleChangeServiceStatus.StaleConcurrency,
                _ => RoleChangeServiceStatus.InvalidInput
            },
            result.RoleCode,
            result.Message);
    }

    private static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
