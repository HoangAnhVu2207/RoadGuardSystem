using System.Data;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository : IRepairSourceEvidenceRepository
{
    public async Task<RepairSourceEvidenceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid package, Guid item, Guid? evidence, CancellationToken token)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                await GuardAsync(actor, role, project, package, item, false, token);
                var work = await db.RepairItems.AsNoTracking().SingleAsync(row => row.Id == item, token);
                var links = await db.Set<HuyDefectSourceLink>().AsNoTracking().Where(row =>
                    row.DefectId == work.DefectId && row.ProjectId == work.ProjectId && row.EndedAt == null)
                    .OrderBy(row => row.Id).ToArrayAsync(token);
                var reportIds = links.Where(row => row.ReportSourceId is not null).Select(row => row.ReportSourceId!.Value).Distinct().ToArray();
                var originals = await db.Reports.AsNoTracking().Where(row => reportIds.Contains(row.Id))
                    .SelectMany(row => row.OriginalEvidence).Where(row => evidence == null || row.Id == evidence).ToArrayAsync(token);
                var supplements = await db.ReportSupplements.AsNoTracking().Where(row => reportIds.Contains(row.ReportId))
                    .SelectMany(row => row.Evidence).Where(row => evidence == null || row.Id == evidence).ToArrayAsync(token);
                var result = new List<RepairSourceEvidenceFact>();
                var missing = new HashSet<string>(StringComparer.Ordinal);
                foreach (var source in originals.Concat(supplements).OrderBy(row => row.Id))
                {
                    // Current Report–Case link must still remain in the origin project.
                    if (!await (from link in db.Set<HuyCaseReportLink>().AsNoTracking()
                                join incident in db.IncidentCases.AsNoTracking() on link.CaseId equals incident.Id
                                where link.ReportId == source.ReportId && link.EndedAt == null && incident.ProjectId == work.ProjectId
                                select link.Id).AnyAsync(token)) { missing.Add("REPORT_SOURCE_SCOPE_UNAVAILABLE"); continue; }
                    var file = await new AnhHuyFactsRepository(db).GetFileAsync(source.FileId, token);
                    if (file is null || file.State != "VERIFIED" || file.ProjectId is not null || file.Purpose != "REPORT_PHOTO" ||
                        file.OwnerId != source.OwnerUserId || file.FileVersion != source.FileVersion)
                    { missing.Add("REPORT_EVIDENCE_UNAVAILABLE"); continue; }
                    var stored = await db.Files.AsNoTracking().SingleAsync(row => row.Id == source.FileId, token);
                    result.Add(new(source.Id, "REPORT", source.CaptureMetadata?.CapturedAt,
                        new(file.FileId, "VERIFIED", file.Checksum, file.MediaType, file.SizeBytes, stored.StorageUri)));
                }
                foreach (var source in links.Where(row => row.SourceKind == RoadGuardSystem.BusinessObjects.Candidates.CandidateSourceKind.AiDetection))
                {
                    var proof = await Anh02AiRepository.ReadCandidateCoreAsync(db, work.ProjectId, source.SourceId, token);
                    if (proof is null) { missing.Add("AI_SOURCE_UNAVAILABLE"); continue; }
                    if (evidence is not null && evidence != proof.Proof.FrameFileId) continue;
                    // Generated frames have no upload/FileScope row. Their existing
                    // authoritative scope is the completed run/result/proof graph.
                    if (proof.Run.ResultId != proof.Result.Id || proof.Result.AttemptId != proof.Run.AttemptId ||
                        proof.Proof.FrameFileVersion != proof.FrameSha256) { missing.Add("AI_SOURCE_UNAVAILABLE"); continue; }
                    var defectRoute = await db.Defects.AsNoTracking().Where(row => row.Id == work.DefectId && row.ProjectId == work.ProjectId)
                        .Select(row => row.RoadSectionVersionId).SingleAsync(token);
                    if (proof.Run.RouteVersionId != defectRoute) { missing.Add("AI_SOURCE_UNAVAILABLE"); continue; }
                    var frame = await db.Files.AsNoTracking().SingleAsync(row => row.Id == proof.Proof.FrameFileId, token);
                    result.Add(new(frame.Id, "AI_FRAME", null,
                        new(frame.Id, "VERIFIED", proof.FrameSha256, proof.FrameMediaType, proof.FrameBytes, frame.StorageUri), proof));
                }
                await tx.CommitAsync(token);
                return new RepairSourceEvidenceResult(200, Items: result.DistinctBy(row => row.EvidenceId).ToArray(),
                    MissingReasons: missing.Order(StringComparer.Ordinal).ToArray());
            });
        }
        catch (Denied error) { db.ChangeTracker.Clear(); return new(error.Result.Status, error.Result.Code); }
    }
}
