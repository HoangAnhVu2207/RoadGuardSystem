using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

public sealed class CaseReportDetailsRepository(RoadGuardDbContext db, ICaseWorkflowRepository cases,
    IUploadRepository files) : ICaseReportDetailsRepository
{
    public Task<CaseReportRows> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, DateTimeOffset? beforeReceivedAt,
        Guid? beforeReportId, int take, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token)
        => InReadAsync(async () =>
        {
            await cases.GuardAsync(actor, role, [caseId], null, projectAccess, token);
            var query = from link in db.Set<HuyCaseReportLink>().AsNoTracking()
                        join report in db.Reports.AsNoTracking() on link.ReportId equals report.Id
                        where link.CaseId == caseId && link.EndedAt == null
                        select report;
            if (beforeReceivedAt is DateTimeOffset received && beforeReportId is Guid id)
                query = query.Where(r => r.ReceivedAt < received || r.ReceivedAt == received && r.Id.CompareTo(id) < 0);
            var heads = await query.OrderByDescending(r => r.ReceivedAt).ThenByDescending(r => r.Id)
                .Select(r => new { r.Id, r.ReceivedAt }).Take(take).ToArrayAsync(token);
            var ids = heads.Select(h => h.Id).ToArray();
            var reports = await db.Reports.AsNoTracking().AsSplitQuery().Where(r => ids.Contains(r.Id))
                .Include(r => r.OriginalEvidence).Include(r => r.Supplements).ThenInclude(s => s.Evidence)
                .ToDictionaryAsync(r => r.Id, token);
            var ownerIds = reports.Values.Select(r => r.ReporterUserId).Distinct().ToArray();
            var owners = await db.Users.AsNoTracking().Where(u => ownerIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName, u.PhoneNumber, u.PhoneNumberConfirmed })
                .ToDictionaryAsync(u => u.Id, token);
            var rows = heads.Select(h =>
            {
                var r = reports[h.Id];
                if (!owners.TryGetValue(r.ReporterUserId, out var owner))
                    throw new CaseWorkflowException(409, "source_not_ready");
                return new CaseReportRow(r.Id, r.ReporterUserId, owner.DisplayName, owner.PhoneNumber,
                    owner.PhoneNumberConfirmed, r.Description, r.ReceivedAt,
                    r.OriginalEvidence.OrderBy(e => e.Id).Select(Evidence).ToArray(),
                    r.Supplements.OrderBy(s => s.ReceivedAt).ThenBy(s => s.Id)
                        .Select(s => new CaseSupplementRow(s.Id, s.Description, s.ReceivedAt,
                            s.Evidence.OrderBy(e => e.Id).Select(Evidence).ToArray())).ToArray());
            }).ToArray();
            return new CaseReportRows(rows, rows.Length == take);
        }, token);

    public Task<CaseEvidenceContent> DownloadAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId,
        Guid evidenceId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token)
        => InReadAsync(async () =>
        {
            await cases.GuardAsync(actor, role, [caseId], null, projectAccess, token);
            if (!await db.Set<HuyCaseReportLink>().AsNoTracking().AnyAsync(l => l.CaseId == caseId &&
                l.ReportId == reportId && l.EndedAt == null, token))
                throw new CaseWorkflowException(404, "not_found");
            var report = await db.Reports.AsNoTracking().AsSplitQuery().Where(r => r.Id == reportId)
                .Include(r => r.OriginalEvidence).Include(r => r.Supplements).ThenInclude(s => s.Evidence)
                .SingleOrDefaultAsync(token);
            var evidence = report?.OriginalEvidence.Concat(report.Supplements.SelectMany(s => s.Evidence))
                .SingleOrDefault(e => e.Id == evidenceId);
            if (evidence is null) throw new CaseWorkflowException(404, "not_found");
            var file = await files.GetFileMetadataAsync(evidence.FileId, token);
            if (file is null || file.OwnerUserId != report!.ReporterUserId || file.ProjectId is not null ||
                file.TargetId is not null || file.Purpose != "REPORT_PHOTO")
                throw new CaseWorkflowException(404, "not_found");
            if (file.Status != "VERIFIED") throw new CaseWorkflowException(409, "source_not_ready");
            if (file.Version != evidence.FileVersion) throw new CaseWorkflowException(412, "concurrency_conflict");
            var stream = await files.OpenFileAsync(file.ObjectKey, token);
            return new CaseEvidenceContent(stream, file.MediaType, file.Version);
        }, token);

    private static CaseEvidenceRow Evidence(ReportEvidence e) => new(e.Id, e.FileId, e.FileVersion, e.SupplementId,
        e.CaptureMetadata?.CapturedAt, e.CaptureMetadata?.LocationSource.ToString().ToUpperInvariant() ?? "UNKNOWN",
        e.CaptureMetadata?.Latitude, e.CaptureMetadata?.Longitude, e.CaptureMetadata?.AccuracyMeters);

    private Task<T> InReadAsync<T>(Func<Task<T>> action, CancellationToken token) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            var result = await action();
            await transaction.CommitAsync(token);
            return result;
        });
}
