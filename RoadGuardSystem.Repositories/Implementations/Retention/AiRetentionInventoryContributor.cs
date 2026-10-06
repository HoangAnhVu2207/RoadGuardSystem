using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.DTOs.Retention;

namespace RoadGuardSystem.Repositories.Retention;

public sealed class AiRetentionInventoryContributor(RoadGuardDbContext db) : IRetentionInventoryContributor
{
    public string Name => "AI";
    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var sourceRuns = await (from file in db.Set<AiManifestFileReference>().AsNoTracking()
            join run in db.Set<AiMockRun>().AsNoTracking() on file.RunId equals run.Id
            where file.FileId == fileId select new { run.Id, run.ProjectId, run.ManifestHash, run.RowVersion }).ToArrayAsync(token);
        var frameRuns = await (from proof in db.Set<AiDetectionProvenance>().AsNoTracking()
            join run in db.Set<AiMockRun>().AsNoTracking() on proof.RunId equals run.Id
            where proof.FrameFileId == fileId select new { run.Id, run.ProjectId, run.ManifestHash, run.RowVersion }).ToArrayAsync(token);
        return new(Name, true, sourceRuns.Concat(frameRuns).DistinctBy(r => r.Id)
            .Select(r => new RetentionReferenceView("AI_PROVENANCE", r.Id, r.ProjectId,
                r.ManifestHash + ":" + Convert.ToBase64String(r.RowVersion))).ToArray(), []);
    }
    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var sourceIds = await (from file in db.Set<AiManifestFileReference>().AsNoTracking()
            join run in db.Set<AiMockRun>().AsNoTracking() on file.RunId equals run.Id where run.ProjectId == projectId
            select file.FileId).Distinct().ToArrayAsync(token);
        var frameIds = await (from proof in db.Set<AiDetectionProvenance>().AsNoTracking()
            join run in db.Set<AiMockRun>().AsNoTracking() on proof.RunId equals run.Id where run.ProjectId == projectId
            select proof.FrameFileId).Distinct().ToArrayAsync(token);
        return sourceIds.Concat(frameIds).Distinct().ToArray();
    }
}
