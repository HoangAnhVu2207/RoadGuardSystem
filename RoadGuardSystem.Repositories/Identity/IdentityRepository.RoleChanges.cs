using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
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
                record => record.Operation == "UserRoleChanged" && record.IdempotencyKey == idempotencyKey,
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
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

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
            .SingleOrDefaultAsync(role => role.Code == newRoleCode, cancellationToken);

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
                        .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > now)
                        .ToListAsync(cancellationToken);

                    var sessionIds = activeSessions.Select(session => session.Id).ToList();

                    foreach (var session in activeSessions)
                    {
                        session.RevokedAt = now;
                    }

                    // C. Revoke all active refresh tokens for these sessions (exclude already expired or revoked)
                    if (sessionIds.Count > 0)
                    {
                        var activeTokens = await _context.RefreshTokens
                            .Where(token => sessionIds.Contains(token.SessionId) && token.RevokedAt == null && token.ExpiresAt > now)
                            .ToListAsync(cancellationToken);

                        foreach (var token in activeTokens)
                        {
                            token.RevokedAt = now;
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
                    var idempotencyRecord = IdempotencyRecord.Create(
                        actorUserId: actorUserId,
                        projectId: null,
                        operation: "UserRoleChanged",
                        idempotencyKey: idempotencyKey,
                        requestFingerprint: requestFingerprint,
                        operationId: operationId,
                        outcomeJson: $"{{\"role_code\":\"{newRoleCode.ToDbCode()}\"}}",
                        createdAt: now);

                    _context.IdempotencyRecords.Add(idempotencyRecord);

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
                        record => record.Operation == "UserRoleChanged" && record.IdempotencyKey == idempotencyKey,
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
                        record => record.Operation == "UserRoleChanged" && record.IdempotencyKey == idempotencyKey,
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
