using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Processing;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;

namespace RoadGuardSystem.Repositories.Processing;

public sealed class Anh02AiRepository(RoadGuardDbContext db, IdempotencyOperationService idempotency,
    IProjectMembershipRepository membership, TimeProvider clock) : IAnh02AiRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task AuthorizeAsync(Guid actorId, UserRoleCode role, Guid projectId, bool managerOnly, CancellationToken ct)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || (managerOnly && role != UserRoleCode.ProjectManager)
            || !await db.Users.AsNoTracking().AnyAsync(u => u.Id == actorId && u.RoleCode == role && u.Status == UserStatus.Active && !u.MustChangePassword, ct))
            throw new AiRequestException(403, "access_forbidden");
        if (!await db.Roles.AsNoTracking().AnyAsync(r => r.Code == role && r.IsActive, ct)) throw new AiRequestException(403, "access_forbidden");
        if (!await db.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId, ct)) throw new AiRequestException(404, "not_found");
        if (role == UserRoleCode.Supervisor) return;
        var member = await membership.FindByUserAndProjectAsync(actorId, projectId, ct);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (member is null || member.Status != ProjectMemberStatus.Active || member.RoleCode != role
            || member.ValidFrom > today || (member.ValidTo is { } end && end < today))
            throw new AiRequestException(403, "access_forbidden");
    }

    public async Task<AiDatasetSourceFacts> ReadSourcesAsync(Guid projectId, CreateAiMockRunRequestFact request, CancellationToken ct)
    {
        var row = await (from d in db.SurveyDataVersions.AsNoTracking()
                         join s in db.Surveys.AsNoTracking() on d.SurveyId equals s.Id
                         where d.Id == request.DatasetId
                         select new { Data = d, s.ProjectId, s.SurveyRequestId }).SingleOrDefaultAsync(ct);
        if (row is null || row.ProjectId != projectId) throw new AiRequestException(404, "not_found");
        if (!await db.SurveyRequests.AsNoTracking().AnyAsync(t => t.Id == row.SurveyRequestId && t.ProjectId == projectId, ct))
            throw new AiRequestException(409, "source_not_ready");
        if (row.Data.Status != SurveyDataVersionStatus.ServerConfirmed || row.Data.IntegrityStatus != SurveyDataIntegrityStatus.Passed
            || row.Data.ScopeManifest is null || row.Data.PairsManifest is null) throw new AiRequestException(409, "source_not_ready");
        if (!await db.AIModelVersions.AsNoTracking().AnyAsync(m => m.Id == request.ModelVersionId && m.Status == AIModelVersionStatus.Released, ct))
            throw new AiRequestException(422, "ai_contract_invalid");
        if (request.FixtureVersion == "synthetic-road-v1" && !await db.DefectTypes.AsNoTracking().AnyAsync(t => t.Code == "CRACK" && t.IsActive, ct))
            throw new AiRequestException(409, "source_not_ready");
        try
        {
            using var scopes = JsonDocument.Parse(row.Data.ScopeManifest);
            if (scopes.RootElement.ValueKind != JsonValueKind.Array) throw new AiRequestException(409, "source_not_ready");
            var scopeItems = scopes.RootElement.EnumerateArray().ToArray();
            if (scopeItems.Length == 0 || scopeItems.Any(s => !Unique(s))) throw new AiRequestException(409, "source_not_ready");
            var tuples = scopeItems.SelectMany(s => Field(s, "segmentIds").EnumerateArray().Select(segment =>
                (Route: Field(s, "routeVersionId").GetGuid(), Set: Field(s, "segmentSetId").GetGuid(),
                    Segment: segment.GetGuid(), Band: Field(s, "targetBand").GetString()))).ToArray();
            if (tuples.Length == 0 || tuples.Distinct().Count() != tuples.Length || tuples.Any(t => t.Route == Guid.Empty
                || t.Set == Guid.Empty || t.Segment == Guid.Empty || t.Band is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE")))
                throw new AiRequestException(409, "source_not_ready");
            if (request.Scope.SegmentIds.Any(id => !tuples.Contains((request.Scope.RouteVersionId,
                request.Scope.SegmentSetId, id, request.Scope.TargetBand)))) throw new AiRequestException(422, "ai_contract_invalid");
            using var source = JsonDocument.Parse(row.Data.SourceManifest);
            if (source.RootElement.ValueKind != JsonValueKind.Array || source.RootElement.EnumerateArray().Any(s => !Unique(s)))
                throw new AiRequestException(409, "source_not_ready");
            var sourceItems = source.RootElement.EnumerateArray().ToArray();
            var ids = sourceItems.Select(s => s.GetProperty("fileId").GetGuid()).ToArray();
            if (ids.Length == 0 || ids.Distinct().Count() != ids.Length) throw new AiRequestException(409, "source_not_ready");
            var files = await (from f in db.Files.AsNoTracking()
                               join s in db.FileScopes.AsNoTracking() on f.Id equals s.FileId
                               join u in db.UploadSessions.AsNoTracking() on f.Id equals u.FileId
                               where ids.Contains(f.Id)
                               && s.ProjectId == projectId && s.TargetId == row.SurveyRequestId && u.Status == UploadSessionStatus.Verified
                               && s.OwnerUserId == u.OwnerUserId && u.OwnerUserId == f.UploadedByUserId
                               && u.ExpectedChecksumSha256 == f.Checksum && u.ExpectedSizeBytes == f.SizeBytes && u.MediaType == f.MimeType
                               && u.Purpose == s.Purpose && (s.Purpose == "SURVEY_VIDEO" || s.Purpose == "TELEMETRY")
                               select new { File = f, Upload = u, s.Purpose }).ToArrayAsync(ct);
            if (files.Length != ids.Length || files.Any(f => !sourceItems.Any(s => s.GetProperty("fileId").GetGuid() == f.File.Id
                && s.GetProperty("checksumSha256").GetString() == f.File.Checksum && s.GetProperty("sizeBytes").GetInt64() == f.File.SizeBytes
                && s.GetProperty("mediaType").GetString() == f.File.MimeType && s.GetProperty("purpose").GetString() == f.Purpose)))
                throw new AiRequestException(409, "source_not_ready");
            var pairs = JsonSerializer.Deserialize<AiSourcePair[]>(row.Data.PairsManifest, Json) ?? [];
            if (pairs.Select(p => p.VideoFileId).Distinct().Count() != pairs.Length
                || pairs.Any(p => !files.Any(f => f.File.Id == p.VideoFileId && f.Purpose == "SURVEY_VIDEO")
                    || (p.TelemetryFileId is { } telemetry && !files.Any(f => f.File.Id == telemetry && f.Purpose == "TELEMETRY"))))
                throw new AiRequestException(409, "source_not_ready");
            var hash = Hash(JsonSerializer.Serialize(new { row.Data.SourceManifest, row.Data.ScopeManifest, row.Data.PairsManifest }));
            return new(request.DatasetId, projectId, hash, files.Select(f => new AiSourceFileFact(f.File.Id,
                Convert.ToBase64String(f.Upload.RowVersion), f.File.Checksum, f.File.SizeBytes, f.File.MimeType, f.Purpose, f.Upload.ObjectKey)).ToArray(), pairs);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new AiRequestException(409, "source_not_ready"); }
    }

    public async Task<AiMockRunViewFact> AdmitAsync(Guid actorId, UserRoleCode role, Guid projectId, CreateAiMockRunRequestFact request,
        string key, string fingerprint, Func<Guid, Guid, Guid, AiDatasetSourceFacts, CancellationToken, Task<AiAdmissionManifest>> createManifest, CancellationToken ct)
    {
        await AuthorizeAsync(actorId, role, projectId, true, ct);
        var outcome = await idempotency.ExecuteSerializableAsync(actorId, projectId, "Anh02.AiMock.Create", key, fingerprint, async token =>
        {
            await AuthorizeAsync(actorId, role, projectId, true, token);
            if (await db.Projects.AsNoTracking().AnyAsync(p => p.Id == projectId && p.Status == ProjectStatus.Closed, token))
                throw new AiRequestException(409, "source_not_ready");
            var facts = await ReadSourcesAsync(projectId, request, token);
            var now = clock.GetUtcNow(); var runId = Guid.NewGuid(); Guid jobId; Guid attemptId;
            AiMockRun? analysis = null;
            if (request.Stage == "DUPLICATE_MATCHING")
            {
                analysis = await db.Set<AiMockRun>().AsNoTracking().SingleOrDefaultAsync(r => r.Id == request.AnalysisRunId && r.ProjectId == projectId, token);
                if (analysis is null) throw new AiRequestException(404, "not_found");
                if (analysis.Stage != "VIDEO_ANALYSIS" || analysis.Status != "SUCCEEDED" || analysis.DatasetVersionId != request.DatasetId
                    || analysis.ModelVersionId != request.ModelVersionId || analysis.RouteVersionId != request.Scope.RouteVersionId
                    || analysis.SegmentSetId != request.Scope.SegmentSetId || analysis.GeometryVersion != request.ExpectedGeometryVersion
                    || analysis.FixtureVersion != request.FixtureVersion) throw new AiRequestException(409, "source_not_ready");
                jobId = analysis.ProcessingJobId; attemptId = analysis.AttemptId;
            }
            else { jobId = Guid.NewGuid(); attemptId = Guid.NewGuid(); }
            var manifest = await createManifest(runId, jobId, attemptId, facts, token);
            if (manifest.Hash != Hash(manifest.CanonicalJson)) throw new AiRequestException(422, "ai_contract_invalid");
            if (request.Stage == "VIDEO_ANALYSIS")
            {
                var range = JsonSerializer.Serialize(new { anh02 = request.Scope }, Json);
                var block = await db.ProcessingBlocks.SingleOrDefaultAsync(b => b.SurveyDataVersionId == request.DatasetId && b.RangeMetadata == range, token);
                if (block is null)
                {
                    var number = (await db.ProcessingBlocks.Where(b => b.SurveyDataVersionId == request.DatasetId).MaxAsync(b => (int?)b.BlockNo, token) ?? 0) + 1;
                    block = ProcessingBlock.Create(Guid.NewGuid(), request.DatasetId, number, range); db.ProcessingBlocks.Add(block);
                }
                db.ProcessingJobs.Add(ProcessingJob.CreateQueued(jobId, block.Id, request.ModelVersionId, projectId, manifest.Hash, manifest.CanonicalJson, "MOCK"));
                db.ProcessingAttempts.Add(ProcessingAttempt.Create(attemptId, jobId, 1, now, null, null, "anh02.mock"));
            }
            var run = new AiMockRun
            {
                Id = runId,
                ProjectId = projectId,
                DatasetVersionId = request.DatasetId,
                ProcessingJobId = jobId,
                AttemptId = attemptId,
                ModelVersionId = request.ModelVersionId,
                RouteVersionId = request.Scope.RouteVersionId,
                SegmentSetId = request.Scope.SegmentSetId,
                CreatedBy = actorId,
                CreatedAt = now,
                Stage = request.Stage,
                FixtureVersion = request.FixtureVersion,
                GeometryVersion = manifest.GeometryVersion,
                CanonicalManifest = manifest.CanonicalJson,
                ManifestHash = manifest.Hash,
                AnalysisRunId = analysis?.Id
            };
            db.Set<AiMockRun>().Add(run);
            db.Set<AiManifestFileReference>().AddRange(facts.Files.Select(f => new AiManifestFileReference { RunId = runId, FileId = f.FileId, FileVersion = f.FileVersion }));
            Audit(actorId, runId, now, "anh02_ai_mock_admitted"); await db.SaveChangesAsync(token);
            return (runId, JsonSerializer.Serialize(View(run), Json));
        }, ct, receiptAccessGuard: async token =>
        {
            await Anh02ReceiptAuthority.LockAsync(db, actorId, projectId, token);
            await AuthorizeAsync(actorId, role, projectId, true, token);
        });
        if (outcome.Status == IdempotencyOperationStatus.Conflict) throw new AiRequestException(409, "duplicate_request");
        return JsonSerializer.Deserialize<AiMockRunViewFact>(outcome.OutcomeJson!, Json)!;
    }

    public Task<AiMockRun?> GetAsync(Guid projectId, Guid runId, CancellationToken ct)
        => db.Set<AiMockRun>().AsNoTracking().SingleOrDefaultAsync(r => r.Id == runId && r.ProjectId == projectId, ct);
    public Task<AiResultProvenance?> GetResultAsync(Guid runId, CancellationToken ct)
        => db.Set<AiResultProvenance>().AsNoTracking().SingleOrDefaultAsync(r => r.RunId == runId, ct);

    public async Task<AiMockRun?> ClaimAsync(Guid owner, DateTimeOffset now, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var run = await db.Set<AiMockRun>().OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
                .FirstOrDefaultAsync(r => r.Status == "QUEUED" || (r.Status == "RUNNING" && r.LeaseUntil < now), ct);
            if (run is null) return null;
            run.Status = "RUNNING"; run.LeaseOwner = owner; run.LeaseUntil = now.AddMinutes(5); run.ResultTimestamp ??= now;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return run;
        });
    }

    public async Task<bool> CompleteAsync(Guid runId, Guid owner, AiCompletion completion,
        Func<CancellationToken, Task<bool>> recheckGeometry, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var run = await db.Set<AiMockRun>().SingleAsync(r => r.Id == runId, ct);
            if (run.Status == "SUCCEEDED") return run.ResultId == completion.Result.Id;
            if (run.Status != "RUNNING" || run.LeaseOwner != owner || (run.LeaseUntil is null || run.LeaseUntil <= clock.GetUtcNow())) return false;
            await AuthorizeAsync(run.CreatedBy, UserRoleCode.ProjectManager, run.ProjectId, true, ct);
            if (!await recheckGeometry(ct)) throw new AiRequestException(409, "source_not_ready");
            if (completion.Result.RunId != run.Id || completion.Result.ProcessingJobId != run.ProcessingJobId || completion.Result.AttemptId != run.AttemptId
                || completion.Result.ManifestHash != run.ManifestHash || completion.Result.ResultHash != Hash(completion.Result.CanonicalResult))
                throw new AiRequestException(422, "ai_contract_invalid");
            db.Files.AddRange(completion.Frames); db.AIDetections.AddRange(completion.Detections);
            db.Set<AiResultProvenance>().Add(completion.Result); db.Set<AiDetectionProvenance>().AddRange(completion.Proofs);
            if (run.Stage == "VIDEO_ANALYSIS") (await db.ProcessingJobs.SingleAsync(j => j.Id == run.ProcessingJobId, ct)).Complete(completion.Result.CompletedAt);
            run.Status = "SUCCEEDED"; run.ResultId = completion.Result.Id; run.CompletedAt = clock.GetUtcNow(); run.LeaseOwner = null; run.LeaseUntil = null;
            Audit(run.CreatedBy, run.Id, run.CompletedAt.Value, "anh02_ai_mock_completed");
            await db.SaveChangesAsync(ct);
            if (run.Stage == "VIDEO_ANALYSIS") await CloseAnalysisAttemptAsync(run, ProcessingAttemptErrorType.None, ct);
            await transaction.CommitAsync(ct); return true;
        });
    }

    public async Task FailAsync(Guid runId, Guid owner, string code, CancellationToken ct)
    {
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var run = await db.Set<AiMockRun>().SingleAsync(r => r.Id == runId, ct);
            var now = clock.GetUtcNow();
            if (run.Status != "RUNNING" || run.LeaseOwner != owner || run.LeaseUntil is null || run.LeaseUntil <= now) return;
            run.Status = "FAILED"; run.ErrorCode = code; run.CompletedAt = now; run.LeaseOwner = null; run.LeaseUntil = null;
            if (run.Stage == "VIDEO_ANALYSIS")
                (await db.ProcessingJobs.SingleAsync(j => j.Id == run.ProcessingJobId, ct)).FailData(now, code);
            Audit(run.CreatedBy, run.Id, now, "anh02_ai_mock_failed");
            await db.SaveChangesAsync(ct);
            if (run.Stage == "VIDEO_ANALYSIS") await CloseAnalysisAttemptAsync(run, ProcessingAttemptErrorType.Data, ct);
            await transaction.CommitAsync(ct);
        });
    }

    private async Task CloseAnalysisAttemptAsync(AiMockRun run, ProcessingAttemptErrorType error, CancellationToken ct)
    {
        // EF still forbids generic attempt mutation. The additive SQL fence permits only this
        // one-way closure of an ANH-02 analysis after run/job terminal writes, in the same tx.
        var changed = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ProcessingAttempts] SET [EndedAt]={run.CompletedAt}, [ErrorType]={(byte)error} WHERE [Id]={run.AttemptId} AND [ProcessingJobId]={run.ProcessingJobId} AND [EndedAt] IS NULL AND [ErrorType] IS NULL", ct);
        if (changed != 1) throw new InvalidOperationException("ANH-02 analysis attempt cannot be closed.");
    }

    public async Task<AiCandidateReadFacts?> ReadCandidateAsync(Guid projectId, Guid detectionId, CancellationToken ct)
    {
        var row = await (from proof in db.Set<AiDetectionProvenance>().AsNoTracking()
                         join run in db.Set<AiMockRun>().AsNoTracking() on proof.RunId equals run.Id
                         join result in db.Set<AiResultProvenance>().AsNoTracking() on proof.ResultId equals result.Id
                         join job in db.ProcessingJobs.AsNoTracking() on run.ProcessingJobId equals job.Id
                         join attempt in db.ProcessingAttempts.AsNoTracking() on run.AttemptId equals attempt.Id
                         join block in db.ProcessingBlocks.AsNoTracking() on job.ProcessingBlockId equals block.Id
                         join detection in db.AIDetections.AsNoTracking() on proof.DetectionId equals detection.Id
                         join frame in db.Files.AsNoTracking() on proof.FrameFileId equals frame.Id
                         join source in db.Files.AsNoTracking() on proof.SourceVideoFileId equals source.Id
                         join upload in db.UploadSessions.AsNoTracking() on source.Id equals upload.FileId
                         where proof.DetectionId == detectionId && run.ProjectId == projectId && run.Status == "SUCCEEDED" && upload.Status == UploadSessionStatus.Verified
                             && job.ProjectId == projectId && job.ModelVersionId == run.ModelVersionId && job.Mode == "MOCK"
                             && job.ManifestHash == run.ManifestHash && job.ManifestJson == run.CanonicalManifest && job.Status == ProcessingJobStatus.Completed
                             && block.SurveyDataVersionId == run.DatasetVersionId && attempt.ProcessingJobId == job.Id
                             && attempt.EndedAt != null && attempt.ErrorType == ProcessingAttemptErrorType.None
                         select new { proof, run, result, detection, frame.Checksum, frame.SizeBytes, frame.MimeType, SourceVersion = upload.RowVersion, SourceHash = source.Checksum, SourceBytes = source.SizeBytes }).SingleOrDefaultAsync(ct);
        if (row is null) return null;
        var disposition = await (from head in db.Set<HuyCandidateSourceHead>().AsNoTracking()
                                 join decision in db.SourceDecisions.AsNoTracking() on head.DecisionId equals decision.Id
                                 where head.SourceKind == CandidateSourceKind.AiDetection && head.SourceId == detectionId && head.ProjectId == projectId
                                 select new { head.DecisionId, HeadVersion = head.RowVersion, DecisionVersion = EF.Property<byte[]>(decision, "RowVersion") }).SingleOrDefaultAsync(ct);
        return new(row.run, row.result, row.proof, row.detection, row.Checksum, row.SizeBytes, row.MimeType, Convert.ToBase64String(row.SourceVersion), row.SourceHash, row.SourceBytes,
            disposition is null ? null : new(disposition.DecisionId, Convert.ToBase64String(disposition.DecisionVersion)),
            disposition is null ? null : Convert.ToBase64String(disposition.DecisionVersion));
    }
    public Task<Guid?> DetectionProjectAsync(Guid detectionId, CancellationToken ct)
        => (from detection in db.AIDetections.AsNoTracking()
            join job in db.ProcessingJobs.AsNoTracking()
            on detection.ProcessingJobId equals job.Id
            where detection.Id == detectionId
            select (Guid?)job.ProjectId).SingleOrDefaultAsync(ct);
    private void Audit(Guid actor, Guid run, DateTimeOffset now, string action)
        => db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, now, action, "AiMockRun", run, null, "{}", "Synthetic mock provenance", "ANH-02", null, []));
    // Existing dataset scope/pairs were persisted with CLR PascalCase; wire and
    // historical fixtures also use camelCase. Reject ambiguous aliases, keep raw hash.
    private static JsonElement Field(JsonElement value, string name)
        => value.EnumerateObject().Single(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object
        && value.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == value.EnumerateObject().Count();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static AiMockRunViewFact View(AiMockRun run) => new(run.Id, run.ProjectId, run.Stage, "MOCK", run.Status, run.ProcessingJobId,
        run.AttemptId, run.ManifestHash, run.ResultId, run.FixtureVersion, run.ErrorCode, Convert.ToBase64String(run.RowVersion));
}
