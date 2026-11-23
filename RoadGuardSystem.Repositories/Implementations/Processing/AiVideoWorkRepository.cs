using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Processing;

/// <summary>SQL boundary for per-video REAL transport; network and model calls stay outside transactions.</summary>
public sealed partial class AiVideoWorkRepository(RoadGuardDbContext db) : IAiVideoWorkRepository
{
    public async Task<AiVideoSource?> NextSourceAsync(string logicalConfigurationIdentity, CancellationToken ct, IReadOnlySet<Guid>? excludedDatasetIds = null)
    {
        // Only explicit post-submission intents are eligible; enabling this worker never sweeps historical data.
        var intents = db.OutboxMessages.AsNoTracking().Where(x => x.MessageType == "ai.real.dataset_submitted.v1");
        if (excludedDatasetIds is { Count: > 0 })
        {
            var excluded = excludedDatasetIds.ToArray();
            intents = intents.Where(x => x.CorrelationId == null || !excluded.Contains(x.CorrelationId.Value));
        }
        var datasets = await intents.OrderBy(x => x.OccurredAtUtc).Select(x => x.CorrelationId).ToListAsync(ct);
        foreach (var datasetId in datasets.OfType<Guid>())
        {
            var row = await (from data in db.SurveyDataVersions.AsNoTracking()
                join survey in db.Surveys.AsNoTracking() on data.SurveyId equals survey.Id
                where data.Id == datasetId && data.Status == SurveyDataVersionStatus.ServerConfirmed && data.IntegrityStatus == SurveyDataIntegrityStatus.Passed
                select new { data.SourceManifest, survey.ProjectId }).SingleOrDefaultAsync(ct);
            if (row is null) continue;
            foreach (var fileId in VideoIds(row.SourceManifest))
            {
                if (await db.Set<AiVideoWorkItem>().AsNoTracking().AnyAsync(x => x.DatasetId == datasetId && x.SourceVideoFileId == fileId && x.LogicalConfigurationIdentity == logicalConfigurationIdentity, ct)) continue;
                var rejectedKey = RejectedKey(datasetId, fileId, logicalConfigurationIdentity);
                if (await db.OutboxMessages.AsNoTracking().AnyAsync(x => x.MessageType == "ai.real.source_rejected.v1" && x.PayloadJson == rejectedKey, ct)) continue;
                var source = await SourceAsync(datasetId, row.ProjectId, fileId, ct);
                if (source is not null) return source;
            }
        }
        return null;
    }

    public async Task RecordSourceFailureAsync(AiVideoSource source, string logicalConfigurationIdentity, string errorCode, CancellationToken ct)
    {
        if (errorCode is not ("SOURCE_CHECKSUM_MISMATCH" or "VIDEO_METADATA_INVALID")) throw new ArgumentException("Not a permanent source rejection.", nameof(errorCode));
        // Durable source quarantine is separate from attempt failures: malformed media cannot form a valid admission.
        var key = RejectedKey(source.DatasetId, source.FileId, logicalConfigurationIdentity);
        db.OutboxMessages.Add(RoadGuardSystem.BusinessObjects.Messaging.OutboxMessage.Create(Guid.NewGuid(), "ai.real.source_rejected.v1", DateTimeOffset.UtcNow, source.DatasetId, key));
        await db.SaveChangesAsync(ct);
    }

    public Task<Guid> PrepareAsync(AiVideoSource source, AiPreparedVideo p, IReadOnlyDictionary<string, string> classMap, CancellationToken ct)
        => RunTransactionAsync(IsolationLevel.Serializable, repo => repo.PrepareCoreAsync(source, p, classMap, ct), ct);

    private async Task<Guid> PrepareCoreAsync(AiVideoSource source, AiPreparedVideo p, IReadOnlyDictionary<string, string> classMap, CancellationToken ct)
    {
        var resource = "ai-video-dataset:" + source.DatasetId.ToString("D");
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'AI_DATASET_LOCK_UNAVAILABLE', 1;", ct);
        var current = await SourceAsync(source.DatasetId, source.ProjectId, source.FileId, ct);
        if (current is null || current.DatasetId != source.DatasetId || current.ProjectId != source.ProjectId || current.FileId != source.FileId ||
            current.OwnerId != source.OwnerId || current.ObjectKey != source.ObjectKey || current.Sha256 != source.Sha256 || current.Bytes != source.Bytes ||
            source.ScopeManifest is not null && source.ScopeManifest != current.ScopeManifest ||
            source.SourceVideoVersion is not null && source.SourceVideoVersion != current.SourceVideoVersion)
            throw new InvalidOperationException("Source scope changed.");
        await CheckMapAsync(p.ModelVersionId, classMap, ct);
        var prior = await db.Set<AiVideoWorkItem>().SingleOrDefaultAsync(x => x.DatasetId == source.DatasetId && x.SourceVideoFileId == source.FileId && x.LogicalConfigurationIdentity == p.LogicalConfigurationIdentity, ct);
        if (prior is not null) return prior.Id;
        using var admission = JsonDocument.Parse(p.AdmissionJson);
        var a = admission.RootElement;
        if (a.GetProperty("projectId").GetGuid() != source.ProjectId || a.GetProperty("datasetId").GetGuid() != source.DatasetId ||
            a.GetProperty("sourceVideoFileId").GetGuid() != source.FileId || a.GetProperty("jobId").GetGuid() != p.JobId ||
            a.GetProperty("attemptId").GetGuid() != p.AttemptId || a.GetProperty("modelVersionId").GetGuid() != p.ModelVersionId ||
            a.GetProperty("inputSha256").GetString() != source.Sha256 || a.GetProperty("inputBytes").GetInt64() != source.Bytes ||
            a.GetProperty("manifestHash").GetString() != p.ManifestHash || Hash(p.ManifestJson) != p.ManifestHash)
            throw new InvalidOperationException("Prepared identity mismatch.");
        var blockNo = (await db.ProcessingBlocks.Where(x => x.SurveyDataVersionId == source.DatasetId).MaxAsync(x => (int?)x.BlockNo, ct) ?? 0) + 1;
        var block = ProcessingBlock.Create(Guid.NewGuid(), source.DatasetId, blockNo, JsonSerializer.Serialize(new { sourceVideoFileId = source.FileId }));
        var job = ProcessingJob.CreateQueued(p.JobId, block.Id, p.ModelVersionId, source.ProjectId, p.ManifestHash, p.ManifestJson, "REAL");
        var now = DateTimeOffset.UtcNow;
        var attempt = ProcessingAttempt.Create(p.AttemptId, job.Id, 1, now, null, null, null);
        var work = AiVideoWorkItem.Create(Guid.NewGuid(), source.DatasetId, source.FileId, source.ProjectId, job.Id, attempt.Id,
            p.LogicalConfigurationIdentity, p.AdmissionJson, now);
        db.ProcessingBlocks.Add(block); db.ProcessingJobs.Add(job); db.ProcessingAttempts.Add(attempt); db.Set<AiVideoWorkItem>().Add(work);
        return work.Id;
    }

    public async Task<AiVideoWorkItem?> ClaimAsync(string owner, DateTimeOffset now, TimeSpan lease, CancellationToken ct)
    {
        var work = await RunTransactionAsync(IsolationLevel.ReadCommitted, repo => repo.ClaimCoreAsync(owner, now, lease, ct), ct);
        // Preserve the repository's tracked lease API without sharing retry-attempt trackers.
        if (work is null) return null;
        var tracked = db.AiVideoWorkItems.Local.SingleOrDefault(x => x.Id == work.Id);
        if (tracked is null) { db.Attach(work); return work; }
        if (db.Entry(tracked).State != EntityState.Unchanged)
            throw new InvalidOperationException("Cannot replace a locally modified lease.");
        db.Entry(tracked).CurrentValues.SetValues(work);
        db.Entry(tracked).State = EntityState.Unchanged;
        return tracked;
    }

    private async Task<AiVideoWorkItem?> ClaimCoreAsync(string owner, DateTimeOffset now, TimeSpan lease, CancellationToken ct)
    {
        if (lease <= TimeSpan.Zero || lease > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(lease));
        var work = await db.Set<AiVideoWorkItem>().FromSqlInterpolated($"SELECT TOP (1) * FROM [AiVideoWorkItems] WITH (UPDLOCK, READPAST, ROWLOCK) WHERE [State] IN (1,2,3,4) AND [NextPollAt] <= {now} AND ([LeaseUntil] IS NULL OR [LeaseUntil] <= {now}) ORDER BY [NextPollAt], [CreatedAt], [Id]")
            .SingleOrDefaultAsync(ct);
        if (work is not null) work.AcquireLease(owner, now, now.Add(lease));
        return work;
    }

    public async Task<AiVideoSource> ReadSourceAsync(AiVideoWorkItem work, CancellationToken ct)
        => await SourceAsync(work.DatasetId, work.ProjectId, work.SourceVideoFileId, ct) ?? throw new InvalidOperationException("Source is no longer verified.");

    public Task ChangeAsync(Guid id, string owner, Guid attemptId, AiVideoWorkState? state, string? errorCode, DateTimeOffset now, DateTimeOffset nextPoll, CancellationToken ct)
        => RunTransactionAsync(IsolationLevel.ReadCommitted, async repo => { await repo.ChangeCoreAsync(id, owner, attemptId, state, errorCode, now, nextPoll, ct); return true; }, ct);

    private async Task ChangeCoreAsync(Guid id, string owner, Guid attemptId, AiVideoWorkState? state, string? errorCode, DateTimeOffset now, DateTimeOffset nextPoll, CancellationToken ct)
    {
        var jobId = await WorkJobIdAsync(id, ct);
        var (job, work) = await LockJobAndWorkAsync(jobId, id, ct);
        CheckLease(work, owner, attemptId, now);
        var currentAttempt = await db.ProcessingAttempts.Where(x => x.ProcessingJobId == job.Id).OrderByDescending(x => x.AttemptNo).FirstAsync(ct);
        if (job.Mode != "REAL" || job.ProjectId != work.ProjectId || currentAttempt.Id != attemptId || currentAttempt.EndedAt is not null ||
            job.Status is not (ProcessingJobStatus.Queued or ProcessingJobStatus.Running) || job.StartedAt > now)
            throw new InvalidOperationException("Stale job attempt.");
        if (state is AiVideoWorkState.RetryableFailed or AiVideoWorkState.TerminalFailed &&
            (string.IsNullOrEmpty(errorCode) || errorCode.Length > 80 || errorCode.Any(c => c is not (>= 'A' and <= 'Z' or >= '0' and <= '9' or '_'))))
            throw new ArgumentException("A safe failure code is required.", nameof(errorCode));
        if (state is { } target) work.Transition(owner, attemptId, target, now, nextPoll, errorCode);
        if (state is AiVideoWorkState.Running or AiVideoWorkState.RetryableFailed or AiVideoWorkState.TerminalFailed or AiVideoWorkState.Cancelled)
        {
            job.ApplyRealWorkerState(state switch { AiVideoWorkState.Running => ProcessingJobStatus.Running,
                AiVideoWorkState.RetryableFailed => ProcessingJobStatus.RetryableFailure,
                AiVideoWorkState.TerminalFailed => ProcessingJobStatus.DataFailure, _ => ProcessingJobStatus.Cancelled }, now, errorCode);
        }
        if (work.State is AiVideoWorkState.Pending or AiVideoWorkState.Admitted or AiVideoWorkState.Running or AiVideoWorkState.ArtifactsReady)
            work.ReleaseLease(owner, attemptId, now, nextPoll);
        if (state is AiVideoWorkState.RetryableFailed or AiVideoWorkState.TerminalFailed or AiVideoWorkState.Cancelled)
            db.AiVideoAttemptOutcomes.Add(AiVideoAttemptOutcome.Create(attemptId, work.Id, job.Id,
                work.State, work.ErrorCode, Hash(work.AdmissionJson), now));
    }

    public Task CompleteAsync(Guid id, string owner, Guid attemptId, IReadOnlyList<AiRegisteredArtifact> artifacts,
        IReadOnlyList<AiProposedDetection> detections, IReadOnlyDictionary<string, string> classMap, DateTimeOffset now, CancellationToken ct)
        => RunTransactionAsync(IsolationLevel.Serializable, async repo => { await repo.CompleteCoreAsync(id, owner, attemptId, artifacts, detections, classMap, now, ct); return true; }, ct);

    private async Task CompleteCoreAsync(Guid id, string owner, Guid attemptId, IReadOnlyList<AiRegisteredArtifact> artifacts,
        IReadOnlyList<AiProposedDetection> detections, IReadOnlyDictionary<string, string> classMap, DateTimeOffset now, CancellationToken ct)
    {
        var jobId = await WorkJobIdAsync(id, ct);
        var (job, work) = await LockJobAndWorkAsync(jobId, id, ct);
        CheckLease(work, owner, attemptId, now);
        if (work.State != AiVideoWorkState.ArtifactsReady) throw new InvalidOperationException("Artifacts are not ready.");
        var currentAttempt = await db.ProcessingAttempts.Where(x => x.ProcessingJobId == job.Id).OrderByDescending(x => x.AttemptNo).FirstAsync(ct);
        if (job.Mode != "REAL" || job.ProjectId != work.ProjectId || currentAttempt.Id != attemptId || currentAttempt.EndedAt is not null ||
            job.Status is not (ProcessingJobStatus.Queued or ProcessingJobStatus.Running) || job.StartedAt > now)
            throw new InvalidOperationException("Stale REAL completion.");
        var source = await SourceAsync(work.DatasetId, work.ProjectId, work.SourceVideoFileId, ct)
            ?? throw new InvalidOperationException("Source is no longer verified.");
        using var admission = JsonDocument.Parse(work.AdmissionJson);
        var a = admission.RootElement;
        using var manifest = JsonDocument.Parse(job.ManifestJson);
        if (a.GetProperty("projectId").GetGuid() != source.ProjectId || a.GetProperty("datasetId").GetGuid() != source.DatasetId ||
            a.GetProperty("sourceVideoFileId").GetGuid() != source.FileId || a.GetProperty("jobId").GetGuid() != job.Id ||
            a.GetProperty("attemptId").GetGuid() != attemptId || a.GetProperty("modelVersionId").GetGuid() != job.ModelVersionId ||
            a.GetProperty("inputSha256").GetString() != source.Sha256 || a.GetProperty("inputBytes").GetInt64() != source.Bytes ||
            a.GetProperty("manifestHash").GetString() != job.ManifestHash || Hash(job.ManifestJson) != job.ManifestHash ||
            manifest.RootElement.TryGetProperty("sourceVideoVersion", out var version) && version.GetString() != source.SourceVideoVersion)
            throw new InvalidOperationException("Completion source identity changed.");
        await CheckMapAsync(job.ModelVersionId, classMap, ct);
        if (artifacts.Count(x => x.Kind == "RAW_RESULT") != 1 || artifacts.Select(x => x.ArtifactId).Distinct().Count() != artifacts.Count || detections.Select(x => x.Id).Distinct().Count() != detections.Count)
            throw new InvalidOperationException("Invalid artifact inventory.");
        var frames = artifacts.Where(x => x.Kind == "FRAME").ToDictionary(x => x.ArtifactId);
        foreach (var d in detections)
            if (!frames.ContainsKey(d.FrameArtifactId) || !classMap.TryGetValue(d.SourceClass, out var type) || type != d.TypeCode)
                throw new InvalidOperationException("Unmapped proposal or frame.");
        var fileIds = new Dictionary<Guid, Guid>();
        foreach (var artifact in artifacts)
        {
            var proof = AiWorkerArtifact.Create(Guid.NewGuid(), work.Id, attemptId, artifact.ArtifactId.ToString("D"), artifact.Sha256, artifact.Bytes, artifact.Kind, artifact.Width, artifact.Height);
            // Private worker IDs never become public BE identities. The SQL mapping commits atomically.
            var fileId = Guid.NewGuid(); fileIds.Add(artifact.ArtifactId, fileId);
            db.Files.Add(StoredFile.Create(fileId, artifact.ObjectKey, fileId.ToString("D"), artifact.MediaType, artifact.Bytes, artifact.Sha256, null, now, null));
            proof.MarkRegistered(fileId); db.Set<AiWorkerArtifact>().Add(proof);
        }
        foreach (var d in detections)
        {
            var payload = System.Text.Json.Nodes.JsonNode.Parse(d.PayloadJson)?.AsObject() ?? throw new InvalidOperationException("Invalid proposal payload.");
            payload["frameFileId"] = fileIds[d.FrameArtifactId];
            payload["sourceVideoFileId"] = work.SourceVideoFileId;
            payload["sourceClass"] = d.SourceClass;
            payload["workItemId"] = work.Id;
            payload["attemptId"] = attemptId;
            payload["groundLocationStatus"] = "NOT_MEASURED";
            db.AIDetections.Add(AIDetection.Create(d.Id, job.Id, job.ModelVersionId, null, null, d.TypeCode, d.Confidence, null, null, payload.ToJsonString()));
        }
        job.Complete(now); work.Transition(owner, attemptId, AiVideoWorkState.Ingested, now);
        db.AiVideoAttemptOutcomes.Add(AiVideoAttemptOutcome.Create(attemptId, work.Id, job.Id,
            AiVideoWorkState.Ingested, null, Hash(work.AdmissionJson), now));
    }

    private async Task<AiVideoSource?> SourceAsync(Guid datasetId, Guid projectId, Guid fileId, CancellationToken ct)
    {
        var row = await (from data in db.SurveyDataVersions.AsNoTracking()
            join survey in db.Surveys.AsNoTracking() on data.SurveyId equals survey.Id
            join scope in db.FileScopes.AsNoTracking() on projectId equals scope.ProjectId
            join file in db.Files.AsNoTracking() on scope.FileId equals file.Id
            join upload in db.UploadSessions.AsNoTracking() on file.Id equals upload.FileId
            where data.Id == datasetId && survey.ProjectId == projectId && data.Status == SurveyDataVersionStatus.ServerConfirmed && data.IntegrityStatus == SurveyDataIntegrityStatus.Passed &&
                file.Id == fileId && scope.Purpose == "SURVEY_VIDEO" && scope.TargetId == survey.SurveyRequestId && upload.Status == UploadSessionStatus.Verified &&
                scope.OwnerUserId == upload.OwnerUserId && upload.OwnerUserId == file.UploadedByUserId && upload.Purpose == scope.Purpose &&
                upload.ExpectedChecksumSha256 == file.Checksum && upload.ExpectedSizeBytes == file.SizeBytes && upload.MediaType == file.MimeType &&
                upload.ObjectKey == file.StorageUri
            select new { data.SourceManifest, data.ScopeManifest, File = file, scope.OwnerUserId, upload.RowVersion }).SingleOrDefaultAsync(ct);
        if (row is null) return null;
        if (!ManifestMatches(row.SourceManifest, fileId, row.File.Checksum, row.File.SizeBytes)) return null;
        return new(datasetId, projectId, fileId, row.OwnerUserId, row.File.StorageUri, row.File.Checksum, row.File.SizeBytes, row.ScopeManifest, Convert.ToBase64String(row.RowVersion));
    }

    private Task<Guid> WorkJobIdAsync(Guid id, CancellationToken ct)
        => db.Set<AiVideoWorkItem>().AsNoTracking().Where(x => x.Id == id).Select(x => x.ProcessingJobId).SingleAsync(ct);

    private async Task<(ProcessingJob Job, AiVideoWorkItem Work)> LockJobAndWorkAsync(Guid jobId, Guid id, CancellationToken ct)
    {
        // Match retry's job -> ledger order. Reload already tracked objects under
        // these locks so a long-lived scope cannot reuse stale lease/state values.
        var job = await db.ProcessingJobs.FromSqlInterpolated($"SELECT * FROM [ProcessingJobs] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={jobId}").SingleAsync(ct);
        await db.Entry(job).ReloadAsync(ct);
        var work = await db.Set<AiVideoWorkItem>().FromSqlInterpolated($"SELECT * FROM [AiVideoWorkItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleAsync(ct);
        await db.Entry(work).ReloadAsync(ct);
        if (work.ProcessingJobId != job.Id) throw new InvalidOperationException("Work job identity changed.");
        return (job, work);
    }

    private static void CheckLease(AiVideoWorkItem work, string owner, Guid attemptId, DateTimeOffset now)
    {
        if (work.CurrentAttemptId != attemptId || work.LeaseOwner != owner || work.LeaseUntil is null || work.LeaseUntil <= now || now < work.UpdatedAt)
            throw new InvalidOperationException("Expired lease, stale attempt, or incorrect lease owner.");
    }

    private async Task CheckMapAsync(Guid modelId, IReadOnlyDictionary<string, string> map, CancellationToken ct)
    {
        string[] labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
        if (map.Count != 6 || labels.Any(x => !map.ContainsKey(x)) || map.Values.Distinct(StringComparer.Ordinal).Count() != 6 ||
            !await db.AIModelVersions.AsNoTracking().AnyAsync(x => x.Id == modelId && x.Status == AIModelVersionStatus.Released, ct)) throw new InvalidOperationException("Released FT95 mapping is not ready.");
        var codes = map.Values.ToArray();
        if (await db.DefectTypes.AsNoTracking().CountAsync(x => codes.Contains(x.Code) && x.IsActive, ct) != 6) throw new InvalidOperationException("Active six-class catalog is not ready.");
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static Guid[] VideoIds(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            return doc.RootElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object &&
                x.TryGetProperty("purpose", out var p) && p.ValueKind == JsonValueKind.String && p.GetString() == "SURVEY_VIDEO" &&
                x.TryGetProperty("fileId", out var id) && id.ValueKind == JsonValueKind.String && id.TryGetGuid(out var value) && value != Guid.Empty)
                .Select(x => x.GetProperty("fileId").GetGuid()).Distinct().ToArray();
        }
        catch (JsonException) { return []; }
    }
    private static bool ManifestMatches(string json, Guid fileId, string hash, long size)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;
            return doc.RootElement.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.Object &&
                x.TryGetProperty("fileId", out var id) && id.ValueKind == JsonValueKind.String && id.TryGetGuid(out var value) && value == fileId &&
                x.TryGetProperty("purpose", out var p) && p.ValueKind == JsonValueKind.String && p.GetString() == "SURVEY_VIDEO" &&
                x.TryGetProperty("checksumSha256", out var h) && h.ValueKind == JsonValueKind.String && h.GetString() == hash &&
                x.TryGetProperty("sizeBytes", out var b) && b.ValueKind == JsonValueKind.Number && b.TryGetInt64(out var bytes) && bytes == size);
        }
        catch (JsonException) { return false; }
    }
    private static string RejectedKey(Guid datasetId, Guid fileId, string config)
        => JsonSerializer.Serialize(new { datasetId, sourceVideoFileId = fileId, configuration = config });
}
