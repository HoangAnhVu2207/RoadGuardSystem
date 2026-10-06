using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Retention;

namespace RoadGuardSystem.Repositories.Implementations.Retention;

public sealed class PavementRetentionContributor(RoadGuardDbContext db) : IRetentionInventoryContributor
{
    public string Name => "HUY_FINAL_PAVEMENT";
    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var references = await (from reference in db.Set<PavementSourceFileReference>().AsNoTracking()
            join layout in db.Set<PavementLayoutRevision>().AsNoTracking() on reference.LayoutRevisionId equals layout.Id
            where reference.FileId == fileId
            orderby reference.Id
            select new { Reference = reference, Layout = layout }).ToArrayAsync(token);
        var checksum = await db.Files.AsNoTracking().Where(x => x.Id == fileId).Select(x => x.Checksum).SingleOrDefaultAsync(token);
        var complete = references.All(x => x.Reference.ContentChecksum == checksum);
        var facts = references.Select(x => new RetentionReferenceView("PAVEMENT_SOURCE_FILE", x.Reference.Id,
            x.Layout.ProjectId, Hash(new { x.Reference.Id, x.Reference.LayoutRevisionId, x.Reference.FileId,
                x.Reference.ContentChecksum, x.Reference.CaptureFactsJson, x.Layout.RouteVersionId,
                x.Layout.SegmentSetId, x.Layout.SourcePlanId, x.Layout.CrsProfileRevisionId, x.Layout.Kind,
                x.Layout.ContentHash, x.Layout.CreatedAt, x.Layout.CreatedBy }))).ToArray();
        return new(Name, complete, facts, complete ? [] : ["PAVEMENT_SOURCE_CONTENT_VERSION_UNRESOLVED"]);
    }
    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
        => await (from reference in db.Set<PavementSourceFileReference>().AsNoTracking()
            join layout in db.Set<PavementLayoutRevision>().AsNoTracking() on reference.LayoutRevisionId equals layout.Id
            where layout.ProjectId == projectId
            select reference.FileId).Distinct().OrderBy(x => x).ToArrayAsync(token);
    private static string Hash(object value)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
}
