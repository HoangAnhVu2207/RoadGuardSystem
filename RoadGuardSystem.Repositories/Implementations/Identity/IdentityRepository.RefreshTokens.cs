using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public Task<RotateRefreshTokenResult> RotateRefreshTokenAsync(Guid oldTokenId,
        byte[] expectedRowVersion, RefreshToken newToken, CancellationToken cancellationToken = default) =>
        RotateRefreshTokenCoreAsync(oldTokenId, expectedRowVersion, newToken, null, null, null, cancellationToken);

    public Task<RotateRefreshTokenResult> RotateRefreshTokenWithReceiptAsync(Guid oldTokenId,
        byte[] expectedRowVersion, RefreshToken newToken, string operationKey, string originalTokenHash,
        string protectedCredential, CancellationToken cancellationToken = default) =>
        RotateRefreshTokenCoreAsync(oldTokenId, expectedRowVersion, newToken, operationKey,
            originalTokenHash, protectedCredential, cancellationToken);

    private async Task<RotateRefreshTokenResult> RotateRefreshTokenCoreAsync(Guid oldTokenId,
        byte[] expectedRowVersion, RefreshToken newToken, string? operationKey, string? originalTokenHash,
        string? protectedCredential, CancellationToken cancellationToken)
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
                    if (session is null || session.UserId != user.Id || session.IssuedRole is { } snapshotRole && snapshotRole != user.RoleCode || session.RevokedAt is not null ||
                        !session.IsActiveAt(_timeProvider.GetUtcNow()))
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null,
                            "Parent session is revoked or expired.");

                    var oldToken = await _context.RefreshTokens
                        .FromSqlInterpolated($"SELECT * FROM [RefreshTokens] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={oldTokenId}")
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    if (oldToken is null)
                    {
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.NotFound, null, "Token not found.");
                    }

                    var now = _timeProvider.GetUtcNow();
                    var receiptKey = $"{oldTokenId:N}:{operationKey}";
                    var fingerprint = HashSha256($"{oldToken.TokenHash}:{session.Id:N}:{session.Transport}");
                    if (operationKey is not null)
                    {
                        if (oldToken.TokenHash != originalTokenHash || operationKey.Length > 150 ||
                            string.IsNullOrWhiteSpace(operationKey) || string.IsNullOrWhiteSpace(protectedCredential))
                            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.InvalidToken);
                        var receipt = await FindIdempotencyRecordAsync(user.Id, "RefreshRotation", receiptKey, attemptCancellationToken);
                        if (receipt is not null)
                        {
                            RefreshRotationOutcome? outcome;
                            try { outcome = System.Text.Json.JsonSerializer.Deserialize<RefreshRotationOutcome>(receipt.OutcomeJson); }
                            catch (System.Text.Json.JsonException) { return new RotateRefreshTokenResult(RotateRefreshTokenStatus.InvalidToken); }
                            if (outcome is null) return new RotateRefreshTokenResult(RotateRefreshTokenStatus.InvalidToken);
                            var successor = await _context.RefreshTokens.FromSqlInterpolated($"SELECT * FROM [RefreshTokens] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={outcome.SuccessorId}").SingleOrDefaultAsync(attemptCancellationToken);
                            if (receipt.RequestFingerprint != fingerprint || now >= outcome.ReplayUntil ||
                                successor is null || successor.SessionId != session.Id || !successor.IsActiveAt(now))
                                return new RotateRefreshTokenResult(RotateRefreshTokenStatus.AlreadyRevoked);
                            return new RotateRefreshTokenResult(RotateRefreshTokenStatus.Success, successor,
                                ProtectedCredential: outcome.ProtectedCredential);
                        }
                    }

                    if (!oldToken.RowVersion.SequenceEqual(expectedRowVersion))
                    {
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.StaleConcurrency, null, "Stale concurrency token.");
                    }

                    if (oldToken.RevokedAt != null)
                    {
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.AlreadyRevoked, null, "Token is already revoked.");
                    }

                    if (oldToken.ExpiresAt <= now)
                    {
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.Expired, null, "Token is expired.");
                    }

                    if (oldToken.SessionId != session.Id || session.RevokedAt != null || !session.IsActiveAt(now))
                    {
                        return new RotateRefreshTokenResult(RotateRefreshTokenStatus.SessionRevoked, null, "Parent session is revoked or expired.");
                    }

                    oldToken.RevokedAt = now;
                    newToken.SessionId = oldToken.SessionId;
                    newToken.ExpiresAt = session.Lifecycle == SessionLifecycle.PersistentRenewable ? null : newToken.ExpiresAt <= session.ExpiresAt
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
                    if (operationKey is not null)
                        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(user.Id, null, "RefreshRotation",
                            receiptKey, fingerprint, newToken.Id,
                            System.Text.Json.JsonSerializer.Serialize(new RefreshRotationOutcome(newToken.Id,
                                protectedCredential!, now.AddMinutes(2))), now));
                    await _context.SaveChangesAsync(attemptCancellationToken);
                    _context.ChangeTracker.Clear();
                    return new RotateRefreshTokenResult(RotateRefreshTokenStatus.Success, newToken, ProtectedCredential: protectedCredential);
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    await using var tx = await _context.Database.BeginTransactionAsync(verificationCancellationToken);
                    var owner = await _context.Sessions.AsNoTracking().Where(s => s.Id == newToken.SessionId)
                        .Select(s => (Guid?)s.UserId).SingleOrDefaultAsync(verificationCancellationToken);
                    if (owner is null) return false;
                    var currentUser = await _context.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={owner.Value}")
                        .SingleOrDefaultAsync(verificationCancellationToken);
                    if (currentUser is null || currentUser.Status != UserStatus.Active || currentUser.MustChangePassword) return false;
                    var currentRoleCode = currentUser.RoleCode.ToDbCode();
                    var currentRole = await _context.Roles.FromSqlInterpolated($"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={currentRoleCode}")
                        .SingleOrDefaultAsync(verificationCancellationToken);
                    var currentSession = await _context.Sessions.FromSqlInterpolated($"SELECT * FROM [Sessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={newToken.SessionId}")
                        .SingleOrDefaultAsync(verificationCancellationToken);
                    if (currentRole is null || !currentRole.IsActive || currentSession is null ||
                        !currentSession.IsActiveAt(_timeProvider.GetUtcNow()) ||
                        currentSession.IssuedRole is { } issuedRole && issuedRole != currentUser.RoleCode) return false;
                    var successor = await _context.RefreshTokens.FromSqlInterpolated($"SELECT * FROM [RefreshTokens] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={newToken.Id}")
                        .SingleOrDefaultAsync(verificationCancellationToken);
                    return successor is not null && successor.TokenHash == newToken.TokenHash && successor.SessionId == currentSession.Id &&
                        successor.IsActiveAt(_timeProvider.GetUtcNow()) && await _context.RefreshTokens.AsNoTracking()
                            .AnyAsync(t => t.Id == oldTokenId && t.RevokedAt != null, verificationCancellationToken);
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

    private sealed record RefreshRotationOutcome(Guid SuccessorId, string ProtectedCredential, DateTimeOffset ReplayUntil);

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
                        .Select(token => new { token.SessionId, token.Session.UserId, token.RowVersion })
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    if (replayState is null)
                    {
                        return new ReplayRevocationResult(ReplayRevocationStatus.TokenNotFound);
                    }

                    if (!replayState.RowVersion.SequenceEqual(expectedTokenRowVersion))
                    {
                        return new ReplayRevocationResult(ReplayRevocationStatus.StaleConcurrency);
                    }

                    _ = await _context.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={replayState.UserId}")
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    var now = _timeProvider.GetUtcNow();
                    var session = await _context.Sessions.FromSqlInterpolated($"SELECT * FROM [Sessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={replayState.SessionId}")
                        .Include(candidate => candidate.RefreshTokens).SingleAsync(attemptCancellationToken);
                    if (session.RevokedAt is null)
                    {
                        session.RevokedAt = now;
                    }

                    foreach (var token in session.RefreshTokens.Where(token =>
                                 token.RevokedAt is null))
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

    public async Task RevokeSessionAndFamilyAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var owner = await _context.Sessions.AsNoTracking().Where(s => s.Id == sessionId)
            .Select(s => (Guid?)s.UserId).SingleOrDefaultAsync(cancellationToken);
        if (owner is null) return;
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(async ct =>
        {
            _context.ChangeTracker.Clear();
            _ = await _context.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={owner.Value}")
                .SingleOrDefaultAsync(ct);
            var session = await _context.Sessions.FromSqlInterpolated($"SELECT * FROM [Sessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={sessionId}")
                .Include(s => s.RefreshTokens).SingleOrDefaultAsync(ct);
            if (session is null) return;
            var now = _timeProvider.GetUtcNow();
            session.RevokedAt ??= now;
            foreach (var token in session.RefreshTokens.Where(t => t.RevokedAt is null)) token.RevokedAt = now;
            await _context.SaveChangesAsync(ct);
        }, async ct => await _context.Sessions.AsNoTracking().AnyAsync(s => s.Id == sessionId && s.RevokedAt != null, ct), cancellationToken);
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

                    _ = await _context.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={userId}")
                        .SingleOrDefaultAsync(attemptCancellationToken);
                    var session = await _context.Sessions.FromSqlInterpolated($"SELECT * FROM [Sessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={sessionId}")
                        .Include(candidate => candidate.RefreshTokens)
                        .SingleOrDefaultAsync(candidate => candidate.UserId == userId, attemptCancellationToken);
                    var now = _timeProvider.GetUtcNow();
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
