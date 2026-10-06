using RoadGuardSystem.BusinessObjects.Identity;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<UserSecurityState?> GetUserSecurityStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserSecurityState(
                user.Id,
                user.UserName!,
                user.DisplayName,
                user.RoleCode,
                user.Status,
                user.MustChangePassword,
                user.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserSecurityState?> GetUserSecurityStateByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalized = username.Trim().ToUpperInvariant();
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.NormalizedUserName == normalized || user.UserName == username)
            .Select(user => new UserSecurityState(
                user.Id,
                user.UserName!,
                user.DisplayName,
                user.RoleCode,
                user.Status,
                user.MustChangePassword,
                user.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsRoleActiveAsync(
        UserRoleCode roleCode,
        CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .AnyAsync(role => role.Code == roleCode && role.IsActive, cancellationToken);
    }

    public async Task<SessionSecurityState?> GetSessionSecurityStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var state = await _context.Sessions
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => new SessionSecurityState(
                session.Id,
                session.UserId,
                session.IssuedAt,
                session.ExpiresAt,
                session.RevokedAt,
                session.RevokedAt == null && (session.Lifecycle == SessionLifecycle.PersistentRenewable && session.ExpiresAt == null && session.IssuedRole != null || session.Lifecycle == SessionLifecycle.LegacyBounded && session.ExpiresAt > _timeProvider.GetUtcNow()),
                session.RowVersion,
                session.Transport,
                session.LastActivityAt, session.Lifecycle, session.IssuedRole))
            .SingleOrDefaultAsync(cancellationToken);
        if (state is null) return null;
        return state with { IsActive = new UserSession { IssuedAt = state.IssuedAt, ExpiresAt = state.ExpiresAt,
            RevokedAt = state.RevokedAt, Transport = state.Transport, LastActivityAt = state.LastActivityAt,
            Lifecycle = state.Lifecycle, IssuedRole = state.IssuedRole }.IsActiveAt(_timeProvider.GetUtcNow()) };
    }

    public async Task<RefreshTokenSecurityState?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new RefreshTokenSecurityState(
                token.Id,
                token.SessionId,
                token.Session.UserId,
                token.ExpiresAt,
                token.RevokedAt,
                token.RevokedAt == null && (token.ExpiresAt == null || token.ExpiresAt > _timeProvider.GetUtcNow()),
                token.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
