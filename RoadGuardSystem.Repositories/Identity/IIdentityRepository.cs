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
    Task<UserSecurityState?> GetUserSecurityStateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserSecurityState?> GetUserSecurityStateByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<SessionSecurityState?> GetSessionSecurityStateAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<RefreshTokenSecurityState?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<RotateRefreshTokenResult> RotateRefreshTokenAsync(
        Guid oldTokenId,
        byte[] expectedRowVersion,
        RefreshToken newToken,
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
