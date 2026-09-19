using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly RoadGuardDbContext _context;

    public IdentityRepository(RoadGuardDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<UserSecurityState?> GetUserSecurityStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSecurityState(
                u.Id,
                u.UserName!,
                u.DisplayName,
                u.RoleCode,
                u.Status,
                u.MustChangePassword,
                u.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserSecurityState?> GetUserSecurityStateByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalized = username.Trim().ToUpperInvariant();
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.NormalizedUserName == normalized || u.UserName == username)
            .Select(u => new UserSecurityState(
                u.Id,
                u.UserName!,
                u.DisplayName,
                u.RoleCode,
                u.Status,
                u.MustChangePassword,
                u.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SessionSecurityState?> GetSessionSecurityStateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Select(s => new SessionSecurityState(
                s.Id,
                s.UserId,
                s.IssuedAt,
                s.ExpiresAt,
                s.RevokedAt,
                s.RevokedAt == null && s.ExpiresAt > DateTimeOffset.UtcNow,
                s.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RefreshTokenSecurityState?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return null;
        }

        return await _context.RefreshTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => new RefreshTokenSecurityState(
                t.Id,
                t.SessionId,
                t.Session.UserId,
                t.ExpiresAt,
                t.RevokedAt,
                t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow,
                t.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RotateRefreshTokenResult> RotateRefreshTokenAsync(
        Guid oldTokenId,
        byte[] expectedRowVersion,
        RefreshToken newToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedRowVersion);
        ArgumentNullException.ThrowIfNull(newToken);

        if (string.IsNullOrWhiteSpace(newToken.TokenHash) || newToken.TokenHash.Trim().Length < 32)
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.InvalidToken, null, "TokenHash must be a valid non-empty cryptographic hash (at least 32 characters).");
        }

        var oldToken = await _context.RefreshTokens
            .Include(t => t.Session)
            .SingleOrDefaultAsync(t => t.Id == oldTokenId, cancellationToken);

        if (oldToken is null)
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.NotFound, null, "Token not found.");
        }

        if (!oldToken.RowVersion.SequenceEqual(expectedRowVersion))
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.StaleConcurrency, null, "Stale concurrency token.");
        }

        if (oldToken.RevokedAt != null)
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.AlreadyRevoked, null, "Token is already revoked.");
        }

        var now = DateTimeOffset.UtcNow;
        if (oldToken.ExpiresAt <= now)
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.Expired, null, "Token is expired.");
        }

        if (oldToken.Session.RevokedAt != null || oldToken.Session.ExpiresAt <= now)
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null, "Parent session is revoked or expired.");
        }

        oldToken.RevokedAt = now;

        newToken.SessionId = oldToken.SessionId;
        _context.RefreshTokens.Add(newToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.Success, newToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.StaleConcurrency, null, ex.Message);
        }
    }

    public async Task RevokeSessionAndFamilyAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.Sessions
            .Include(s => s.RefreshTokens)
            .SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.RevokedAt = now;

        foreach (var token in session.RefreshTokens.Where(t => t.RevokedAt == null))
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserRoleChangeResult> ChangeUserRoleAtomicAsync(
        Guid userId,
        UserRoleCode newRoleCode,
        byte[] expectedRowVersion,
        Guid actorUserId,
        Guid operationId,
        Guid? correlationId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (newRoleCode == UserRoleCode.Unknown)
        {
            throw new ArgumentException("Cannot change role to Unknown.", nameof(newRoleCode));
        }

        var idempotencyKey = operationId.ToString();
        var rawFingerprint = $"userId:{userId:N};targetRole:{newRoleCode.ToDbCode()}";
        var requestFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawFingerprint))).ToLowerInvariant();

        // 1. Check idempotency record
        var existingRecord = await _context.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.Operation == "UserRoleChanged" && r.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existingRecord is not null)
        {
            if (existingRecord.RequestFingerprint == requestFingerprint)
            {
                return new UserRoleChangeResult(UserRoleChangeStatus.IdempotentReplay, newRoleCode);
            }

            return new UserRoleChangeResult(
                UserRoleChangeStatus.IdempotentConflict,
                newRoleCode,
                "Idempotency key was previously executed with a different payload.");
        }

        // 2. Fetch user
        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return new UserRoleChangeResult(UserRoleChangeStatus.UserNotFound, newRoleCode, "User not found.");
        }

        if (!user.RowVersion.SequenceEqual(expectedRowVersion))
        {
            return new UserRoleChangeResult(UserRoleChangeStatus.StaleConcurrency, user.RoleCode, "Stale user concurrency row version.");
        }

        // 3. Verify target role exists
        var targetRole = await _context.Roles
            .SingleOrDefaultAsync(r => r.Code == newRoleCode, cancellationToken);

        if (targetRole is null || !targetRole.IsActive)
        {
            return new UserRoleChangeResult(UserRoleChangeStatus.RoleNotFound, newRoleCode, "Target role does not exist or is inactive.");
        }

        // 4. Execute atomic update
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var oldRoleCode = user.RoleCode;
                var now = DateTimeOffset.UtcNow;

                using (_context.PermitRoleMutationScope())
                {
                    // A. Update role
                    user.RoleCode = newRoleCode;

                    // B. Revoke all active sessions (exclude already expired or revoked)
                    var activeSessions = await _context.Sessions
                        .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
                        .ToListAsync(cancellationToken);

                    var sessionIds = activeSessions.Select(s => s.Id).ToList();

                    foreach (var s in activeSessions)
                    {
                        s.RevokedAt = now;
                    }

                    // C. Revoke all active refresh tokens for these sessions (exclude already expired or revoked)
                    if (sessionIds.Count > 0)
                    {
                        var activeTokens = await _context.RefreshTokens
                            .Where(t => sessionIds.Contains(t.SessionId) && t.RevokedAt == null && t.ExpiresAt > now)
                            .ToListAsync(cancellationToken);

                        foreach (var t in activeTokens)
                        {
                            t.RevokedAt = now;
                        }
                    }

                    // D. Append AuditLog
                    var auditLog = AuditLog.Create(
                        id: Guid.NewGuid(),
                        actorUserId: actorUserId,
                        occurredAt: now,
                        eventType: "UserRoleChanged",
                        entityType: "User",
                        entityId: userId,
                        beforeSnapshot: $"{{\"role_code\":\"{oldRoleCode.ToDbCode()}\"}}",
                        afterSnapshot: $"{{\"role_code\":\"{newRoleCode.ToDbCode()}\"}}",
                        reason: reason ?? "Authoritative role change with credential revocation",
                        source: "IdentityRepository",
                        correlationId: correlationId,
                        snapshotAllowedPropertyNames: new[] { "role_code" });

                    _context.AuditLogs.Add(auditLog);

                    // E. Append IdempotencyRecord
                    var idempRecord = IdempotencyRecord.Create(
                        actorUserId: actorUserId,
                        projectId: null,
                        operation: "UserRoleChanged",
                        idempotencyKey: idempotencyKey,
                        requestFingerprint: requestFingerprint,
                        operationId: operationId,
                        outcomeJson: $"{{\"role_code\":\"{newRoleCode.ToDbCode()}\"}}",
                        createdAt: now);

                    _context.IdempotencyRecords.Add(idempRecord);

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return new UserRoleChangeResult(UserRoleChangeStatus.Success, newRoleCode);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                var winnerRecord = await _context.IdempotencyRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        r => r.Operation == "UserRoleChanged" && r.IdempotencyKey == idempotencyKey,
                        CancellationToken.None);

                if (winnerRecord is not null)
                {
                    if (winnerRecord.RequestFingerprint == requestFingerprint)
                    {
                        return new UserRoleChangeResult(UserRoleChangeStatus.IdempotentReplay, newRoleCode);
                    }
                    return new UserRoleChangeResult(
                        UserRoleChangeStatus.IdempotentConflict,
                        newRoleCode,
                        "Idempotency key was previously executed with a different payload.");
                }

                return new UserRoleChangeResult(
                    UserRoleChangeStatus.StaleConcurrency,
                    user.RoleCode,
                    "Concurrent update conflicted with expected user row version.");
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                var winnerRecord = await _context.IdempotencyRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        r => r.Operation == "UserRoleChanged" && r.IdempotencyKey == idempotencyKey,
                        CancellationToken.None);

                if (winnerRecord is not null)
                {
                    if (winnerRecord.RequestFingerprint == requestFingerprint)
                    {
                        return new UserRoleChangeResult(UserRoleChangeStatus.IdempotentReplay, newRoleCode);
                    }
                    return new UserRoleChangeResult(
                        UserRoleChangeStatus.IdempotentConflict,
                        newRoleCode,
                        "Idempotency key was previously executed with a different payload.");
                }

                throw;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }
}
