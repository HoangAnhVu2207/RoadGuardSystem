using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Integration;

public sealed class AnhHuyFactsRepository(RoadGuardDbContext db) : IAnhHuyFactsRepository
{
    public async Task<bool> IsCurrentActorAsync(Guid actorId, UserRoleCode role, CancellationToken ct)
    {
        return await db.Users.AsNoTracking().AnyAsync(u =>
                   u.Id == actorId && u.Status == UserStatus.Active && u.RoleCode == role && !u.MustChangePassword, ct)
               && await db.Roles.AsNoTracking().AnyAsync(r => r.Code == role && r.IsActive, ct);
    }

    public async Task<ReporterFileFacts?> GetFileAsync(Guid fileId, CancellationToken ct)
    {
        var row=await (from f in db.Files.AsNoTracking() join scope in db.FileScopes.AsNoTracking() on f.Id equals scope.FileId
            join upload in db.UploadSessions.AsNoTracking() on f.Id equals upload.FileId
            where f.Id==fileId select new{f,scope,upload}).SingleOrDefaultAsync(ct);
        return row is null?null:new(row.f.Id,row.scope.OwnerUserId,row.scope.ProjectId,row.scope.Purpose,
            row.upload.Status.ToString().ToUpperInvariant(),Convert.ToBase64String(row.upload.RowVersion),row.f.Checksum,
            row.f.SizeBytes,row.f.MimeType,row.f.UploadedAt);
    }

    public async Task<PublicationFileFacts?> GetPublicationFileAsync(Guid actorId, Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken ct)
    {
        var projection=await (from e in db.Set<HuyPublicationEvidence>().AsNoTracking()
            join recipient in db.Set<HuyPublicationRecipient>().AsNoTracking()
                on new{e.PublicationId,ReportId=e.RecipientReportId} equals new{recipient.PublicationId,recipient.ReportId}
            join publication in db.Set<CasePublication>().AsNoTracking() on e.PublicationId equals publication.Id
            join report in db.Set<Report>().AsNoTracking() on recipient.ReportId equals report.Id
            where e.PublicationId==publicationId && e.RecipientReportId==reportId && e.EvidenceId==evidenceId && report.ReporterUserId==actorId
            select new{e.SourceReportId,e.OriginalEvidenceId,e.SupplementEvidenceId}).SingleOrDefaultAsync(ct);
        if(projection is null) return null;
        if(projection.OriginalEvidenceId is Guid original)
            return await db.Set<Report>().Where(r=>r.Id==projection.SourceReportId).SelectMany(r=>r.OriginalEvidence)
                .Where(e=>e.Id==original && e.Id==evidenceId).Select(e=>new PublicationFileFacts(e.FileId,e.FileVersion,e.OwnerUserId)).SingleOrDefaultAsync(ct);
        return await db.Set<ReportSupplement>().Where(s=>s.ReportId==projection.SourceReportId).SelectMany(s=>s.Evidence)
            .Where(e=>e.Id==projection.SupplementEvidenceId && e.Id==evidenceId).Select(e=>new PublicationFileFacts(e.FileId,e.FileVersion,e.OwnerUserId)).SingleOrDefaultAsync(ct);
    }

    public async Task<ReportCandidateFacts?> GetReportSourceAsync(Guid reportId, CancellationToken ct)
    {
        var row=await (from report in db.Set<Report>().AsNoTracking()
            join link in db.Set<HuyCaseReportLink>().AsNoTracking() on report.Id equals link.ReportId
            join incident in db.Set<IncidentCase>().AsNoTracking() on link.CaseId equals incident.Id
            where report.Id==reportId && link.EndedAt==null
            select new{report.Id,report.ReporterUserId,CaseId=incident.Id,incident.ProjectId,
                Route=EF.Property<Guid?>(incident,"GeometryRouteVersionId"),Set=EF.Property<Guid?>(incident,"GeometrySegmentSetId"),
                ReportVersion=EF.Property<byte[]>(report,"RowVersion"),CaseVersion=EF.Property<byte[]>(incident,"RowVersion")}).SingleOrDefaultAsync(ct);
        if(row is null) return null;
        // Materialize each owned navigation before combining; EF cannot UNION a
        // client-constructed record projection across two different owned tables.
        var original=await db.Set<Report>().Where(r=>r.Id==reportId).SelectMany(r=>r.OriginalEvidence)
            .Select(e=>new ReportSourceEvidence(e.Id,e.FileId,e.FileVersion)).ToArrayAsync(ct);
        var supplements=await db.Set<ReportSupplement>().Where(s=>s.ReportId==reportId).SelectMany(s=>s.Evidence)
            .Select(e=>new ReportSourceEvidence(e.Id,e.FileId,e.FileVersion)).ToArrayAsync(ct);
        var evidence=original.Concat(supplements).ToArray();
        var disposition=await (from head in db.Set<HuyCandidateSourceHead>().AsNoTracking()
            join decision in db.Set<CandidateDecision>().AsNoTracking() on head.DecisionId equals decision.Id
            where head.SourceKind==CandidateSourceKind.Report && head.SourceId==reportId
            select new{head.DecisionId,HeadVersion=head.RowVersion,Version=EF.Property<byte[]>(decision,"RowVersion")}).SingleOrDefaultAsync(ct);
        return new(row.Id,row.ReporterUserId,row.CaseId,row.ProjectId,row.Route,row.Set,Convert.ToBase64String(row.ReportVersion),
            Convert.ToBase64String(row.CaseVersion),disposition is null?null:new ActiveCandidateDisposition(disposition.DecisionId,Convert.ToBase64String(disposition.Version)),
            disposition is null?null:Convert.ToBase64String(disposition.HeadVersion),evidence);
    }
}
