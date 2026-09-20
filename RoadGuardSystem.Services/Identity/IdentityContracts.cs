using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Identity;

public sealed record ProfileView(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Email,
    UserRoleCode RoleCode,
    UserStatus Status,
    byte[] RowVersion);

public sealed record UpdateProfileCommand(
    string? DisplayName,
    string? Email,
    string? ExpectedRowVersion,
    Guid OperationId,
    Guid? CorrelationId,
    bool HasUnknownFields);

public enum ProfileUpdateStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    UserNotFound,
    StaleConcurrency,
    EmailConflict,
    InvalidInput
}

public sealed record ProfileUpdateResult(
    ProfileUpdateStatus Status,
    ProfileView? Profile = null,
    string? Message = null);

public sealed record AdminPasswordResetCommand(
    string? ExpectedTargetRowVersion,
    Guid OperationId,
    Guid? CorrelationId);

public enum AdminPasswordResetServiceStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    ActorNotAuthorized,
    UserNotFound,
    TargetNotActive,
    StaleConcurrency,
    InvalidInput
}

public sealed record AdminPasswordResetServiceResult(
    AdminPasswordResetServiceStatus Status,
    Guid TargetUserId,
    bool MustChangePassword = true,
    string? TemporaryPassword = null,
    string? Message = null);

public interface IIdentityService
{
    Task<ProfileView?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ProfileUpdateResult> UpdateProfileAsync(
        Guid userId,
        UpdateProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<AdminPasswordResetServiceResult> ResetPasswordAsync(
        Guid actorUserId,
        Guid targetUserId,
        AdminPasswordResetCommand command,
        CancellationToken cancellationToken = default);
}
