using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;

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
                    var tokenOwner = await _context.RefreshTokens.AsNoTracking()
                        .Where(token => token.Id == oldTokenId)
                        .Select(token => new { token.Session.UserId, token.SessionId })
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    if (tokenOwner is null)
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.NotFound, null, "Token not found.");

                    var user = await _context.Users.FromSqlInterpolated(
                            $"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={tokenOwner.UserId}")
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    if (user is null || user.Status != UserStatus.Active || user.MustChangePassword)
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null,
                            "User is no longer authorized.");
                    var roleCode = user.RoleCode.ToDbCode();
                    var role = await _context.Roles.FromSqlInterpolated(
                            $"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={roleCode}")
                        .AsNoTracking().SingleOrDefaultAsync(attemptCancellationToken);
                    if (role is null || !role.IsActive)
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null,
                            "Role is no longer active.");
                    var session = await _context.Sessions.FromSqlInterpolated(
                            $"SELECT * FROM [Sessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={tokenOwner.SessionId}")
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    if (session is null || session.UserId != user.Id || session.RevokedAt is not null ||
                        session.ExpiresAt <= DateTimeOffset.UtcNow)
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null,
                            "Parent session is revoked or expired.");

                    var oldToken = await _context.RefreshTokens
                        .FromSqlInterpolated($"SELECT * FROM [RefreshTokens] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={oldTokenId}")
                        .SingleOrDefaultAsync(attemptCancellationToken);
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

                    if (oldToken.SessionId != session.Id || session.RevokedAt != null || session.ExpiresAt <= now)
                    {
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null, "Parent session is revoked or expired.");
                    }

                    oldToken.RevokedAt = now;
                    newToken.SessionId = oldToken.SessionId;
                    newToken.ExpiresAt = newToken.ExpiresAt <= session.ExpiresAt
                        ? newToken.ExpiresAt
                        : session.ExpiresAt;
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

    public async Task<LogoutPersistenceResult> RevokeSessionAndFamilyAtomicAsync(
        Guid userId,
        Guid sessionId,
        string idempotencyKey,
        string requestFingerprint,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty ||
            string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(requestFingerprint))
        {
            return new LogoutPersistenceResult(IdempotentConflict: true);
        }

        const string operation = "Logout";
        var existing = await FindIdempotencyRecordAsync(userId, operation, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return new LogoutPersistenceResult(
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
                        return new LogoutPersistenceResult(
                            IdempotentReplay: durable.RequestFingerprint == requestFingerprint,
                            IdempotentConflict: durable.RequestFingerprint != requestFingerprint);
                    }

                    var session = await _context.Sessions
                        .Include(candidate => candidate.RefreshTokens)
                        .SingleOrDefaultAsync(candidate =>
                            candidate.Id == sessionId && candidate.UserId == userId,
                            attemptCancellationToken);
                    var now = DateTimeOffset.UtcNow;
                    if (session is not null)
                    {
                        session.RevokedAt ??= now;
                        foreach (var token in session.RefreshTokens.Where(token => token.RevokedAt is null))
                        {
                            token.RevokedAt = now;
                        }
                    }

                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        userId,
                        null,
                        operation,
                        idempotencyKey,
                        requestFingerprint,
                        Guid.NewGuid(),
                        "{\"status\":\"completed\"}",
                        now));
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(), userId, now, "auth_logout", "Session", sessionId,
                        null, null, null, "IdentityRepository", correlationId));

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new LogoutPersistenceResult();
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    return await FindIdempotencyRecordAsync(
                        userId, operation, idempotencyKey, verificationCancellationToken) is not null;
                },
                cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            var winner = await FindIdempotencyRecordAsync(userId, operation, idempotencyKey, CancellationToken.None);
            if (winner is null) throw;
            return new LogoutPersistenceResult(
                IdempotentReplay: winner.RequestFingerprint == requestFingerprint,
                IdempotentConflict: winner.RequestFingerprint != requestFingerprint);
        }
    }
}
