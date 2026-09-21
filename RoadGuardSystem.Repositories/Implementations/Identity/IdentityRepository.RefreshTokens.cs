using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
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

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var oldToken = await _context.RefreshTokens
                        .Include(token => token.Session)
                        .SingleOrDefaultAsync(token => token.Id == oldTokenId, attemptCancellationToken);
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
                    var attemptReplacement = new RefreshToken
                    {
                        Id = newToken.Id,
                        SessionId = oldToken.SessionId,
                        TokenHash = newToken.TokenHash,
                        ExpiresAt = newToken.ExpiresAt,
                        RevokedAt = newToken.RevokedAt
                    };
                    _context.RefreshTokens.Add(attemptReplacement);
                    await _context.SaveChangesAsync(attemptCancellationToken);
                    _context.ChangeTracker.Clear();
                    return new RotateRefreshTokenResult(RotateRefreshTokenStatus.Success, newToken);
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    return await _context.RefreshTokens
                        .AsNoTracking()
                        .Where(token => token.Id == oldTokenId && token.RevokedAt != null)
                        .AnyAsync(token => token.Session.RefreshTokens.Any(replacement =>
                            replacement.Id == newToken.Id &&
                            replacement.TokenHash == newToken.TokenHash), verificationCancellationToken);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _context.ChangeTracker.Clear();
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.StaleConcurrency, null, exception.Message);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.InvalidToken, null, "Replacement token conflicts with an existing credential.");
        }
    }

    public async Task<ReplayRevocationResult> RevokeRefreshTokenFamilyForReplayAsync(
        Guid refreshTokenId,
        byte[] expectedTokenRowVersion,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedTokenRowVersion);
        if (refreshTokenId == Guid.Empty || expectedTokenRowVersion.Length == 0)
        {
            return new ReplayRevocationResult(ReplayRevocationStatus.InvalidInput);
        }

        const string operation = "RefreshTokenReplay";
        var idempotencyKey = refreshTokenId.ToString("N");
        var requestFingerprint = HashSha256($"refreshTokenId:{refreshTokenId:N}");
        var existingRecord = await FindIdempotencyRecordAsync(
            actorUserId: null,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existingRecord is not null)
        {
            return new ReplayRevocationResult(ReplayRevocationStatus.IdempotentReplay);
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var durableOutcome = await FindIdempotencyRecordAsync(
                        actorUserId: null,
                        operation,
                        idempotencyKey,
                        attemptCancellationToken);
                    if (durableOutcome is not null)
                    {
                        return new ReplayRevocationResult(ReplayRevocationStatus.IdempotentReplay);
                    }

                    var replayState = await _context.RefreshTokens
                        .AsNoTracking()
                        .Where(token => token.Id == refreshTokenId)
                        .Select(token => new { token.SessionId, token.RowVersion })
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    if (replayState is null)
                    {
                        return new ReplayRevocationResult(ReplayRevocationStatus.TokenNotFound);
                    }

                    if (!replayState.RowVersion.SequenceEqual(expectedTokenRowVersion))
                    {
                        return new ReplayRevocationResult(ReplayRevocationStatus.StaleConcurrency);
                    }

                    var now = DateTimeOffset.UtcNow;
                    var session = await _context.Sessions
                        .Include(candidate => candidate.RefreshTokens)
                        .SingleAsync(candidate => candidate.Id == replayState.SessionId, attemptCancellationToken);
                    if (session.RevokedAt is null)
                    {
                        session.RevokedAt = now;
                    }

                    foreach (var token in session.RefreshTokens.Where(token =>
                                 token.RevokedAt is null && token.ExpiresAt > now))
                    {
                        token.RevokedAt = now;
                    }

                    _context.AuditLogs.Add(AuditLog.Create(
                        id: Guid.NewGuid(),
                        actorUserId: null,
                        occurredAt: now,
                        eventType: "auth_token_replay_detected",
                        entityType: "Session",
                        entityId: session.Id,
                        beforeSnapshot: null,
                        afterSnapshot: null,
                        reason: null,
                        source: "IdentityRepository",
                        correlationId: correlationId));

                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        actorUserId: null,
                        projectId: null,
                        operation: operation,
                        idempotencyKey: idempotencyKey,
                        requestFingerprint: requestFingerprint,
                        operationId: Guid.NewGuid(),
                        outcomeJson: $"{{\"status\":\"family_revoked\",\"session_id\":\"{session.Id:N}\"}}",
                        createdAt: now));

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new ReplayRevocationResult(ReplayRevocationStatus.Success, session.Id);
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    return await FindIdempotencyRecordAsync(
                        actorUserId: null,
                        operation,
                        idempotencyKey,
                        verificationCancellationToken) is not null;
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(
                actorUserId: null,
                operation,
                idempotencyKey,
                CancellationToken.None);
            return winner is null
                ? new ReplayRevocationResult(ReplayRevocationStatus.StaleConcurrency)
                : new ReplayRevocationResult(ReplayRevocationStatus.IdempotentReplay);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(
                actorUserId: null,
                operation,
                idempotencyKey,
                CancellationToken.None);
            if (winner is null)
            {
                throw;
            }

            return new ReplayRevocationResult(ReplayRevocationStatus.IdempotentReplay);
        }
    }

    public async Task RevokeSessionAndFamilyAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _context.ChangeTracker.Clear();
        var session = await _context.Sessions
            .Include(candidate => candidate.RefreshTokens)
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken);

        if (session is null)
        {
            _context.ChangeTracker.Clear();
            return;
        }

        if (session.RevokedAt is not null)
        {
            _context.ChangeTracker.Clear();
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.RevokedAt = now;

        foreach (var token in session.RefreshTokens.Where(token =>
                     token.RevokedAt == null && token.ExpiresAt > now))
        {
            token.RevokedAt = now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            var persistedRevocation = await _context.Sessions
                .AsNoTracking()
                .Where(candidate => candidate.Id == sessionId)
                .Select(candidate => candidate.RevokedAt)
                .SingleOrDefaultAsync(CancellationToken.None);
            if (persistedRevocation is null)
            {
                throw;
            }
        }
    }
}
