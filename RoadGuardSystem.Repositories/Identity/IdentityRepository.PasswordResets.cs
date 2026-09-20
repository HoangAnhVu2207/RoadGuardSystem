using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<AdminPasswordResetResult> ResetUserPasswordAtomicAsync(
        Guid actorUserId,
        Guid targetUserId,
        string newPasswordHash,
        string newSecurityStamp,
        byte[] expectedTargetRowVersion,
        string requestFingerprint,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty || targetUserId == Guid.Empty || operationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(newPasswordHash) || string.IsNullOrWhiteSpace(newSecurityStamp) ||
            expectedTargetRowVersion.Length == 0 || string.IsNullOrWhiteSpace(requestFingerprint))
        {
            return new AdminPasswordResetResult(AdminPasswordResetStatus.InvalidInput);
        }

        const string operation = "AdminPasswordReset";
        var idempotencyKey = operationId.ToString("N");
        var existingRecord = await FindIdempotencyRecordAsync(
            actorUserId,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existingRecord is not null)
        {
            return await MapAdminPasswordResetReplayAsync(
                existingRecord,
                requestFingerprint,
                targetUserId,
                cancellationToken);
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var durableOutcome = await FindIdempotencyRecordAsync(
                        actorUserId,
                        operation,
                        idempotencyKey,
                        attemptCancellationToken);
                    if (durableOutcome is not null)
                    {
                        return await MapAdminPasswordResetReplayAsync(
                            durableOutcome,
                            requestFingerprint,
                            targetUserId,
                            attemptCancellationToken);
                    }

                    var actor = await _context.Users.SingleOrDefaultAsync(
                        user => user.Id == actorUserId,
                        attemptCancellationToken);
                    if (actor is null || actor.Status != UserStatus.Active ||
                        actor.RoleCode != UserRoleCode.Supervisor)
                    {
                        return new AdminPasswordResetResult(AdminPasswordResetStatus.ActorNotAuthorized);
                    }

                    var target = await _context.Users.SingleOrDefaultAsync(
                        user => user.Id == targetUserId,
                        attemptCancellationToken);
                    if (target is null)
                    {
                        return new AdminPasswordResetResult(AdminPasswordResetStatus.UserNotFound);
                    }

                    var now = DateTimeOffset.UtcNow;
                    if (target.Status != UserStatus.Active)
                    {
                        _context.PasswordResetLogs.Add(new PasswordResetLog
                        {
                            Id = Guid.NewGuid(),
                            TargetUserId = targetUserId,
                            PerformedByUserId = actorUserId,
                            OccurredAt = now,
                            Reason = SecurityLogSafeValueCodes.Reasons.AdministratorInitiated,
                            Result = PasswordResetResult.Rejected,
                            Source = SecurityLogSafeValueCodes.Sources.AdminApi,
                            CorrelationId = correlationId
                        });
                        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                            actorUserId,
                            null,
                            operation,
                            idempotencyKey,
                            requestFingerprint,
                            operationId,
                            "{\"status\":\"rejected\"}",
                            now));
                        await _context.SaveChangesAsync(attemptCancellationToken);
                        return new AdminPasswordResetResult(AdminPasswordResetStatus.TargetNotActive);
                    }

                    if (!target.RowVersion.SequenceEqual(expectedTargetRowVersion))
                    {
                        return new AdminPasswordResetResult(AdminPasswordResetStatus.StaleConcurrency);
                    }

                    target.PasswordHash = newPasswordHash;
                    target.SecurityStamp = newSecurityStamp;
                    target.MustChangePassword = true;

                    var activeSessions = await _context.Sessions
                        .Where(session =>
                            session.UserId == targetUserId &&
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

                    _context.PasswordResetLogs.Add(new PasswordResetLog
                    {
                        Id = Guid.NewGuid(),
                        TargetUserId = targetUserId,
                        PerformedByUserId = actorUserId,
                        OccurredAt = now,
                        Reason = SecurityLogSafeValueCodes.Reasons.AdministratorInitiated,
                        Result = PasswordResetResult.Success,
                        Source = SecurityLogSafeValueCodes.Sources.AdminApi,
                        CorrelationId = correlationId
                    });
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        actorUserId,
                        now,
                        "user_password_reset",
                        "User",
                        targetUserId,
                        "{\"must_change_password\":false}",
                        "{\"must_change_password\":true}",
                        null,
                        "IdentityRepository",
                        correlationId,
                        ["must_change_password"]));
                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        actorUserId,
                        null,
                        operation,
                        idempotencyKey,
                        requestFingerprint,
                        operationId,
                        "{\"status\":\"success\"}",
                        now));

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new AdminPasswordResetResult(
                        AdminPasswordResetStatus.Success,
                        new UserSecurityState(
                            target.Id,
                            target.UserName!,
                            target.DisplayName,
                            target.RoleCode,
                            target.Status,
                            target.MustChangePassword,
                            target.RowVersion));
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var winner = await FindIdempotencyRecordAsync(
                        actorUserId,
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
                actorUserId,
                operation,
                idempotencyKey,
                CancellationToken.None);
            return winner is null
                ? new AdminPasswordResetResult(AdminPasswordResetStatus.StaleConcurrency)
                : await MapAdminPasswordResetReplayAsync(
                    winner,
                    requestFingerprint,
                    targetUserId,
                    CancellationToken.None);
        }
    }

    private async Task<AdminPasswordResetResult> MapAdminPasswordResetReplayAsync(
        IdempotencyRecord record,
        string requestFingerprint,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(record.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            return new AdminPasswordResetResult(
                AdminPasswordResetStatus.IdempotentConflict,
                Message: "Idempotency key was previously executed with a different payload.");
        }

        var target = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == targetUserId)
            .Select(user => new UserSecurityState(
                user.Id,
                user.UserName!,
                user.DisplayName,
                user.RoleCode,
                user.Status,
                user.MustChangePassword,
                user.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
        return new AdminPasswordResetResult(AdminPasswordResetStatus.IdempotentReplay, target);
    }
}
