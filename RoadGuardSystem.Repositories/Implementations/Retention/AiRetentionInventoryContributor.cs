using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention;

namespace RoadGuardSystem.Repositories.Retention;

public sealed class AiRetentionInventoryContributor(RoadGuardDbContext db) : IRetentionInventoryContributor
{
    public string Name => "AI";
    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var sourceRuns = await (from file in db.Set<AiManifestFileReference>().AsNoTracking()
                                join run in db.Set<AiMockRun>().AsNoTracking() on file.RunId equals run.Id
                                where file.FileId == fileId
                                select new { run.Id, run.ProjectId, run.ManifestHash, run.RowVersion }).ToArrayAsync(token);
        var frameRuns = await (from proof in db.Set<AiDetectionProvenance>().AsNoTracking()
                               join run in db.Set<AiMockRun>().AsNoTracking() on proof.RunId equals run.Id
                               where proof.FrameFileId == fileId
                               select new { run.Id, run.ProjectId, run.ManifestHash, run.RowVersion }).ToArrayAsync(token);
        var references = sourceRuns.Concat(frameRuns).DistinctBy(r => r.Id)
            .Select(r => new RetentionReferenceViewFact("AI_PROVENANCE", r.Id, r.ProjectId,
                r.ManifestHash + ":" + Convert.ToBase64String(r.RowVersion))).ToList();
        // Work and artifact ownership survives retries, cancellation and terminal failure.
        var sourceWork = await db.Set<AiVideoWorkItem>().AsNoTracking()
            .Where(w => w.SourceVideoFileId == fileId).ToArrayAsync(token);
        references.AddRange(sourceWork.Select(w => new RetentionReferenceViewFact("AI_VIDEO_SOURCE", w.Id, w.ProjectId,
            RetentionInventoryRepository.Hash(new { w.DatasetId, w.SourceVideoFileId, w.ProjectId, w.ProcessingJobId }))));
        var artifacts = await (from artifact in db.Set<AiWorkerArtifact>().AsNoTracking()
                               join work in db.Set<AiVideoWorkItem>().AsNoTracking() on artifact.WorkItemId equals work.Id
                               where artifact.FileId == fileId
                               select new { Artifact = artifact, work.ProjectId }).ToArrayAsync(token);
        references.AddRange(artifacts.Select(a => new RetentionReferenceViewFact("AI_WORKER_ARTIFACT", a.Artifact.Id, a.ProjectId,
            RetentionInventoryRepository.Hash(a.Artifact))));
        var sourceWorkIds = sourceWork.Select(w => w.Id).ToArray();
        var revisions = await db.Set<AiVideoTelemetryRevision>().AsNoTracking()
            .Where(r => sourceWorkIds.Contains(r.WorkId)).ToArrayAsync(token);
        references.AddRange(revisions.Select(r => new RetentionReferenceViewFact("AI_VIDEO_TELEMETRY", r.Id,
            sourceWork.Single(w => w.Id == r.WorkId).ProjectId, RetentionInventoryRepository.Hash(r))));
        var external = await ExternalSrtFilesAsync(null, token);
        references.AddRange(external.Files.Where(x => x.FileId == fileId)
            .Select(x => new RetentionReferenceViewFact("AI_EXTERNAL_SRT", x.Revision.Id, x.ProjectId,
                RetentionInventoryRepository.Hash(x.Revision))));
        // A revision without a unique StoredFile cannot enter the file-based evaluator.
        // Until that missing mapping is repaired, do not certify a complete AI inventory.
        return new(Name, !external.Unresolved, references,
            external.Unresolved ? ["AI_EXTERNAL_SRT_FILE_UNRESOLVED"] : []);
    }
    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var sourceIds = await (from file in db.Set<AiManifestFileReference>().AsNoTracking()
                               join run in db.Set<AiMockRun>().AsNoTracking() on file.RunId equals run.Id
                               where run.ProjectId == projectId
                               select file.FileId).Distinct().ToArrayAsync(token);
        var frameIds = await (from proof in db.Set<AiDetectionProvenance>().AsNoTracking()
                              join run in db.Set<AiMockRun>().AsNoTracking() on proof.RunId equals run.Id
                              where run.ProjectId == projectId
                              select proof.FrameFileId).Distinct().ToArrayAsync(token);
        var realSources = await db.Set<AiVideoWorkItem>().AsNoTracking()
            .Where(w => w.ProjectId == projectId).Select(w => w.SourceVideoFileId).ToArrayAsync(token);
        var realArtifacts = await (from artifact in db.Set<AiWorkerArtifact>().AsNoTracking()
                                   join work in db.Set<AiVideoWorkItem>().AsNoTracking() on artifact.WorkItemId equals work.Id
                                   where work.ProjectId == projectId && artifact.FileId != null
                                   select artifact.FileId!.Value).ToArrayAsync(token);
        var external = await ExternalSrtFilesAsync(projectId, token);
        return sourceIds.Concat(frameIds).Concat(realSources).Concat(realArtifacts)
            .Concat(external.Files.Select(x => x.FileId)).Distinct().ToArray();
    }

    private async Task<(bool Unresolved, IReadOnlyList<ExternalSrtFile> Files)> ExternalSrtFilesAsync(Guid? projectId, CancellationToken token)
    {
        var revisions = await (from revision in db.Set<AiVideoTelemetryRevision>().AsNoTracking()
                               join work in db.Set<AiVideoWorkItem>().AsNoTracking() on revision.WorkId equals work.Id
                               where revision.Kind == "EXTERNAL_SRT" && (projectId == null || work.ProjectId == projectId)
                               select new { Revision = revision, work.ProjectId }).ToArrayAsync(token);
        // SQL collation may ignore object-key case and trailing spaces. It only finds
        // candidates; storage identity and content evidence are compared ordinally below.
        var candidates = await db.Files.AsNoTracking()
            .Where(f => db.Set<AiVideoTelemetryRevision>().Any(r => r.Kind == "EXTERNAL_SRT" && r.OriginalSrtObjectKey == f.StorageUri))
            .Select(f => new { f.Id, f.StorageUri, f.Checksum, f.SizeBytes }).ToArrayAsync(token);
        var files = new List<ExternalSrtFile>();
        var unresolved = false;
        foreach (var row in revisions)
        {
            var matches = candidates.Where(f => string.Equals(f.StorageUri, row.Revision.OriginalSrtObjectKey, StringComparison.Ordinal)
                && string.Equals(f.Checksum, row.Revision.OriginalSrtSha256, StringComparison.Ordinal)
                && f.SizeBytes == row.Revision.OriginalSrtBytes).ToArray();
            if (matches.Length != 1) { unresolved = true; continue; }
            files.Add(new(row.Revision, row.ProjectId, matches[0].Id));
        }
        return (unresolved, files);
    }

    private sealed record ExternalSrtFile(AiVideoTelemetryRevision Revision, Guid ProjectId, Guid FileId);
}
