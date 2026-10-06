using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<V2AccountUpdatePersistenceResult> UpdateAccountV2AtomicAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserStatus status,
        UserRoleCode roleCode,
        string reason,
        byte[] expectedRowVersion,
        string idempotencyKey,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        const string operation = "AdminAccountUpdateV2";
        if (actorUserId == Guid.Empty || targetUserId == Guid.Empty ||
            status is not (UserStatus.Active or UserStatus.Suspended) ||
            roleCode == UserRoleCode.Unknown || expectedRowVersion.Length == 0 ||
            string.IsNullOrWhiteSpace(idempotencyKey) || operationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(reason))
        {
            return new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.InvalidInput);
        }

        var fingerprint = HashSha256(
            $"target:{targetUserId:N};status:{status};role:{roleCode};reason:{reason}");
        var existing = await FindIdempotencyRecordAsync(
            actorUserId,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return await MapReplayAsync(existing, fingerprint, targetUserId, cancellationToken);
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var replay = await FindIdempotencyRecordAsync(
                        actorUserId,
                        operation,
                        idempotencyKey,
                        attemptCancellationToken);
                    if (replay is not null)
                    {
                        return await MapReplayAsync(replay, fingerprint, targetUserId, attemptCancellationToken);
                    }

                    var target = await _context.Users.SingleOrDefaultAsync(
                        item => item.Id == targetUserId,
                        attemptCancellationToken);
                    if (target is null)
                    {
                        return new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.NotFound);
                    }

                    if (!target.RowVersion.SequenceEqual(expectedRowVersion))
                    {
                        return new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.StaleConcurrency);
                    }

                    if (!await _context.Roles.AnyAsync(
                            item => item.Code == roleCode && item.IsActive,
                            attemptCancellationToken))
                    {
                        return new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.RoleNotFound);
                    }

                    var oldStatus = target.Status;
                    var oldRole = target.RoleCode;
                    var now = _timeProvider.GetUtcNow();
                    using var roleMutationScope = _context.PermitRoleMutationScope();
                    target.RoleCode = roleCode;
                    target.Status = status;
                    target.SuspendedAt = status == UserStatus.Suspended ? now : null;

                    var activeSessions = await _context.Sessions
                        .Where(item => item.UserId == targetUserId && item.RevokedAt == null)
                        .ToListAsync(attemptCancellationToken);
                    var sessionIds = activeSessions.Select(item => item.Id).ToList();
                    foreach (var session in activeSessions)
                    {
                        session.RevokedAt = now;
                    }

                    if (sessionIds.Count > 0)
                    {
                        var tokens = await _context.RefreshTokens
                            .Where(item => sessionIds.Contains(item.SessionId) && item.RevokedAt == null)
                            .ToListAsync(attemptCancellationToken);
                        foreach (var token in tokens)
                        {
                            token.RevokedAt = now;
                        }
                    }

                    if (oldStatus != status)
                    {
                        _context.AccountStatusChangeLogs.Add(new AccountStatusChangeLog
                        {
                            Id = Guid.NewGuid(),
                            TargetUserId = targetUserId,
                            ChangedByUserId = actorUserId,
                            OccurredAt = now,
                            FromStatus = oldStatus,
                            ToStatus = status,
                            Reason = status == UserStatus.Suspended
                                ? SecurityLogSafeValueCodes.Reasons.AdministrativeLock
                                : SecurityLogSafeValueCodes.Reasons.AccountReactivated,
                            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
                            CorrelationId = correlationId
                        });
                    }

                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        actorUserId,
                        now,
                        "user_account_updated",
                        "User",
                        targetUserId,
                        JsonSerializer.Serialize(new { status = oldStatus.ToString(), role = oldRole.ToDbCode() }),
                        JsonSerializer.Serialize(new { status = status.ToString(), role = roleCode.ToDbCode() }),
                        reason,
                        "IdentityRepository",
                        correlationId,
                        ["status", "role"]));
                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        actorUserId,
                        null,
                        operation,
                        idempotencyKey,
                        fingerprint,
                        operationId,
                        "{\"status\":\"success\"}",
                        now));

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new V2AccountUpdatePersistenceResult(
                        V2AccountUpdatePersistenceStatus.Success,
                        ToProfile(target));
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var record = await FindIdempotencyRecordAsync(
                        actorUserId,
                        operation,
                        idempotencyKey,
                        verificationCancellationToken);
                    return record?.RequestFingerprint == fingerprint;
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(
                actorUserId,
                operation,
                idempotencyKey,
                CancellationToken.None);
            return winner is null
                ? new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.StaleConcurrency)
                : await MapReplayAsync(winner, fingerprint, targetUserId, CancellationToken.None);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(
                actorUserId,
                operation,
                idempotencyKey,
                CancellationToken.None);
            return winner is null
                ? new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.StaleConcurrency)
                : await MapReplayAsync(winner, fingerprint, targetUserId, CancellationToken.None);
        }
    }

    private async Task<V2AccountUpdatePersistenceResult> MapReplayAsync(
        IdempotencyRecord record,
        string fingerprint,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(record.RequestFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.IdempotentConflict);
        }

        var profile = await GetUserProfileAsync(targetUserId, cancellationToken);
        return new V2AccountUpdatePersistenceResult(V2AccountUpdatePersistenceStatus.IdempotentReplay, profile);
    }

    private static UserProfileState ToProfile(ApplicationUser user) => new(
        user.Id,
        user.UserName!,
        user.DisplayName,
        user.Email,
        user.RoleCode,
        user.Status,
        user.RowVersion);
}
