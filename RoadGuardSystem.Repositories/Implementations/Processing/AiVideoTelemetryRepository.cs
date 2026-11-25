using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.Repositories.Implementations.Processing;

/// <summary>Current authority, source proof and revision CAS are checked inside one serializable work lock.</summary>
public sealed class AiVideoTelemetryRepository(RoadGuardDbContext db, TimeProvider clock) : IAiVideoTelemetryRepository
{
    public Task<AiVideoWorkPage> DiscoverAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid datasetId, int offset, int limit, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        if (projectId == Guid.Empty || datasetId == Guid.Empty || offset < 0 || limit is < 1 or > 100)
            throw new AiRequestException(422, "invalid_video_query");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await (from d in db.SurveyDataVersions.AsNoTracking()
                    join s in db.Surveys.AsNoTracking() on d.SurveyId equals s.Id
                    where d.Id == datasetId && s.ProjectId == projectId
                    select d.Id).AnyAsync(ct))
            throw new AiRequestException(404, "not_found");
        await AuthorizeAsync(actorId, role, projectId, datasetId, false, ct);
        // Discovery includes pending/failed/zero-detection work and never requires successful ingestion.
        var rows = await (from w in db.AiVideoWorkItems.AsNoTracking()
                          join j in db.ProcessingJobs.AsNoTracking() on w.ProcessingJobId equals j.Id
                          where w.ProjectId == projectId && w.DatasetId == datasetId && j.ProjectId == projectId && j.Mode == "REAL"
                          orderby w.CreatedAt, w.Id
                          select new { w.Id, w.SourceVideoFileId, w.ProcessingJobId, w.CurrentAttemptId, w.State, w.ErrorCode })
            .Skip(offset).Take(limit + 1).ToArrayAsync(ct);
        var result = new AiVideoWorkPage(rows.Take(limit).Select(x => new AiVideoWorkSummary(x.Id, x.SourceVideoFileId,
            x.ProcessingJobId, x.CurrentAttemptId, x.State.ToString(), x.ErrorCode)).ToArray(), offset, limit, rows.Length > limit);
        await tx.CommitAsync(ct); return result;
    });

    public Task<AiVideoTelemetryScope> ReadScopeAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var work = await WorkAsync(projectId, workId, ct);
        await AuthorizeAsync(actorId, role, work, false, ct);
        var scope = await ScopeAsync(work, ct);
        await tx.CommitAsync(ct); return scope;
    });

    public Task<IReadOnlyList<AiVideoTelemetryRevision>> ListAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var work = await WorkAsync(projectId, workId, ct);
        await AuthorizeAsync(actorId, role, work, false, ct);
        await ScopeAsync(work, ct);
        var rows = await db.AiVideoTelemetryRevisions.AsNoTracking().Where(x => x.WorkId == workId).OrderBy(x => x.Number).ToListAsync(ct);
        await tx.CommitAsync(ct); return (IReadOnlyList<AiVideoTelemetryRevision>)rows;
    });

    public Task<AiVideoTelemetryRevision> AppendAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid workId, AiVideoTelemetryAppend request, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var work = await WorkAsync(projectId, workId, ct);
        await AuthorizeAsync(actorId, role, work, true, ct);
        var scope = await ScopeAsync(work, ct);
        var replay = await db.AiVideoTelemetryRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.WorkId == workId && x.ActorId == actorId && x.IdempotencyKeyHash == request.IdempotencyKeyHash, ct);
        if (replay is not null)
        {
            if (replay.PayloadFingerprint != request.PayloadFingerprint) throw new AiRequestException(409, "idempotency_conflict");
            await tx.CommitAsync(ct); return replay;
        }
        if (request.ExpectedHeadId != scope.HeadRevisionId) throw new AiRequestException(412, "telemetry_revision_stale");
        if (request.ExpectedAttemptId != scope.CurrentAttemptId || request.SourceVideoHash != scope.SourceVideoHash ||
            request.ExpectedEmbeddedSrtFileId != scope.EmbeddedSrt?.FileId) throw new AiRequestException(409, "telemetry_source_changed");
        if (scope.HeadRevisionId == Guid.Empty && request.EmbeddedSrtIsValid) throw new AiRequestException(409, "embedded_telemetry_available");
        var number = await db.AiVideoTelemetryRevisions.Where(x => x.WorkId == workId).MaxAsync(x => (int?)x.Number, ct) ?? 0;
        AiVideoTelemetryRevision revision;
        try
        {
            revision = AiVideoTelemetryRevision.Create(request.RevisionId, workId, checked(number + 1), number == 0 ? null : scope.HeadRevisionId,
                actorId, clock.GetUtcNow(), scope.SourceVideoHash, request.Kind, request.IntervalsJson, scope.DurationMs,
                request.OriginalSrtObjectKey, request.OriginalSrtSha256, request.OriginalSrtBytes, request.OffsetMs, request.Note,
                request.IdempotencyKeyHash, request.PayloadFingerprint);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or InvalidOperationException)
        { throw new AiRequestException(422, "telemetry_invalid"); }
        StoredFile? addedFile = null;
        if (revision.Kind == "EXTERNAL_SRT")
        {
            var matches = await db.Files.AsNoTracking().Where(x => x.StorageUri == request.OriginalSrtObjectKey).ToListAsync(ct);
            if (matches.Count > 1 || matches.Any(x =>
                    !string.Equals(x.StorageUri, request.OriginalSrtObjectKey, StringComparison.Ordinal) ||
                    x.Checksum != request.OriginalSrtSha256 || x.SizeBytes != request.OriginalSrtBytes ||
                    x.MimeType != "application/x-subrip" || x.UploadedByUserId != actorId))
                throw new AiRequestException(409, "telemetry_artifact_conflict");
            if (matches.Count == 0)
            {
                addedFile = StoredFile.Create(revision.Id, request.OriginalSrtObjectKey!, "telemetry.srt",
                    "application/x-subrip", request.OriginalSrtBytes!.Value, request.OriginalSrtSha256!, actorId,
                    revision.CreatedAt, null);
                db.Files.Add(addedFile);
            }
        }
        db.AiVideoTelemetryRevisions.Add(revision);
        try
        {
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return revision;
        }
        catch
        {
            // A retry re-reads the durable receipt, including an ambiguous commit.
            // Do not carry a failed insert in the tracker into that attempt.
            db.Entry(revision).State = EntityState.Detached;
            if (addedFile is not null) db.Entry(addedFile).State = EntityState.Detached;
            throw;
        }
    });

    private async Task<AiVideoWorkItem> WorkAsync(Guid projectId, Guid workId, CancellationToken ct)
        => await db.AiVideoWorkItems.FromSqlInterpolated($"SELECT * FROM [AiVideoWorkItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={workId} AND [ProjectId]={projectId}")
            .AsNoTracking().SingleOrDefaultAsync(ct) ?? throw new AiRequestException(404, "not_found");

    private async Task AuthorizeAsync(Guid actorId, UserRoleCode role, AiVideoWorkItem work, bool append, CancellationToken ct)
        => await AuthorizeAsync(actorId, role, work.ProjectId, work.DatasetId, append, ct);

    private async Task AuthorizeAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid datasetId, bool append, CancellationToken ct)
    {
        if (append && role != UserRoleCode.DroneOperator || role is not (UserRoleCode.DroneOperator or UserRoleCode.ProjectManager or UserRoleCode.Supervisor) ||
            !await db.Users.AsNoTracking().AnyAsync(x => x.Id == actorId && x.RoleCode == role && x.Status == UserStatus.Active && !x.MustChangePassword, ct) ||
            !await db.Roles.AsNoTracking().AnyAsync(x => x.Code == role && x.IsActive, ct)) throw new AiRequestException(403, "access_forbidden");
        if (role == UserRoleCode.Supervisor) return;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await db.ProjectMembers.AsNoTracking().AnyAsync(x => x.ProjectId == projectId && x.UserId == actorId && x.RoleCode == role &&
            x.Status == ProjectMemberStatus.Active && x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today), ct)) throw new AiRequestException(403, "access_forbidden");
        if (role == UserRoleCode.ProjectManager) return;
        var requestId = await (from d in db.SurveyDataVersions.AsNoTracking()
                               join s in db.Surveys.AsNoTracking() on d.SurveyId equals s.Id
                               where d.Id == datasetId && s.ProjectId == projectId
                               select s.SurveyRequestId).SingleOrDefaultAsync(ct);
        if (requestId is null || !await db.SurveyRequests.AsNoTracking().AnyAsync(x => x.Id == requestId && x.ProjectId == projectId, ct) ||
            !await db.SurveyAssignments.AsNoTracking().AnyAsync(x => x.SurveyRequestId == requestId && x.OperatorUserId == actorId &&
                x.AcceptedAt != null && x.RejectedAt == null && x.EndedAt == null, ct)) throw new AiRequestException(403, "access_forbidden");
    }

    private async Task<AiVideoTelemetryScope> ScopeAsync(AiVideoWorkItem work, CancellationToken ct)
    {
        var job = await db.ProcessingJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == work.ProcessingJobId, ct);
        var attempt = await db.ProcessingAttempts.AsNoTracking().Where(x => x.ProcessingJobId == work.ProcessingJobId).OrderByDescending(x => x.AttemptNo).FirstOrDefaultAsync(ct);
        if (work.State != AiVideoWorkState.Ingested || job is null || job.Mode != "REAL" || job.Status != ProcessingJobStatus.Completed || job.ProjectId != work.ProjectId || attempt?.Id != work.CurrentAttemptId)
            throw new AiRequestException(409, "telemetry_source_not_ready");
        AiVideoSource source;
        try { source = await new AiVideoWorkRepository(db).ReadSourceAsync(work, ct); }
        catch (InvalidOperationException) { throw new AiRequestException(409, "telemetry_source_not_ready"); }
        long duration;
        try
        {
            using var admission = JsonDocument.Parse(work.AdmissionJson);
            var a = admission.RootElement;
            duration = a.GetProperty("inputDurationMs").GetInt64();
            if (duration <= 0 || a.GetProperty("inputSha256").GetString() != source.Sha256 || a.GetProperty("inputBytes").GetInt64() != source.Bytes ||
                a.GetProperty("attemptId").GetGuid() != work.CurrentAttemptId || a.GetProperty("sourceVideoFileId").GetGuid() != work.SourceVideoFileId)
                throw new InvalidOperationException();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new AiRequestException(409, "telemetry_source_not_ready"); }
        var artifacts = await db.AiWorkerArtifacts.AsNoTracking().Where(x => x.WorkItemId == work.Id && x.AttemptId == work.CurrentAttemptId && x.Kind == "TELEMETRY_SRT").ToListAsync(ct);
        AiEmbeddedTelemetryFile? embedded = null;
        if (artifacts.Count > 1) throw new AiRequestException(409, "telemetry_source_not_ready");
        if (artifacts.SingleOrDefault() is { } artifact)
        {
            var file = await db.Files.AsNoTracking().SingleOrDefaultAsync(x => x.Id == artifact.FileId, ct);
            if (file is null || file.Checksum != artifact.Sha256 || file.SizeBytes != artifact.Bytes || file.MimeType != "application/x-subrip" ||
                !Guid.TryParseExact(artifact.ArtifactId, "D", out var artifactId)) throw new AiRequestException(409, "telemetry_source_not_ready");
            embedded = new(artifactId, file.Id, file.StorageUri, file.Checksum, file.SizeBytes, file.MimeType);
        }
        var head = await db.AiVideoTelemetryRevisions.AsNoTracking().Where(x => x.WorkId == work.Id).OrderByDescending(x => x.Number).Select(x => x.Id).FirstOrDefaultAsync(ct);
        var paired = await ReadPairedAsync(work, source.OwnerId, ct);
        return new(work.Id, work.ProjectId, work.CurrentAttemptId, work.SourceVideoFileId, source.Sha256, duration, work.AdmissionJson, head, embedded, paired);
    }

    internal async Task<AiPairedTelemetryFile?> ReadPairedAsync(AiVideoWorkItem work, Guid ownerId, CancellationToken ct)
    {
        var dataset = await (from d in db.SurveyDataVersions.AsNoTracking()
                             join s in db.Surveys.AsNoTracking() on d.SurveyId equals s.Id
                             where d.Id == work.DatasetId && s.ProjectId == work.ProjectId
                             select new { d.PairsManifest, d.SourceManifest, s.SurveyRequestId }).SingleAsync(ct);
        if (string.IsNullOrWhiteSpace(dataset.PairsManifest)) return null;
        Guid telemetryId; long offset;
        try
        {
            using var pairs = JsonDocument.Parse(dataset.PairsManifest);
            var matches = pairs.RootElement.EnumerateArray().Where(p => p.GetProperty("videoFileId").GetGuid() == work.SourceVideoFileId).ToArray();
            if (matches.Length == 0) return null;
            if (matches.Length != 1) throw new InvalidOperationException();
            var pair = matches[0];
            if (!pair.TryGetProperty("telemetryFileId", out var id) || id.ValueKind == JsonValueKind.Null) return null;
            telemetryId = id.GetGuid(); offset = pair.GetProperty("timeOffsetMilliseconds").GetInt64();
            if (telemetryId == Guid.Empty || offset is < -86400000 or > 86400000) throw new InvalidOperationException();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new AiRequestException(409, "paired_telemetry_provenance_invalid"); }
        var file = await (from f in db.Files.AsNoTracking()
                          join u in db.UploadSessions.AsNoTracking() on f.Id equals u.FileId
                          join s in db.FileScopes.AsNoTracking() on f.Id equals s.FileId
                          where f.Id == telemetryId && s.ProjectId == work.ProjectId && s.TargetId == dataset.SurveyRequestId && s.Purpose == "TELEMETRY" &&
                              s.OwnerUserId == ownerId && u.OwnerUserId == ownerId && f.UploadedByUserId == ownerId && u.Purpose == s.Purpose &&
                              u.Status == UploadSessionStatus.Verified && u.ExpectedChecksumSha256 == f.Checksum && u.ExpectedSizeBytes == f.SizeBytes &&
                              u.MediaType == f.MimeType && u.ObjectKey == f.StorageUri
                          select f).SingleOrDefaultAsync(ct);
        if (file is null) throw new AiRequestException(409, "paired_telemetry_provenance_invalid");
        try
        {
            using var manifest = JsonDocument.Parse(dataset.SourceManifest);
            if (!manifest.RootElement.EnumerateArray().Any(p => p.GetProperty("fileId").GetGuid() == file.Id &&
                p.GetProperty("purpose").GetString() == "TELEMETRY" && p.GetProperty("checksumSha256").GetString() == file.Checksum &&
                p.GetProperty("sizeBytes").GetInt64() == file.SizeBytes)) throw new InvalidOperationException();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        { throw new AiRequestException(409, "paired_telemetry_provenance_invalid"); }
        return new(new(Guid.Empty, file.Id, file.StorageUri, file.Checksum, file.SizeBytes, file.MimeType), offset);
    }
}
