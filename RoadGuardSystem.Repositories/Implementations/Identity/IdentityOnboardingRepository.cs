using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Identity;

public sealed class IdentityOnboardingRepository : IIdentityOnboardingRepository
{
    private const string RegisterOperation = "RegisterReporter";
    private const string ResendOperation = "ResendReporterOtp";
    private const string VerifyOperation = "VerifyReporterOtp";
    private const string CreateInvitationOperation = "CreateInvitation";
    private const string AcceptInvitationOperation = "AcceptInvitation";
    private readonly RoadGuardDbContext _context;

    public IdentityOnboardingRepository(RoadGuardDbContext context)
    {
        _context = context;
    }

    public Task<ReporterRegistrationIntent?> GetReporterRegistrationIntentAsync(
        Guid intentId,
        CancellationToken cancellationToken = default) =>
        _context.ReporterRegistrationIntents.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == intentId, cancellationToken);

    public Task<StaffInvitation?> GetInvitationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _context.StaffInvitations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

    public async Task<ReporterRegistrationPersistenceResult> RegisterReporterAsync(
        ApplicationUser pendingUser,
        ReporterRegistrationIntent intent,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var replay = await FindRecordAsync(null, RegisterOperation, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return await MapReporterReplayAsync(replay, requestFingerprint, cancellationToken);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var winner = await FindRecordAsync(null, RegisterOperation, idempotencyKey, attemptCancellationToken);
                    if (winner is not null)
                    {
                        return await MapReporterReplayAsync(winner, requestFingerprint, attemptCancellationToken);
                    }

                    var emailExists = await _context.Users.AnyAsync(
                        user => user.NormalizedEmail == intent.NormalizedEmail,
                        attemptCancellationToken);
                    if (!emailExists)
                    {
                        _context.Users.Add(pendingUser);
                    }
                    else
                    {
                        intent.UserId = null;
                    }

                    _context.ReporterRegistrationIntents.Add(intent);
                    _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        null,
                        null,
                        RegisterOperation,
                        idempotencyKey,
                        requestFingerprint,
                        operationId,
                        JsonSerializer.Serialize(new { intentId = intent.Id }),
                        intent.CreatedAt));
                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new ReporterRegistrationPersistenceResult(
                        IdentityOnboardingPersistenceStatus.Success,
                        intent);
                },
                async verificationCancellationToken =>
                    await FindRecordAsync(null, RegisterOperation, idempotencyKey, verificationCancellationToken) is not null,
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindRecordAsync(null, RegisterOperation, idempotencyKey, CancellationToken.None);
            return winner is null
                ? new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.Conflict)
                : await MapReporterReplayAsync(winner, requestFingerprint, CancellationToken.None);
        }
    }

    public async Task<ReporterRegistrationPersistenceResult> ResendReporterOtpAsync(
        Guid intentId,
        string otpHash,
        DateTimeOffset expiresAt,
        DateTimeOffset resendAvailableAt,
        int maxResendsPerWindow,
        TimeSpan resendWindow,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var replay = await FindRecordAsync(null, ResendOperation, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return await MapReporterReplayAsync(replay, requestFingerprint, cancellationToken);
        }

        var intent = await _context.ReporterRegistrationIntents.SingleOrDefaultAsync(
            item => item.Id == intentId,
            cancellationToken);
        if (intent is null || intent.ConsumedAt is not null || intent.ExpiresAt <= now)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.NotFound);
        }

        if (intent.ResendAvailableAt > now)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.TooManyRequests, intent);
        }

        var resendWindowStart = now.Subtract(resendWindow);
        var recentResendCount = await _context.IdempotencyRecords.AsNoTracking().CountAsync(
            item => item.Operation == ResendOperation &&
                    item.RequestFingerprint == requestFingerprint &&
                    item.CreatedAtUtc >= resendWindowStart,
            cancellationToken);
        if (recentResendCount >= maxResendsPerWindow)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.TooManyRequests, intent);
        }

        intent.OtpGeneration++;
        intent.OtpHash = otpHash;
        intent.FailedAttempts = 0;
        intent.ExpiresAt = expiresAt;
        intent.ResendAvailableAt = resendAvailableAt;
        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
            null,
            null,
            ResendOperation,
            idempotencyKey,
            requestFingerprint,
            operationId,
            JsonSerializer.Serialize(new { intentId }),
            now));
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.TooManyRequests);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindRecordAsync(null, ResendOperation, idempotencyKey, CancellationToken.None);
            return winner is null
                ? new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.Conflict)
                : await MapReporterReplayAsync(winner, requestFingerprint, CancellationToken.None);
        }

        return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.Success, intent);
    }

    public async Task<ReporterRegistrationPersistenceResult> VerifyReporterOtpAsync(
        Guid intentId,
        string otpHash,
        int maxAttempts,
        UserSession session,
        RefreshToken refreshToken,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var replay = await FindRecordAsync(null, VerifyOperation, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return await MapActivationReplayAsync(replay, requestFingerprint, cancellationToken);
        }

        var intent = await _context.ReporterRegistrationIntents.SingleOrDefaultAsync(
            item => item.Id == intentId,
            cancellationToken);
        if (intent is null || intent.UserId is null || intent.ConsumedAt is not null || intent.ExpiresAt <= now)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.NotFound);
        }

        if (intent.FailedAttempts >= maxAttempts)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.TooManyRequests, intent);
        }

        if (!string.Equals(intent.OtpHash, otpHash, StringComparison.Ordinal))
        {
            intent.FailedAttempts++;
            await _context.SaveChangesAsync(cancellationToken);
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.InvalidInput, intent);
        }

        var user = await _context.Users.SingleAsync(item => item.Id == intent.UserId.Value, cancellationToken);
        user.Status = UserStatus.Active;
        user.EmailConfirmed = true;
        user.LastLoginAt = now;
        intent.ConsumedAt = now;
        intent.EmailConfirmedAt = now;
        _context.Sessions.Add(session);
        _context.RefreshTokens.Add(refreshToken);
        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
            null,
            null,
            VerifyOperation,
            idempotencyKey,
            requestFingerprint,
            operationId,
            JsonSerializer.Serialize(new { intentId, userId = user.Id, sessionId = session.Id }),
            now));
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindRecordAsync(null, VerifyOperation, idempotencyKey, CancellationToken.None);
            return winner is null
                ? new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.Conflict)
                : await MapActivationReplayAsync(winner, requestFingerprint, CancellationToken.None);
        }

        return new ReporterRegistrationPersistenceResult(
            IdentityOnboardingPersistenceStatus.Success,
            intent,
            ToSecurityState(user),
            session.Id);
    }

    public async Task<InvitationPersistenceResult> CreateInvitationAsync(
        StaffInvitation invitation,
        IReadOnlyCollection<Guid> projectIds,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var replay = await FindRecordAsync(invitation.CreatedByUserId, CreateInvitationOperation, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return await MapInvitationReplayAsync(replay, requestFingerprint, cancellationToken);
        }

        if (projectIds.Count > 0 && await _context.Projects.CountAsync(
                item => projectIds.Contains(item.Id),
                cancellationToken) != projectIds.Count)
        {
            return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.NotFound);
        }

        _context.StaffInvitations.Add(invitation);
        _context.StaffInvitationProjects.AddRange(projectIds.Select(projectId => new StaffInvitationProject
        {
            InvitationId = invitation.Id,
            ProjectId = projectId
        }));
        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
            invitation.CreatedByUserId,
            null,
            CreateInvitationOperation,
            idempotencyKey,
            requestFingerprint,
            operationId,
            JsonSerializer.Serialize(new { invitationId = invitation.Id }),
            invitation.CreatedAt));
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindRecordAsync(
                invitation.CreatedByUserId,
                CreateInvitationOperation,
                idempotencyKey,
                CancellationToken.None);
            return winner is null
                ? new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.Conflict)
                : await MapInvitationReplayAsync(winner, requestFingerprint, CancellationToken.None);
        }

        return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.Success, invitation, DeliveryToken: null);
    }

    public async Task<InvitationPersistenceResult> AcceptInvitationAsync(
        string tokenHash,
        ApplicationUser user,
        UserSession session,
        RefreshToken refreshToken,
        string idempotencyKey,
        string requestFingerprint,
        Guid operationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var replay = await FindRecordAsync(null, AcceptInvitationOperation, idempotencyKey, cancellationToken);
        if (replay is not null)
        {
            return await MapInvitationActivationReplayAsync(replay, requestFingerprint, cancellationToken);
        }

        var invitation = await _context.StaffInvitations.SingleOrDefaultAsync(
            item => item.TokenHash == tokenHash,
            cancellationToken);
        if (invitation is null || invitation.RevokedAt is not null || invitation.ExpiresAt <= now)
        {
            return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.NotFound);
        }

        if (invitation.AcceptedAt is not null || await _context.Users.AnyAsync(
                item => item.NormalizedEmail == invitation.NormalizedEmail,
                cancellationToken))
        {
            return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.Conflict);
        }

        user.Email = invitation.Email;
        user.NormalizedEmail = invitation.NormalizedEmail;
        user.UserName = invitation.Email;
        user.NormalizedUserName = invitation.NormalizedEmail;
        user.RoleCode = invitation.RoleCode;
        invitation.AcceptedAt = now;
        _context.Users.Add(user);
        _context.Sessions.Add(session);
        _context.RefreshTokens.Add(refreshToken);
        var projectIds = await _context.StaffInvitationProjects
            .Where(item => item.InvitationId == invitation.Id)
            .Select(item => item.ProjectId)
            .ToListAsync(cancellationToken);
        _context.ProjectMembers.AddRange(projectIds.Select(projectId => new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = user.Id,
            RoleCode = user.RoleCode,
            IsPrimary = false,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime),
            Status = ProjectMemberStatus.Active
        }));
        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
            null,
            null,
            AcceptInvitationOperation,
            idempotencyKey,
            requestFingerprint,
            operationId,
            JsonSerializer.Serialize(new { invitationId = invitation.Id, userId = user.Id, sessionId = session.Id }),
            now));
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            var winner = await FindRecordAsync(null, AcceptInvitationOperation, idempotencyKey, CancellationToken.None);
            return winner is null
                ? new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.Conflict)
                : await MapInvitationActivationReplayAsync(winner, requestFingerprint, CancellationToken.None);
        }

        return new InvitationPersistenceResult(
            IdentityOnboardingPersistenceStatus.Success,
            invitation,
            ToSecurityState(user),
            session.Id);
    }

    private Task<IdempotencyRecord?> FindRecordAsync(
        Guid? actorUserId,
        string operation,
        string key,
        CancellationToken cancellationToken) =>
        _context.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(item =>
            item.ActorUserId == actorUserId && item.ProjectId == null &&
            item.Operation == operation && item.IdempotencyKey == key,
            cancellationToken);

    private async Task<ReporterRegistrationPersistenceResult> MapReporterReplayAsync(
        IdempotencyRecord record,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (record.RequestFingerprint != fingerprint)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.IdempotentConflict);
        }

        var id = JsonDocument.Parse(record.OutcomeJson).RootElement.GetProperty("intentId").GetGuid();
        var intent = await _context.ReporterRegistrationIntents.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.IdempotentReplay, intent);
    }

    private async Task<ReporterRegistrationPersistenceResult> MapActivationReplayAsync(
        IdempotencyRecord record,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (record.RequestFingerprint != fingerprint)
        {
            return new ReporterRegistrationPersistenceResult(IdentityOnboardingPersistenceStatus.IdempotentConflict);
        }

        using var outcome = JsonDocument.Parse(record.OutcomeJson);
        var root = outcome.RootElement;
        var intentId = root.GetProperty("intentId").GetGuid();
        var userId = root.GetProperty("userId").GetGuid();
        var intent = await _context.ReporterRegistrationIntents.AsNoTracking().SingleAsync(item => item.Id == intentId, cancellationToken);
        var user = await GetUserStateAsync(userId, cancellationToken);
        return new ReporterRegistrationPersistenceResult(
            IdentityOnboardingPersistenceStatus.IdempotentReplay,
            intent,
            user,
            root.GetProperty("sessionId").GetGuid());
    }

    private async Task<InvitationPersistenceResult> MapInvitationReplayAsync(
        IdempotencyRecord record,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (record.RequestFingerprint != fingerprint)
        {
            return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.IdempotentConflict);
        }

        var id = JsonDocument.Parse(record.OutcomeJson).RootElement.GetProperty("invitationId").GetGuid();
        var invitation = await _context.StaffInvitations.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.IdempotentReplay, invitation);
    }

    private async Task<InvitationPersistenceResult> MapInvitationActivationReplayAsync(
        IdempotencyRecord record,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (record.RequestFingerprint != fingerprint)
        {
            return new InvitationPersistenceResult(IdentityOnboardingPersistenceStatus.IdempotentConflict);
        }

        using var outcome = JsonDocument.Parse(record.OutcomeJson);
        var root = outcome.RootElement;
        var invitation = await _context.StaffInvitations.AsNoTracking()
            .SingleAsync(item => item.Id == root.GetProperty("invitationId").GetGuid(), cancellationToken);
        var user = await GetUserStateAsync(root.GetProperty("userId").GetGuid(), cancellationToken);
        return new InvitationPersistenceResult(
            IdentityOnboardingPersistenceStatus.IdempotentReplay,
            invitation,
            user,
            root.GetProperty("sessionId").GetGuid());
    }

    private Task<UserSecurityState?> GetUserStateAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new UserSecurityState(
                item.Id,
                item.UserName!,
                item.DisplayName,
                item.RoleCode,
                item.Status,
                item.MustChangePassword,
                item.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

    private static UserSecurityState ToSecurityState(ApplicationUser user) => new(
        user.Id,
        user.UserName!,
        user.DisplayName,
        user.RoleCode,
        user.Status,
        user.MustChangePassword,
        user.RowVersion);
}
