using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Reports;

namespace RoadGuardSystem.Repositories.Implementations.Reports;

public sealed class ReporterLifecycleException(int status, string code) : Exception
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public sealed class ReporterLifecycleRepository(RoadGuardDbContext db, IReporterReportRepository intake) : IReporterLifecycleRepository
{
    public Task<OwnReportFact> ReadAsync(Guid actorId, Guid reportId, CancellationToken ct)
        => InReadTransactionAsync(async () => { await GuardAsync(actorId, reportId, [], ct); return await ProjectAsync(reportId, ct); }, ct);

    public Task<OwnReportPageFact> ListAsync(Guid actorId, int pageSize, string? cursor, CancellationToken ct)
        => InReadTransactionAsync(async () =>
        {
            await intake.EnsureCurrentReceiptAccessAsync(actorId, [], ct);
            var query = db.Reports.AsNoTracking().Where(r => r.ReporterUserId == actorId);
            if (cursor is not null)
            {
                PageCursor value;
                try { value = JsonSerializer.Deserialize<PageCursor>(Convert.FromBase64String(cursor))!; }
                catch (Exception e) when (e is FormatException or JsonException) { throw new ReporterLifecycleException(400, "validation_error"); }
                if (value is null || value.ActorId != actorId || value.Id == Guid.Empty) throw new ReporterLifecycleException(400, "validation_error");
                query = query.Where(r => r.ReceivedAt < value.CreatedAt || r.ReceivedAt == value.CreatedAt && r.Id.CompareTo(value.Id) < 0);
            }
            var rows = await query.OrderByDescending(r => r.ReceivedAt).ThenByDescending(r => r.Id).Take(pageSize + 1)
                .Select(r => new { r.Id, r.ReceivedAt }).ToArrayAsync(ct);
            var items = new List<OwnReportFact>();
            foreach (var row in rows.Take(pageSize)) items.Add(await ProjectAsync(row.Id, ct));
            var last = rows.Take(pageSize).LastOrDefault();
            return new OwnReportPageFact(items, rows.Length > pageSize && last is not null
                ? Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new PageCursor(actorId, last.ReceivedAt, last.Id))) : null);
        }, ct);

    public async Task GuardAsync(Guid actorId, Guid reportId, IReadOnlyList<VerifiedEvidenceReference> evidence, CancellationToken ct)
    {
        await intake.EnsureCurrentReceiptAccessAsync(actorId, [], ct);
        await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Reports] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", reportId, ct);
        if (!await db.Reports.AsNoTracking().AnyAsync(r => r.Id == reportId && r.ReporterUserId == actorId, ct))
            throw new ReporterLifecycleException(404, "not_found");
        await intake.EnsureCurrentReceiptAccessAsync(actorId, evidence, ct);
    }

    public async Task<OwnReportFact> SupplementAsync(Guid actorId, Guid reportId, string expectedVersion, string description,
        IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken ct)
    {
        // Actor, case, report, files; re-check the active link after locking.
        await intake.EnsureCurrentReceiptAccessAsync(actorId, [], ct);
        if (!await db.Reports.AsNoTracking().AnyAsync(r => r.Id == reportId && r.ReporterUserId == actorId, ct))
            throw new ReporterLifecycleException(404, "not_found");
        var link = await db.Set<HuyCaseReportLink>().AsNoTracking().SingleAsync(l => l.ReportId == reportId && l.EndedAt == null, ct);
        await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [IncidentCases] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", link.CaseId, ct);
        await GuardAsync(actorId, reportId, evidence, ct);
        if (!await db.Set<HuyCaseReportLink>().AsNoTracking().AnyAsync(l => l.ReportId == reportId && l.CaseId == link.CaseId && l.EndedAt == null, ct))
            throw new ReporterLifecycleException(412, "concurrency_conflict");
        var report = await db.Reports.Include(r => r.Supplements).ThenInclude(s => s.Evidence).SingleAsync(r => r.Id == reportId, ct);
        if (Convert.ToBase64String(db.Entry(report).Property<byte[]>("RowVersion").CurrentValue!) != expectedVersion)
            throw new ReporterLifecycleException(412, "concurrency_conflict");
        var incident = await db.IncidentCases.SingleAsync(c => c.Id == link.CaseId, ct);
        var now = DateTimeOffset.UtcNow;
        report.AddSupplement(Guid.NewGuid(), description, evidence, now);
        incident.RegisterSupplement(reportId, now);
        db.Entry(report).Property<long>("Revision").CurrentValue++;
        db.Entry(incident).Property<long>("Revision").CurrentValue++;
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actorId, now, "report_supplemented", "Report", reportId,
            null, JsonSerializer.Serialize(new { reportId, caseId = incident.Id }), "Reporter supplement appended",
            "huy01.reporter-supplement", correlationId, ["reportId", "caseId"]));
        await db.SaveChangesAsync(ct);
        return await ProjectAsync(reportId, ct);
    }

    public Task<(Guid FileId, Guid? PublicationId, string? FileVersion)> ResolveDownloadAsync(Guid actorId, Guid reportId, Guid evidenceId, CancellationToken ct)
        => InReadTransactionAsync(async () =>
        {
            await GuardAsync(actorId, reportId, [], ct);
            var report = await db.Reports.AsNoTracking().Include(r => r.Supplements).ThenInclude(s => s.Evidence).SingleAsync(r => r.Id == reportId, ct);
            var own = report.OriginalEvidence.Concat(report.Supplements.SelectMany(s => s.Evidence)).SingleOrDefault(e => e.Id == evidenceId);
            if (own is not null) return (own.FileId, (Guid?)null, (string?)own.FileVersion);
            var projection = await db.Set<HuyPublicationEvidence>().AsNoTracking()
                .Where(e => e.RecipientReportId == reportId && e.EvidenceId == evidenceId)
                .OrderBy(e => e.PublicationId).FirstOrDefaultAsync(ct);
            if (projection is null) throw new ReporterLifecycleException(404, "not_found");
            return (Guid.Empty, (Guid?)projection.PublicationId, (string?)null);
        }, ct);

    private async Task<OwnReportFact> ProjectAsync(Guid reportId, CancellationToken ct)
    {
        var report = await db.Reports.AsNoTracking().Include(r => r.Supplements).ThenInclude(s => s.Evidence).SingleAsync(r => r.Id == reportId, ct);
        var version = await db.Reports.Where(r => r.Id == reportId).Select(r => EF.Property<byte[]>(r, "RowVersion")).SingleAsync(ct);
        var routing = await (from link in db.Set<HuyCaseReportLink>()
                             join incident in db.IncidentCases on link.CaseId equals incident.Id
                             where link.ReportId == reportId && link.EndedAt == null
                             select incident.ProjectId).SingleAsync(ct);
        var updates = await (from recipient in db.Set<HuyPublicationRecipient>()
                             join p in db.Set<CasePublication>() on recipient.PublicationId equals p.Id
                             where recipient.ReportId == reportId
                             orderby p.PublishedAt, p.Id
                             select p).AsNoTracking().ToArrayAsync(ct);
        return new(report.Id, report.Description, report.ReceivedAt, Convert.ToBase64String(version),
            report.OriginalEvidence.Concat(report.Supplements.OrderBy(s => s.ReceivedAt).ThenBy(s => s.Id).SelectMany(s => s.Evidence))
                .Select(e => new OwnReportEvidenceFact(e.Id, e.FileId, e.FileVersion, e.SupplementId, e.CaptureMetadata?.CapturedAt,
                    e.CaptureMetadata?.LocationSource.ToString().ToUpperInvariant() ?? "UNKNOWN", e.CaptureMetadata?.Latitude is decimal lat
                        ? new ReportEvidenceLocationFact(lat, e.CaptureMetadata.Longitude, e.CaptureMetadata.AccuracyMeters) : null)).ToArray(),
            routing is null ? "UNASSIGNED" : "ASSIGNED",
            updates.Select(p => new OwnReportUpdateFact(p.Id, p.Summary, p.PublishedAt, p.EvidenceIds)).ToArray(),
            report.Supplements.OrderBy(s => s.ReceivedAt).ThenBy(s => s.Id).Select(s => new OwnReportSupplementFact(s.Id, s.Description, s.ReceivedAt)).ToArray());
    }

    private Task<int> LockAsync(string sql, Guid id, CancellationToken ct) => db.Database.SqlQueryRaw<int>(sql, id).SingleAsync(ct);
    private Task<T> InReadTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await action(); await transaction.CommitAsync(ct); return result;
        });
    private sealed record PageCursor(Guid ActorId, DateTimeOffset CreatedAt, Guid Id);
}
