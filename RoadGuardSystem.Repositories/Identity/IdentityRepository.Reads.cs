using Microsoft.EntityFrameworkCore;

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

    public async Task<SessionSecurityState?> GetSessionSecurityStateAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => new SessionSecurityState(
                session.Id,
                session.UserId,
                session.IssuedAt,
                session.ExpiresAt,
                session.RevokedAt,
                session.RevokedAt == null && session.ExpiresAt > DateTimeOffset.UtcNow,
                session.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
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
                token.RevokedAt == null && token.ExpiresAt > DateTimeOffset.UtcNow,
                token.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
