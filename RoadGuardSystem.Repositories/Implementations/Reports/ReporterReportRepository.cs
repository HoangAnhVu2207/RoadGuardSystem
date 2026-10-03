using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Reports;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Reports;

public sealed class ReporterReportRepository(RoadGuardDbContext context) : IReporterReportRepository
{
    public async Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
        IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        await EnsureCurrentReceiptAccessAsync(reporterUserId, evidence, cancellationToken);
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
        context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), reporterUserId, now, "report_received", "Report", report.Id,
            null, System.Text.Json.JsonSerializer.Serialize(new { report.Id, intakeCaseId = intakeCase.Id, evidenceCount = evidence.Count }),
            "Reporter intake created", "huy01.reporter-intake", correlationId, ["id", "intakeCaseId", "evidenceCount"]));
        context.Entry(report).Property<long>("Revision").CurrentValue = 1;
        context.Entry(intakeCase).Property<long>("Revision").CurrentValue = 1;
        await context.SaveChangesAsync(cancellationToken);

        var version = Convert.ToBase64String(context.Entry(report).Property<byte[]>("RowVersion").CurrentValue!);
        return new ReporterReportWriteResult(report, version);
    }

    public async Task EnsureCurrentReceiptAccessAsync(Guid reporterUserId, IReadOnlyList<VerifiedEvidenceReference> evidence,
        CancellationToken cancellationToken = default)
    {
        await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {0}", reporterUserId, cancellationToken);
        var actor = await context.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == reporterUserId, cancellationToken);
        if (actor is null) throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.NotFound);
        if (actor.Status != RoadGuardSystem.aBusinessObjects.Commons.UserStatus.Active || actor.RoleCode != RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Reporter || actor.MustChangePassword)
            throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.Forbidden);
        var roleCode = actor.RoleCode.ToDbCode();
        var role = await context.Roles.FromSqlInterpolated($"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={roleCode}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (role is not { IsActive: true } || role.Code != RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Reporter)
            throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.Forbidden);
        foreach (var item in evidence.OrderBy(reference => reference.FileId))
        {
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {0}", item.FileId, cancellationToken);
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId] = {0}", item.FileId, cancellationToken);
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId] = {0}", item.FileId, cancellationToken);
            var facts = await (from file in context.Files.AsNoTracking()
                               join scope in context.FileScopes.AsNoTracking() on file.Id equals scope.FileId
                               join upload in context.UploadSessions.AsNoTracking() on file.Id equals upload.FileId
                               where file.Id == item.FileId
                               select new { scope, upload }).SingleOrDefaultAsync(cancellationToken);
            if (facts is null || facts.scope.OwnerUserId != reporterUserId || facts.scope.ProjectId is not null || facts.scope.TargetId is not null || facts.scope.Purpose != "REPORT_PHOTO" || item.OwnerUserId != reporterUserId)
                throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.NotFound);
            if (facts.upload.Status != UploadSessionStatus.Verified) throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.SourceNotReady);
            if (!string.Equals(Convert.ToBase64String(facts.upload.RowVersion), item.FileVersion, StringComparison.Ordinal))
                throw new ReporterIntakeFactsException(ReporterIntakeFactsStatus.StaleFile);
        }
    }

    private Task<int> LockAsync(string sql, Guid id, CancellationToken cancellationToken)
        => context.Database.SqlQueryRaw<int>(sql, id).SingleAsync(cancellationToken);
}
