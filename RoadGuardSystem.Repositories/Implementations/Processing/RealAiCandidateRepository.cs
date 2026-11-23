using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.Repositories.Implementations.Processing;

public sealed class RealAiCandidateRepository(RoadGuardDbContext db, IAiVideoWorkRepository videos) : IRealAiCandidateRepository
{
    public async Task<RealAiCandidateReadFacts?> ReadAsync(Guid projectId, Guid detectionId, CancellationToken ct)
    {
        var row = await (from d in db.AIDetections.AsNoTracking()
            join j in db.ProcessingJobs.AsNoTracking() on d.ProcessingJobId equals j.Id
            join w in db.Set<AiVideoWorkItem>().AsNoTracking() on j.Id equals w.ProcessingJobId
            join a in db.ProcessingAttempts.AsNoTracking() on w.CurrentAttemptId equals a.Id
            join b in db.ProcessingBlocks.AsNoTracking() on j.ProcessingBlockId equals b.Id
            where d.Id == detectionId && j.ProjectId == projectId && w.ProjectId == projectId && j.Mode == "REAL" &&
                j.Status == ProcessingJobStatus.Completed && w.State == AiVideoWorkState.Ingested && d.ModelVersionId == j.ModelVersionId &&
                a.ProcessingJobId == j.Id && b.SurveyDataVersionId == w.DatasetId &&
                !db.ProcessingAttempts.Any(later => later.ProcessingJobId == j.Id && later.AttemptNo > a.AttemptNo)
            select new { Detection = d, Job = j, Work = w }).SingleOrDefaultAsync(ct);
        if (row is null) return null;
        AiVideoSource source;
        try { source = await videos.ReadSourceAsync(row.Work, ct); }
        catch (InvalidOperationException) { return null; }
        var artifacts = await (from p in db.Set<AiWorkerArtifact>().AsNoTracking()
            join f in db.Files.AsNoTracking() on p.FileId equals (Guid?)f.Id
            where p.WorkItemId == row.Work.Id && p.AttemptId == row.Work.CurrentAttemptId
            select new RealAiArtifactReadFact(p, f)).ToArrayAsync(ct);
        var disposition = await (from head in db.Set<HuyCandidateSourceHead>().AsNoTracking()
            join decision in db.SourceDecisions.AsNoTracking() on head.DecisionId equals decision.Id
            where head.SourceKind == CandidateSourceKind.AiDetection && head.SourceId == detectionId && head.ProjectId == projectId
            select new { head.DecisionId, Version = EF.Property<byte[]>(decision, "RowVersion") }).SingleOrDefaultAsync(ct);
        var version = disposition is null ? null : Convert.ToBase64String(disposition.Version);
        var revisions = await db.AiVideoTelemetryRevisions.AsNoTracking().Where(x => x.WorkId == row.Work.Id).OrderBy(x => x.Number).ToArrayAsync(ct);
        AiPairedTelemetryFile? paired;
        try { paired = await new AiVideoTelemetryRepository(db, TimeProvider.System).ReadPairedAsync(row.Work, source.OwnerId, ct); }
        catch (AiRequestException) { return null; }
        var pairsManifest = await db.SurveyDataVersions.AsNoTracking().Where(x => x.Id == row.Work.DatasetId).Select(x => x.PairsManifest).SingleAsync(ct);
        var pairsHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pairsManifest ?? ""))).ToLowerInvariant();
        return new(row.Work, row.Job, row.Detection, source, artifacts,
            disposition is null ? null : new(disposition.DecisionId, version!), version, revisions, paired, pairsHash);
    }
    public Task<Guid?> DetectionProjectAsync(Guid detectionId, CancellationToken ct)
        => (from d in db.AIDetections.AsNoTracking() join j in db.ProcessingJobs.AsNoTracking() on d.ProcessingJobId equals j.Id
            where d.Id == detectionId && j.Mode == "REAL" select (Guid?)j.ProjectId).SingleOrDefaultAsync(ct);
}
