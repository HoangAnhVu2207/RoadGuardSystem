using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Retention;

// Anh read-side integration of handed-off intake tables. This deliberately does
// not replace Huy's complete candidate/publication/label/repair inventory producer.
public sealed class ReporterIntakeRetentionInventoryContributor(RoadGuardDbContext db) : IRetentionInventoryContributor
{
    public string Name => "REPORTER_INTAKE";
    public async Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token)
    {
        var reports = await db.Reports.AsNoTracking()
            .Where(r => r.OriginalEvidence.Any(e => e.FileId == fileId) || r.Supplements.Any(s => s.Evidence.Any(e => e.FileId == fileId)))
            .Include(r => r.OriginalEvidence).Include(r => r.Supplements).ThenInclude(s => s.Evidence)
            .Select(r => new { Report = r, Version = EF.Property<byte[]>(r, "RowVersion") }).ToArrayAsync(token);
        var ids = reports.Select(r => r.Report.Id).ToArray();
        var refs = new List<RetentionReferenceView>();
        foreach (var row in reports)
        {
            refs.Add(new("REPORT", row.Report.Id, null, Convert.ToBase64String(row.Version)));
            refs.AddRange(row.Report.OriginalEvidence.Where(e => e.FileId == fileId).Select(e => new RetentionReferenceView("REPORT_EVIDENCE", e.Id, null, e.FileVersion)));
            foreach (var supplement in row.Report.Supplements)
            {
                var evidence = supplement.Evidence.Where(e => e.FileId == fileId).OrderBy(e => e.Id).ToArray();
                if (evidence.Length == 0) continue;
                refs.Add(new("REPORT_SUPPLEMENT", supplement.Id, null, RetentionInventoryRepository.Hash(new { supplement.Id, supplement.ReportId, supplement.ReceivedAt, Evidence = evidence.Select(e => new { e.Id, e.FileId, e.FileVersion }) })));
                refs.AddRange(evidence.Select(e => new RetentionReferenceView("REPORT_SUPPLEMENT_EVIDENCE", e.Id, null, e.FileVersion)));
            }
        }
        // Retention includes closed links too: relinking cannot erase an obligation.
        var links = await (from l in db.Set<HuyCaseReportLink>().AsNoTracking()
            join c in db.IncidentCases.AsNoTracking() on l.CaseId equals c.Id
            where ids.Contains(l.ReportId)
            select new { l.Id, l.ReportId, l.CaseId, l.StartedAt, l.EndedAt, c.ProjectId, CaseVersion = EF.Property<byte[]>(c, "RowVersion") }).ToArrayAsync(token);
        refs.AddRange(links.Select(l => new RetentionReferenceView("CASE_REPORT_LINK", l.Id, l.ProjectId,
            RetentionInventoryRepository.Hash(new { l.ReportId, l.CaseId, l.StartedAt, l.EndedAt, l.ProjectId, CaseVersion = Convert.ToBase64String(l.CaseVersion) }))));
        // Complete only for this named intake-table slice. The composite still
        // requires a separate HUY contributor and stays fail-closed without it.
        return new(Name, true, refs, []);
    }

    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var reports = from r in db.Reports.AsNoTracking()
            where db.Set<HuyCaseReportLink>().Any(l => l.ReportId == r.Id && db.IncidentCases.Any(c => c.Id == l.CaseId && c.ProjectId == projectId))
            select r;
        var original = await reports.SelectMany(r => r.OriginalEvidence).Select(e => e.FileId).Distinct().ToArrayAsync(token);
        var supplemental = await reports.SelectMany(r => r.Supplements).SelectMany(s => s.Evidence).Select(e => e.FileId).Distinct().ToArrayAsync(token);
        return original.Concat(supplemental).Distinct().Order().ToArray();
    }
}
