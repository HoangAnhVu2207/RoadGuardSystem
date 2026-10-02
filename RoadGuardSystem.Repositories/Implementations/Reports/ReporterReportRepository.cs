using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Reports;

namespace RoadGuardSystem.Repositories.Implementations.Reports;

public sealed class ReporterReportRepository(RoadGuardDbContext context) : IReporterReportRepository
{
    public async Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
        IReadOnlyList<VerifiedEvidenceReference> evidence, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var report = Report.Create(Guid.NewGuid(), reporterUserId, description, now,
            evidence);
        var intakeCase = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);

        context.Reports.Add(report);
        context.IncidentCases.Add(intakeCase);
        context.Set<HuyCaseReportLink>().Add(new HuyCaseReportLink
        {
            Id = Guid.NewGuid(), CaseId = intakeCase.Id, ReportId = report.Id, StartedAt = now
        });
        context.Entry(report).Property<long>("Revision").CurrentValue = 1;
        context.Entry(intakeCase).Property<long>("Revision").CurrentValue = 1;
        await context.SaveChangesAsync(cancellationToken);

        var version = Convert.ToBase64String(context.Entry(report).Property<byte[]>("RowVersion").CurrentValue!);
        return new ReporterReportWriteResult(report, version);
    }
}
