using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention;
namespace RoadGuardSystem.Repositories.Retention;

public sealed class ExportRetentionInventoryContributor(RoadGuardDbContext context) : IRetentionInventoryContributor
{
    public string Name => "EXPORT";
    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var refs = new List<RetentionReferenceViewFact>(); var bounds = new List<DateTimeOffset?>(); var reasons = new List<string>();
        var sources = await (from f in context.Set<ExportSnapshotFile>().AsNoTracking() join s in context.Set<ExportSnapshot>().AsNoTracking() on f.SnapshotId equals s.Id where f.FileId == fileId select new { f, s }).ToListAsync(token);
        refs.AddRange(sources.Select(x => new RetentionReferenceViewFact("EXPORT_SNAPSHOT_SOURCE", x.f.Id, x.s.ProjectId, RetentionInventoryRepository.Hash(new { x.s.Hash, x.f.FileVersion, x.f.Sha256, x.f.SizeBytes, x.f.Included }))));
        var outputs = await (from a in context.Set<GeneratedArtifact>().AsNoTracking() join j in context.Set<ExportJob>().AsNoTracking() on a.ExportJobId equals j.Id where a.FileId == fileId select new { a, j }).ToListAsync(token);
        foreach (var row in outputs)
        {
            refs.Add(new("GENERATED_EXPORT", row.a.Id, row.j.ProjectId, RetentionInventoryRepository.Hash(new { row.a.SnapshotId, row.a.CreatedAt, JobVersion = Convert.ToBase64String(row.j.RowVersion), row.j.CompletedAt, row.j.Status })));
            var confirmed = row.j.Status == "SUCCEEDED" && row.j.ArtifactId == row.a.Id && row.j.CompletedAt.HasValue;
            bounds.Add(confirmed ? row.j.CompletedAt!.Value.AddDays(30) : null);
            if (!confirmed) reasons.Add("EXPORT_COMPLETION_UNRESOLVED");
        }
        return new(Name, reasons.Count == 0, refs, reasons, bounds);
    }
    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var files = await (from f in context.Set<ExportSnapshotFile>().AsNoTracking() join s in context.Set<ExportSnapshot>().AsNoTracking() on f.SnapshotId equals s.Id where s.ProjectId == projectId select f.FileId).ToListAsync(token);
        files.AddRange(await (from a in context.Set<GeneratedArtifact>().AsNoTracking() join j in context.Set<ExportJob>().AsNoTracking() on a.ExportJobId equals j.Id where j.ProjectId == projectId select a.FileId).ToListAsync(token));
        return files.Distinct().ToArray();
    }
}
