using System;
using System.Threading;
using System.Threading.Tasks;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed record UserSecurityState(
    Guid Id,
    string UserName,
    string DisplayName,
    UserRoleCode RoleCode,
    UserStatus Status,
    bool MustChangePassword,
    byte[] RowVersion);

public sealed record UserProfileState(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Email,
    UserRoleCode RoleCode,
    UserStatus Status,
    byte[] RowVersion);

public enum UserProfileUpdateStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    UserNotFound,
    StaleConcurrency,
    EmailConflict,
    InvalidInput
}

public sealed record UserProfileUpdateResult(
    UserProfileUpdateStatus Status,
    UserProfileState? Profile = null,
    string? Message = null);

public enum AdminPasswordResetStatus
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

public sealed record AdminPasswordResetResult(
    AdminPasswordResetStatus Status,
    UserSecurityState? Target = null,
    string? Message = null);

public sealed record SessionSecurityState(
    Guid Id,
    Guid UserId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool IsActive,
    byte[] RowVersion);

public sealed record RefreshTokenSecurityState(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool IsActive,
    byte[] RowVersion);

public enum RotateRefreshTokenStatus
{
    Success,
    StaleConcurrency,
    AlreadyRevoked,
    SessionRevoked,
    NotFound,
    Expired,
    InvalidToken
}

public sealed record RotateRefreshTokenResult(
    RotateRefreshTokenStatus Status,
    RefreshToken? NewToken = null,
    string? ErrorMessage = null);

public enum IssueSessionStatus
{
    Success,
    UserNotFound,
    UserNotEligible,
    StaleConcurrency,
    DuplicateCredential,
    InvalidInput
}

public sealed record IssueSessionResult(
    IssueSessionStatus Status,
    Guid? SessionId = null,
    string? ErrorMessage = null);

public enum ForcedPasswordChangeStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    UserNotFound,
    NotRequired,
    StaleConcurrency,
    InvalidInput
}

public sealed record ForcedPasswordChangeResult(
    ForcedPasswordChangeStatus Status,
    string? ErrorMessage = null);

public enum ReplayRevocationStatus
{
    Success,
    IdempotentReplay,
    TokenNotFound,
    StaleConcurrency,
    InvalidInput
}

public sealed record ReplayRevocationResult(
    ReplayRevocationStatus Status,
    Guid? SessionId = null,
    string? ErrorMessage = null);

public enum UserRoleChangeStatus
{
    Success,
    StaleConcurrency,
    IdempotentReplay,
    IdempotentConflict,
    UserNotFound,
    RoleNotFound
}

public sealed record UserRoleChangeResult(
    UserRoleChangeStatus Status,
    UserRoleCode RoleCode,
    string? Message = null);

public interface IIdentityRepository
{
    Task<UserProfileState?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileUpdateResult> UpdateUserProfileAtomicAsync(
        Guid userId,
        string displayName,
        string? email,
        byte[] expectedRowVersion,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<AdminPasswordResetResult> ResetUserPasswordAtomicAsync(
        Guid actorUserId,
        Guid targetUserId,
        string newPasswordHash,
        string newSecurityStamp,
        byte[] expectedTargetRowVersion,
        string requestFingerprint,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<UserSecurityState?> GetUserSecurityStateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserSecurityState?> GetUserSecurityStateByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> IsRoleActiveAsync(UserRoleCode roleCode, CancellationToken cancellationToken = default);

    Task<SessionSecurityState?> GetSessionSecurityStateAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<RefreshTokenSecurityState?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<RotateRefreshTokenResult> RotateRefreshTokenAsync(
        Guid oldTokenId,
        byte[] expectedRowVersion,
        RefreshToken newToken,
        CancellationToken cancellationToken = default);

    Task<IssueSessionResult> IssueSessionWithRefreshTokenAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        UserSession session,
        RefreshToken initialRefreshToken,
        DateTimeOffset successfulLoginAt,
        CancellationToken cancellationToken = default);

    Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        string newPasswordHash,
        string newSecurityStamp,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        string newPasswordHash,
        string newSecurityStamp,
        string requestFingerprint,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<ReplayRevocationResult> RevokeRefreshTokenFamilyForReplayAsync(
        Guid refreshTokenId,
        byte[] expectedTokenRowVersion,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default);

    Task RevokeSessionAndFamilyAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<UserRoleChangeResult> ChangeUserRoleAtomicAsync(
        Guid userId,
        UserRoleCode newRoleCode,
        byte[] expectedRowVersion,
        Guid actorUserId,
        Guid operationId,
        Guid? correlationId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
