using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Models;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Implementations.Files;

public sealed partial class UploadPersistenceService
{
    private UploadSessionOptions RecoveryOptions => _recoveryOptions ?? new();
    private readonly UploadSessionOptions? _recoveryOptions;

    public UploadPersistenceService(RoadGuardDbContext context,
        RoadGuardSystem.Repositories.Idempotency.IdempotencyOperationService idempotency,
        IUploadObjectStorage storage, IOptions<UploadSessionOptions> options) : this(context, idempotency, storage)
        => _recoveryOptions = options.Value;

    private async Task GuardMultipartAsync(Guid actor, Guid? fileId, CancellationToken token)
    {
        if (fileId is not { } id) throw new UploadNotFoundException();
        var scope = await _context.FileScopes.AsNoTracking().SingleOrDefaultAsync(s => s.FileId == id, token)
            ?? throw new UploadNotFoundException();
        if (scope.ProjectId is not { } project) { await GuardPrivateAsync(actor, fileId, token); return; }
        await MultipartReceiptAuthority.LockAsync(_context, actor, project, token);
        var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actor, token);
        if (user is null || user.RoleCode is not (UserRoleCode.Supervisor or UserRoleCode.ProjectManager or UserRoleCode.DroneOperator or UserRoleCode.RepairCrew)
            || !await new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(_context).IsCurrentActorAsync(actor, user.RoleCode, token))
            throw new UploadNotFoundException();
        if (user.RoleCode != UserRoleCode.Supervisor)
        {
            var membership = await new RoadGuardSystem.Repositories.Projects.ProjectMembershipReadModel(_context).FindByUserAndProjectAsync(actor, project, token);
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            if (membership is null || membership.Status != ProjectMemberStatus.Active || membership.RoleCode != user.RoleCode || membership.ValidFrom > today || membership.ValidTo < today)
                throw new UploadNotFoundException();
        }
        if (scope.Purpose is "SURVEY_VIDEO" or "TELEMETRY")
        {
            if (user.RoleCode != UserRoleCode.DroneOperator || scope.TargetId is not { } task) throw new UploadNotFoundException();
            await _context.SurveyRequests.FromSqlInterpolated($"SELECT * FROM [SurveyRequests] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={task}").AsNoTracking().ToListAsync(token);
            await _context.SurveyAssignments.FromSqlInterpolated($"SELECT * FROM [SurveyAssignments] WITH (UPDLOCK,HOLDLOCK) WHERE [SurveyRequestId]={task}").AsNoTracking().ToListAsync(token);
            if (!await IsCurrentSurveyOperatorAsync(actor, project, task, true, token)) throw new UploadNotFoundException();
        }
        await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={id}").AsNoTracking().ToListAsync(token);
    }

    private int RetrySeconds => Math.Clamp(RecoveryOptions.RecoveryRetrySeconds, 1, 300);

    // Clear tracking after a failed/acknowledgement-lost transaction before consulting durable state.
    private Task<T> MultipartTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token)
        => _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var tx = await _context.Database.BeginTransactionAsync(token);
            var result = await operation(token);
            await _context.SaveChangesAsync(token);
            await tx.CommitAsync(token);
            return result;
        });

    private async Task EnsureMultipartAsync(Guid actor, Guid? projectId, Guid upload, string key, DateTimeOffset now, CancellationToken token)
    {
        var fence = Guid.NewGuid();
        var decision = await MultipartTransactionAsync(async ct =>
        {
            var fileId = await _context.UploadSessions.Where(s => s.Id == upload).Select(s => s.FileId).SingleAsync(ct);
            await GuardMultipartAsync(actor, fileId, ct);
            if (await _context.IdempotencyRecords.AnyAsync(r => r.ActorUserId == actor && r.ProjectId == projectId
                && r.Operation == "UploadPartUrlsIssued" && r.IdempotencyKey == key, ct)) return (Initiate: false, Receipt: true);
            var s = await _context.UploadSessions.SingleAsync(s => s.Id == upload, ct);
            if (s.StorageUploadId is not null) return (Initiate: false, Receipt: false);
            if (s.Status != UploadSessionStatus.Pending || now >= s.ExpiresAt) throw new UploadPartConflictException();
            // A pre-existing legacy marker has no new attempt metadata; it must never initiate again.
            if (s.MultipartPhase is null && s.FailureCode is null)
                s.BeginMultipart(fence, now.AddSeconds(Math.Clamp(RecoveryOptions.RecoveryDeadlineSeconds, 1, 3600)), now);
            return (Initiate: s.MultipartFence == fence && s.MultipartPhase == "CLAIMED", Receipt: false);
        }, token);
        if (decision.Receipt) return;
        if (decision.Initiate)
        {
            // Commit CALLING separately: CLAIMED is safely recoverable without another external create.
            // A lost commit ack recognizes this invocation's fence; no remote call in the retry delegate.
            var work = await MultipartTransactionAsync(async ct =>
            {
                var fileId = await _context.UploadSessions.AsNoTracking().Where(s => s.Id == upload).Select(s => s.FileId).SingleAsync(ct);
                await GuardMultipartAsync(actor, fileId, ct);
                var s = await _context.UploadSessions.SingleAsync(s => s.Id == upload, ct);
                if (s.MultipartFence != fence || s.Status != UploadSessionStatus.Pending || now >= s.ExpiresAt) return null;
                s.SetMultipartPhase(fence, "CALLING", now.AddSeconds(RetrySeconds));
                return s;
            }, token);
            if (work is not null)
            {
                var id = await _storage.InitiateAsync(work.ObjectKey, work.MediaType, token);
                var accepted = await AcceptMultipartAsync(actor, upload, fence, "CALLING", id, now, token);
                if (!accepted) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart worker fenced by recovery.");
                return;
            }
        }
        await ReconcileMultipartAsync(upload, actor, now, token);
        var current = await _context.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload, token);
        if (current.Status == UploadSessionStatus.Failed || now >= current.ExpiresAt) throw new UploadPartConflictException();
        if (current.StorageUploadId is null) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart recovery pending.");
    }

    private Task<bool> AcceptMultipartAsync(Guid actor, Guid upload, Guid fence, string phase, string id, DateTimeOffset now, CancellationToken token)
        => MultipartTransactionAsync(async ct =>
        {
            var fileId = await _context.UploadSessions.AsNoTracking().Where(s => s.Id == upload).Select(s => s.FileId).SingleAsync(ct);
            await GuardMultipartAsync(actor, fileId, ct);
            var s = await _context.UploadSessions.SingleAsync(s => s.Id == upload, ct);
            var checkedNow = now > DateTimeOffset.UtcNow ? now : DateTimeOffset.UtcNow;
            if (s.MultipartFence != fence || s.MultipartPhase != phase || s.Status != UploadSessionStatus.Pending || checkedNow >= s.ExpiresAt)
                return s.MultipartFence == fence && s.StorageUploadId == id && s.Status == UploadSessionStatus.Uploading && checkedNow < s.ExpiresAt;
            s.StartUploading(id, now);
            s.SetMultipartPhase(fence, "DURABLE", now.AddSeconds(RetrySeconds));
            _context.Set<UploadMultipartSweep>().Add(new() { UploadSessionId = s.Id, NextCheckAt = now });
            return true;
        }, token);

    public async Task RecoverMultipartsAsync(CancellationToken cancellationToken = default)
    {
        var token = cancellationToken;
        var now = DateTimeOffset.UtcNow;
        var uploads = await (from s in _context.UploadSessions.AsNoTracking()
            join scope in _context.FileScopes.AsNoTracking() on s.FileId equals scope.FileId
            where (s.MultipartPhase == "TERMINAL" || s.Status == UploadSessionStatus.Pending
                && (s.MultipartPhase != null || (s.FailureCode != null && s.FailureCode.StartsWith("multipart_initiating:"))))
                && (s.MultipartNextCheckAt == null || s.MultipartNextCheckAt <= now)
            orderby s.MultipartNextCheckAt, s.Id
            select s.Id).Take(Math.Clamp(RecoveryOptions.RecoveryBatchSize, 1, 100)).ToArrayAsync(token);
        foreach (var upload in uploads)
        {
            try { await ReconcileMultipartAsync(upload, null, now, token); }
            catch (FileStorageException) { /* durable next-check remains due after bounded delay */ }
            catch (UploadNotFoundException) { /* no protected result is returned by this worker */ }
        }
        var durable = await _context.Set<UploadMultipartSweep>().AsNoTracking().Where(s => s.NextCheckAt <= now)
            .OrderBy(s => s.NextCheckAt).ThenBy(s => s.UploadSessionId)
            .Take(Math.Clamp(RecoveryOptions.RecoveryBatchSize, 1, 100)).Select(s => s.UploadSessionId).ToArrayAsync(token);
        foreach (var upload in durable)
        {
            try { await SweepDurableMultipartAsync(upload, now, token); }
            catch (FileStorageException) { /* schedule retains retry, no protected result */ }
        }
    }

    private async Task SweepDurableMultipartAsync(Guid upload, DateTimeOffset now, CancellationToken token)
    {
        if (_storage is not IMultipartRecoveryStorage storage) return;
        var work = await MultipartTransactionAsync(async ct =>
        {
            var sweep = await _context.Set<UploadMultipartSweep>().FromSqlInterpolated($"SELECT * FROM [UploadMultipartSweeps] WITH (UPDLOCK,HOLDLOCK) WHERE [UploadSessionId]={upload}").SingleOrDefaultAsync(ct);
            if (sweep is null || sweep.NextCheckAt > now) return null;
            sweep.NextCheckAt = now.AddSeconds(RetrySeconds);
            var current = await (from s in _context.UploadSessions.AsNoTracking()
                join f in _context.Files.AsNoTracking() on s.FileId equals f.Id
                join scope in _context.FileScopes.AsNoTracking() on s.FileId equals scope.FileId
                where s.Id == upload && f.StorageUri == s.ObjectKey
                    && scope.OwnerUserId == s.OwnerUserId && s.MultipartPhase == "DURABLE" && s.StorageUploadId != null
                select s).SingleOrDefaultAsync(ct);
            if (current is not null && current.Status == UploadSessionStatus.Uploading && now >= current.ExpiresAt)
            {
                var terminal = await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={upload}").SingleAsync(ct);
                if (terminal.Status == UploadSessionStatus.Uploading && now >= terminal.ExpiresAt)
                {
                    terminal.StopMultipart(now.AddSeconds(RetrySeconds));
                    _context.Set<UploadMultipartSweep>().Remove(sweep);
                    return terminal;
                }
                return null;
            }
            return current;
        }, token);
        if (work is null || work.ObjectKey != $"uploads/{work.FileId:N}") return;
        foreach (var id in await storage.ListMultipartIdsAsync(work.ObjectKey, token))
            if (work.MultipartPhase == "TERMINAL" || id != work.StorageUploadId) await storage.AbortMultipartAsync(work.ObjectKey, id, token);
    }

    private async Task ReconcileMultipartAsync(Guid upload, Guid? caller, DateTimeOffset now, CancellationToken token)
    {
        if (_storage is not IMultipartRecoveryStorage recovery) return;
        var fence = Guid.NewGuid();
        var work = await MultipartTransactionAsync(async ct =>
        {
            var fileId = await _context.UploadSessions.AsNoTracking().Where(s => s.Id == upload).Select(s => s.FileId).SingleAsync(ct);
            if (caller is { } actor) await GuardMultipartAsync(actor, fileId, ct);
            var s = await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={upload}").SingleAsync(ct);
            // Proven ownership is an immutable session + file + scope exact-key join.
            var owned = await (from file in _context.Files.AsNoTracking()
                join scope in _context.FileScopes.AsNoTracking() on file.Id equals scope.FileId
                where file.Id == s.FileId && file.StorageUri == s.ObjectKey && scope.OwnerUserId == s.OwnerUserId
                select file.Id).AnyAsync(ct);
            if (!owned) return null;
            if (s.MultipartNextCheckAt > now && s.MultipartFence != fence) return null;
            if (s.MultipartPhase is null)
            {
                if (s.FailureCode?.StartsWith("multipart_initiating:", StringComparison.Ordinal) != true) return null;
                s.BeginMultipart(fence, now.AddSeconds(Math.Clamp(RecoveryOptions.RecoveryDeadlineSeconds, 1, 3600)), now);
                // Old SDK calls may have retried without an attempt identity. Do not adopt a guess.
                s.StopMultipart(now.AddSeconds(RetrySeconds));
            }
            if (s.Status == UploadSessionStatus.Uploading || s.Status == UploadSessionStatus.Verifying || s.Status == UploadSessionStatus.Verified)
                return null; // Durable binding is immutable; worker polling must not drift its public ETag.
            if (s.Status is UploadSessionStatus.Failed || now >= s.ExpiresAt)
                s.StopMultipart(now.AddSeconds(RetrySeconds));
            else if (s.Status == UploadSessionStatus.Pending)
                s.SetMultipartPhase(fence, "RECONCILING", now.AddSeconds(RetrySeconds));
            else s.SetMultipartPhase(s.MultipartFence!.Value, s.MultipartPhase!, now.AddSeconds(RetrySeconds));
            return s;
        }, token);
        if (work is null || work.ObjectKey != $"uploads/{work.FileId:N}") return;
        // No SQL transaction/locks survive this point. The fence is rechecked after remote reads.
        var candidates = await recovery.ListMultipartIdsAsync(work.ObjectKey, token);
        if (work.Status != UploadSessionStatus.Pending)
        {
            foreach (var id in candidates)
                if (work.MultipartPhase == "TERMINAL" || id != work.StorageUploadId)
                    await recovery.AbortMultipartAsync(work.ObjectKey, id, token);
            return;
        }
        if (candidates.Count == 1 && !await recovery.HasPartsAsync(work.ObjectKey, candidates[0], token))
        {
            try { await AcceptMultipartAsync(caller ?? work.OwnerUserId, upload, fence, "RECONCILING", candidates[0], now, token); }
            catch (UploadNotFoundException) when (caller is null) { /* revoke during worker recovery leaves no issued receipt */ }
            return;
        }
        if (candidates.Count == 0 && now < work.MultipartDeadline) return;
        var stopped = await MultipartTransactionAsync(async ct =>
        {
            if (caller is { } actor) await GuardMultipartAsync(actor, work.FileId, ct);
            var s = await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={upload}").SingleAsync(ct);
            if (s.MultipartFence != fence || s.MultipartPhase != "RECONCILING" || s.Status != UploadSessionStatus.Pending) return false;
            s.StopMultipart(now.AddSeconds(RetrySeconds));
            return true;
        }, token);
        if (stopped)
            foreach (var id in candidates) await recovery.AbortMultipartAsync(work.ObjectKey, id, token);
    }
}
