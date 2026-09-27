using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<PasswordChangePersistenceResult> ChangePasswordAtomicAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        string newPasswordHash,
        string newSecurityStamp,
        string idempotencyKey,
        string requestFingerprint,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        const string operation = "ChangePassword";
        var existing = await FindIdempotencyRecordAsync(userId, operation, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return new PasswordChangePersistenceResult(
                IdempotentReplay: existing.RequestFingerprint == requestFingerprint,
                IdempotentConflict: existing.RequestFingerprint != requestFingerprint);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var durable = await FindIdempotencyRecordAsync(
                        userId, operation, idempotencyKey, attemptCancellationToken);
                    if (durable is not null)
                    {
                        return new PasswordChangePersistenceResult(
                            IdempotentReplay: durable.RequestFingerprint == requestFingerprint,
                            IdempotentConflict: durable.RequestFingerprint != requestFingerprint);
                    }

                    var user = await _context.Users.SingleOrDefaultAsync(
                        candidate => candidate.Id == userId, attemptCancellationToken);
                    if (user is null)
                    {
                        return new PasswordChangePersistenceResult(UserNotFound: true);
                    }

                    if (!user.RowVersion.SequenceEqual(expectedUserRowVersion))
                    {
                        return new PasswordChangePersistenceResult(StaleConcurrency: true);
                    }

                    var now = DateTimeOffset.UtcNow;
                    user.PasswordHash = newPasswordHash;
                    user.SecurityStamp = newSecurityStamp;
                    user.MustChangePassword = false;
                    var sessions = await _context.Sessions
                        .Include(session => session.RefreshTokens)
                        .Where(session => session.UserId == userId && session.RevokedAt == null)
                        .ToListAsync(attemptCancellationToken);
                    foreach (var session in sessions)
                    {
                        session.RevokedAt = now;
                        foreach (var token in session.RefreshTokens.Where(token => token.RevokedAt is null))
                        {
                            token.RevokedAt = now;
                        }
                    }

                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(), userId, now, "auth_password_changed", "User", userId,
                        null, null, null, "IdentityRepository", correlationId));
                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        userId, null, operation, idempotencyKey, requestFingerprint, Guid.NewGuid(),
                        "{\"status\":\"completed\"}", now));

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new PasswordChangePersistenceResult(Succeeded: true);
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    return await FindIdempotencyRecordAsync(
                        userId, operation, idempotencyKey, verificationCancellationToken) is not null;
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return new PasswordChangePersistenceResult(StaleConcurrency: true);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(userId, operation, idempotencyKey, CancellationToken.None);
            if (winner is null) throw;
            return new PasswordChangePersistenceResult(
                IdempotentReplay: winner.RequestFingerprint == requestFingerprint,
                IdempotentConflict: winner.RequestFingerprint != requestFingerprint);
        }
    }

    public Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        string newPasswordHash,
        string newSecurityStamp,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default) =>
        CompleteForcedPasswordChangeAtomicAsync(
            userId,
            expectedUserRowVersion,
            newPasswordHash,
            newSecurityStamp,
            HashSha256($"passwordHash:{newPasswordHash};securityStamp:{newSecurityStamp}"),
            operationId,
            correlationId,
            cancellationToken);

    public async Task<ForcedPasswordChangeResult> CompleteForcedPasswordChangeAtomicAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        string newPasswordHash,
        string newSecurityStamp,
        string requestFingerprint,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedUserRowVersion);
        if (userId == Guid.Empty ||
            expectedUserRowVersion.Length == 0 ||
            string.IsNullOrWhiteSpace(newPasswordHash) ||
            string.IsNullOrWhiteSpace(newSecurityStamp) ||
            string.IsNullOrWhiteSpace(requestFingerprint) ||
            operationId == Guid.Empty)
        {
            return new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.InvalidInput);
        }

        const string operation = "ForcedPasswordChange";
        var idempotencyKey = operationId.ToString("N");

        var existingRecord = await FindIdempotencyRecordAsync(
            userId,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existingRecord is not null)
        {
            return MapForcedPasswordChangeReplay(existingRecord, requestFingerprint);
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var durableOutcome = await FindIdempotencyRecordAsync(
                        userId,
                        operation,
                        idempotencyKey,
                        attemptCancellationToken);
                    if (durableOutcome is not null)
                    {
                        return MapForcedPasswordChangeReplay(durableOutcome, requestFingerprint);
                    }

                    var user = await _context.Users.SingleOrDefaultAsync(
                        candidate => candidate.Id == userId,
                        attemptCancellationToken);
                    if (user is null)
                    {
                        return new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.UserNotFound);
                    }

                    if (!user.MustChangePassword)
                    {
                        _context.ChangeTracker.Clear();
                        var winner = await FindIdempotencyRecordAsync(
                            userId,
                            operation,
                            idempotencyKey,
                            CancellationToken.None);
                        return winner is null
                            ? new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.NotRequired)
                            : MapForcedPasswordChangeReplay(winner, requestFingerprint);
                    }

                    if (!user.RowVersion.SequenceEqual(expectedUserRowVersion))
                    {
                        return new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.StaleConcurrency);
                    }

                    var now = DateTimeOffset.UtcNow;
                    user.PasswordHash = newPasswordHash;
                    user.SecurityStamp = newSecurityStamp;
                    user.MustChangePassword = false;

                    var activeSessions = await _context.Sessions
                        .Where(session =>
                            session.UserId == userId &&
                            session.RevokedAt == null &&
                            session.ExpiresAt > now)
                        .ToListAsync(attemptCancellationToken);
                    var activeSessionIds = activeSessions.Select(session => session.Id).ToList();
                    foreach (var session in activeSessions)
                    {
                        session.RevokedAt = now;
                    }

                    if (activeSessionIds.Count > 0)
                    {
                        var activeTokens = await _context.RefreshTokens
                            .Where(token =>
                                activeSessionIds.Contains(token.SessionId) &&
                                token.RevokedAt == null &&
                                token.ExpiresAt > now)
                            .ToListAsync(attemptCancellationToken);
                        foreach (var token in activeTokens)
                        {
                            token.RevokedAt = now;
                        }
                    }

                    _context.AuditLogs.Add(AuditLog.Create(
                        id: Guid.NewGuid(),
                        actorUserId: userId,
                        occurredAt: now,
                        eventType: "auth_password_changed",
                        entityType: "User",
                        entityId: userId,
                        beforeSnapshot: "{\"must_change_password\":true}",
                        afterSnapshot: "{\"must_change_password\":false}",
                        reason: null,
                        source: "IdentityRepository",
                        correlationId: correlationId,
                        snapshotAllowedPropertyNames: ["must_change_password"]));

                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        actorUserId: userId,
                        projectId: null,
                        operation: operation,
                        idempotencyKey: idempotencyKey,
                        requestFingerprint: requestFingerprint,
                        operationId: operationId,
                        outcomeJson: "{\"status\":\"completed\"}",
                        createdAt: now));

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.Success);
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var winner = await FindIdempotencyRecordAsync(
                        userId,
                        operation,
                        idempotencyKey,
                        verificationCancellationToken);
                    return winner?.RequestFingerprint == requestFingerprint;
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(
                userId,
                operation,
                idempotencyKey,
                CancellationToken.None);
            return winner is null
                ? new ForcedPasswordChangeResult(ForcedPasswordChangeStatus.StaleConcurrency)
                : MapForcedPasswordChangeReplay(winner, requestFingerprint);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(
                userId,
                operation,
                idempotencyKey,
                CancellationToken.None);
            if (winner is null)
            {
                throw;
            }

            return MapForcedPasswordChangeReplay(winner, requestFingerprint);
        }
    }
}
