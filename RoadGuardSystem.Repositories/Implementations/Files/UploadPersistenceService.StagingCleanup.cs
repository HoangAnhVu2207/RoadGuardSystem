using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Repositories.Models;

namespace RoadGuardSystem.Repositories.Implementations.Files;

public sealed partial class UploadPersistenceService
{
    private async Task CleanupExpiredStagingAsync(DateTimeOffset now, CancellationToken token)
    {
        if (_storage is not IStagingCleanupStorage storage) return;
        var due = await (from session in _context.UploadSessions.AsNoTracking()
                         join scope in _context.FileScopes.AsNoTracking() on session.FileId equals scope.FileId
                         where scope.ProjectId == null && scope.TargetId == null &&
                             scope.Purpose == FileScope.ProjectHandoverStaging && session.ExpiresAt <= now &&
                             session.FailureCode != "staging_expired_cleaned" &&
                             (session.MultipartNextCheckAt == null || session.MultipartNextCheckAt <= now)
                         orderby session.ExpiresAt, session.Id
                         select new { session.Id, session.FileId, session.OwnerUserId })
            .Take(Math.Clamp(RecoveryOptions.RecoveryBatchSize, 1, 100)).ToArrayAsync(token);

        foreach (var entry in due)
        {
            var work = await MultipartTransactionAsync(async ct =>
            {
                // Same authority/file/scope/session order as finalize. A durable failed state fences
                // finalize and verifier before external cleanup starts.
                await RoadGuardSystem.Repositories.Idempotency.MultipartReceiptAuthority.LockAsync(
                    _context, entry.OwnerUserId, null, ct);
                var file = await _context.Files.FromSqlInterpolated(
                    $"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={entry.FileId}")
                    .AsNoTracking().SingleOrDefaultAsync(ct);
                var scope = await _context.FileScopes.FromSqlInterpolated(
                    $"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={entry.FileId}")
                    .AsNoTracking().SingleOrDefaultAsync(ct);
                var session = await _context.UploadSessions.FromSqlInterpolated(
                    $"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={entry.Id}")
                    .SingleOrDefaultAsync(ct);
                if (scope is not { ProjectId: null, TargetId: null, Purpose: FileScope.ProjectHandoverStaging } ||
                    session is null || session.Purpose != "DOCUMENT" || file is null || scope.OwnerUserId != entry.OwnerUserId ||
                    session.OwnerUserId != entry.OwnerUserId || session.ObjectKey != file.StorageUri ||
                    session.ObjectKey != $"uploads/{session.FileId:N}" || session.ExpiresAt > now ||
                    session.FailureCode == "staging_expired_cleaned" || session.MultipartNextCheckAt > now ||
                    await _context.HandoverDocuments.AnyAsync(row => row.FileId == entry.FileId, ct))
                    return null;
                if (session.FailureCode != "staging_expired")
                    _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), null, now, "project_handover_staging_expired",
                        "UploadSession", session.Id, null, null, "Unused staging expired before project creation",
                        "staging.cleanup.v1", session.Id));
                session.ExpireStaging(now.AddSeconds(RetrySeconds));
                var sweep = await _context.Set<UploadMultipartSweep>().SingleOrDefaultAsync(row => row.UploadSessionId == session.Id, ct);
                if (sweep is not null) _context.Remove(sweep);
                return session;
            }, token);
            if (work is null) continue;
            try
            {
                foreach (var id in await storage.ListMultipartIdsAsync(work.ObjectKey, token))
                    await storage.AbortMultipartAsync(work.ObjectKey, id, token);
                await storage.DeleteStagedObjectAsync(work.ObjectKey, token);
                // Signed URLs expire with the upload. Delay final cleanup acknowledgment to let
                // already-running multipart initiation and verification calls finish, then sweep again.
                if (now < work.ExpiresAt.AddHours(1)) continue;
                await MultipartTransactionAsync(async ct =>
                {
                    var current = await _context.UploadSessions.FromSqlInterpolated(
                        $"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={work.Id}").SingleAsync(ct);
                    if (current.FailureCode == "staging_expired")
                    {
                        current.CompleteStagingCleanup();
                        _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), null, now, "project_handover_staging_cleaned",
                            "UploadSession", current.Id, null, null, "Unused object and multipart uploads removed",
                            "staging.cleanup.v1", current.Id));
                    }
                    return true;
                }, token);
            }
            catch (FileStorageException) { /* The durable next-check retries abort and deletion together. */ }
        }
    }
}
