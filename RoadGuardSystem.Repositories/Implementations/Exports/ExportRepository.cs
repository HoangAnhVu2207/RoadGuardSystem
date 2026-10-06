using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Exports;

public sealed partial class ExportRepository : IExportRepository
{
    private readonly RoadGuardDbContext _db;
    private readonly IdempotencyOperationService _idempotency;
    private readonly TimeProvider _clock;
    private readonly ExportRepositoryOptions _options;
    public ExportRepository(RoadGuardDbContext db, IdempotencyOperationService idempotency, TimeProvider clock, IOptions<ExportRepositoryOptions>? options = null)
    {
        _db = db; _idempotency = idempotency; _clock = clock; _options = options?.Value ?? new();
        if (_options.LeaseDurationSeconds < 60 || _options.RetryBackoffSeconds < 1) throw new ArgumentException("Invalid export lease or backoff configuration.");
    }
    public async Task<ExportAdmissionResult> AdmitAsync(Guid actorId, Guid projectId, CreateExportRequestDto request, string key, string fingerprint, Guid? correlationId, Func<Guid, DateTimeOffset, CancellationToken, Task<ExportCaptureResult>> capture, CancellationToken ct)
    {
        try
        {
            var outcome = await _idempotency.ExecuteSerializableAsync(actorId, projectId, "Anh02.Export.Create", key, fingerprint, async token =>
            {
                // This runs inside the existing idempotency transaction. Hold selected heads/ranges until receipt commit.
                var now = _clock.GetUtcNow(); var snapshotId = Guid.NewGuid();
                var facts = await capture(snapshotId, now, token);
                if (facts.ErrorCode is not null || facts.Payload is null) throw new CaptureException(facts.ErrorCode ?? "producer_unavailable");
                var canonical = JsonSerializer.Serialize(facts.Payload, ExportSerialization.Options);
                var hash = ExportSerialization.Hash(canonical);
                var snapshot = ExportSnapshot.Create(snapshotId, projectId, canonical, hash, now);
                var job = ExportJob.Create(Guid.NewGuid(), projectId, actorId, snapshotId, request.Kind, request.Format, now);
                _db.Set<ExportSnapshot>().Add(snapshot); _db.Set<ExportJob>().Add(job);
                foreach (var f in facts.Payload.Manifest.Files) _db.Set<ExportSnapshotFile>().Add(ExportSnapshotFile.Create(snapshotId, f.FileId, f.FileVersion, f.Sha256, f.SizeBytes, f.MediaType, f.Included, f.ArchivePath));
                _db.Set<AuditLog>().Add(AuditLog.Create(Guid.NewGuid(), actorId, now, "Anh02.Export.Admitted", "ExportJob", job.Id, null, null, "Immutable snapshot admitted", "ANH02_EXPORT", correlationId));
                return (job.Id, JsonSerializer.Serialize(new { id = job.Id }));
            }, ct, receiptAccessGuard: async token =>
            {
                await Anh02ReceiptAuthority.LockAsync(_db, actorId, projectId, token);
                var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actorId, token);
                if (user is null || user.Status != UserStatus.Active || user.MustChangePassword || user.RoleCode is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)
                    || !await _db.Roles.AsNoTracking().AnyAsync(r => r.Code == user.RoleCode && r.IsActive, token))
                    throw new CaptureException("forbidden");
                if (user.RoleCode == UserRoleCode.ProjectManager)
                {
                    var member = await new RoadGuardSystem.Repositories.Projects.ProjectMembershipReadModel(_db).FindByUserAndProjectAsync(actorId, projectId, token);
                    var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
                    if (member is null || member.RoleCode != user.RoleCode || member.Status != ProjectMemberStatus.Active
                        || member.ValidFrom > today || member.ValidTo < today) throw new CaptureException("forbidden");
                }
                // The canonical stored export, including conflict/recovery paths, owns source authority.
                var originalId = await _db.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().AsNoTracking()
                    .Where(record => record.ActorUserId == actorId && record.ProjectId == projectId &&
                        record.Operation == "Anh02.Export.Create" && record.IdempotencyKey == key)
                    .Select(record => (Guid?)record.OperationId).SingleOrDefaultAsync(token);
                if (originalId is Guid storedId)
                {
                    var original = await GetAsync(projectId, storedId, token);
                    if (original is null) throw new CaptureException("forbidden");
                    var payload = ExportSerialization.Read(original.Snapshot);
                    if (payload.Manifest.ProjectId != projectId) throw new CaptureException("forbidden");
                    var caseFacts = payload.Dossier?.CaseDefectFacts;
                    var sourceQuery = new ExportSourceAuthorityQuery(payload.Manifest.ProjectId,
                        payload.Manifest.SourceRevisions.Where(source => source.Kind.StartsWith("Repair", StringComparison.Ordinal))
                            .Select(source => new ExportSourceAuthorityReference(source.Kind, source.Id)).ToArray(),
                        payload.Dossier?.Summary.Metrics.Any(metric => metric.Code == "repairItemsByStatus" && metric.Availability == "AVAILABLE") == true,
                        caseFacts is null ? null : new(caseFacts.ProjectId, caseFacts.Cases.Select(value => value.CaseId).ToArray(),
                            caseFacts.Defects.Select(value => value.DefectId).ToArray()));
                    if (!await CanReadSnapshotSourcesAsync(actorId, projectId, sourceQuery, token)) throw new CaptureException("forbidden");
                }
            });
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new("idempotency_conflict", null);
            return new(null, await GetAsync(projectId, outcome.OperationId, ct));
        }
        catch (CaptureException ex) { return new(ex.Code, null); }
    }
    public async Task<ExportPersistenceView?> GetAsync(Guid projectId, Guid id, CancellationToken ct)
    {
        var job = await _db.Set<ExportJob>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);
        if (job is null) return null;
        var snapshot = await _db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(x => x.Id == job.SnapshotId, ct);
        StoredFile? file = null;
        if (job.ArtifactId.HasValue) file = await (from a in _db.Set<GeneratedArtifact>() join f in _db.Set<StoredFile>() on a.FileId equals f.Id where a.Id == job.ArtifactId select f).AsNoTracking().SingleOrDefaultAsync(ct);
        return new(job, snapshot, file);
    }
    public async Task<ExportClaim?> ClaimAsync(CancellationToken ct)
    {
        _db.ChangeTracker.Clear(); var now = _clock.GetUtcNow();
        var job = await _db.Set<ExportJob>().Where(x => (x.Status == "QUEUED" || x.Status == "RUNNING") && (x.LeaseUntil == null || x.LeaseUntil <= now) && (x.NextAttemptAt == null || x.NextAttemptAt <= now)).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (job is null) return null;
        var lease = Guid.NewGuid(); if (!job.TryClaim(lease, now, TimeSpan.FromSeconds(_options.LeaseDurationSeconds))) return null;
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { _db.ChangeTracker.Clear(); return null; }
        var snapshot = await _db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(x => x.Id == job.SnapshotId, ct);
        return new(job.Id, lease, job.RequestedBy, job.ProjectId, snapshot);
    }
    public async Task<bool> RenewAsync(Guid id, Guid token, CancellationToken ct)
    {
        // Conditional SQL update also advances rowversion; no tracked job can overwrite another claimant.
        var now = _clock.GetUtcNow();
        return await _db.Set<ExportJob>().Where(x => x.Id == id && x.LeaseToken == token && x.Status == "RUNNING" && x.LeaseUntil > now).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseUntil, now + TimeSpan.FromSeconds(_options.LeaseDurationSeconds)), ct) == 1;
    }
    public async Task<bool> CompleteAsync(ExportClaim claim, Anh02ArtifactMetadata metadata, Func<CancellationToken, Task<bool>> authorize, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();
            await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var job = await _db.Set<ExportJob>().SingleAsync(x => x.Id == claim.Id, ct); var now = _clock.GetUtcNow();
            if (!job.OwnsLease(claim.Token, now) || !await authorize(ct)) return false;
            var file = StoredFile.Create(Guid.NewGuid(), metadata.Key, $"export-{job.Id:D}.{job.Format.ToLowerInvariant()}", metadata.MediaType, metadata.SizeBytes, metadata.Sha256, null, now, null);
            var artifact = GeneratedArtifact.Create(Guid.NewGuid(), job.Id, job.SnapshotId, file.Id, now);
            _db.Set<StoredFile>().Add(file); _db.Set<GeneratedArtifact>().Add(artifact);
            job.Complete(claim.Token, artifact.Id, now);
            _db.Set<AuditLog>().Add(AuditLog.Create(Guid.NewGuid(), job.RequestedBy, now, "Anh02.Export.Completed", "ExportJob", job.Id, null, null, null, "ANH02_EXPORT", null));
            await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
        });
    }
    public async Task FailAsync(ExportClaim claim, string code, bool permanent, CancellationToken ct)
    {
        _db.ChangeTracker.Clear(); var job = await _db.Set<ExportJob>().SingleAsync(x => x.Id == claim.Id, ct); var now = _clock.GetUtcNow();
        if (!job.OwnsLease(claim.Token, now)) return;
        job.Fail(claim.Token, now, code, permanent, TimeSpan.FromSeconds(_options.RetryBackoffSeconds));
        try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { _db.ChangeTracker.Clear(); }
    }
    public Task<StoredFile?> GetSourceFileAsync(Guid fileId, CancellationToken ct) => _db.Set<StoredFile>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, ct);
    public async Task<bool> CanReadSurveySourcesAsync(Guid projectId, Guid[] fileIds, CancellationToken ct)
    {
        if (fileIds.Length == 0) return true;
        var ids = fileIds.Distinct().ToArray();
        var count = await _db.Set<StoredFile>().CountAsync(f => ids.Contains(f.Id) &&
            _db.Set<FileScope>().Any(s => s.FileId == f.Id && s.ProjectId == projectId && (s.Purpose == "SURVEY_VIDEO" || s.Purpose == "TELEMETRY")) &&
            _db.Set<UploadSession>().Any(u => u.FileId == f.Id && u.Status == UploadSessionStatus.Verified && (u.Purpose == "SURVEY_VIDEO" || u.Purpose == "TELEMETRY")), ct);
        return count == ids.Length;
    }
    private sealed class CaptureException(string code) : Exception { public string Code { get; } = code; }
}

public static class ExportSerialization
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public static string Hash(string json) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    public static ExportSnapshotPayloadDto Read(ExportSnapshot snapshot)
    {
        if (Hash(snapshot.PayloadJson) != snapshot.Hash) throw new InvalidDataException("Snapshot checksum mismatch.");
        return JsonSerializer.Deserialize<ExportSnapshotPayloadDto>(snapshot.PayloadJson, Options) ?? throw new InvalidDataException("Snapshot missing.");
    }
}
