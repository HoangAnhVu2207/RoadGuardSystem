using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Repositories.Implementations.Reports;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

internal sealed class ReporterRepairProgressProbe(DbContextOptions<RoadGuardDbContext> options, Guid pm, Guid caseId, Guid reportId, Guid defectId, Guid owner)
{
    internal static async Task<ReporterRepairProgressProbe> CreateAsync(RoadGuardDbContext db, H4GenuineRepairSource.Source source)
    {
        var incident = await db.IncidentCases.AsNoTracking().SingleAsync(row => row.ProjectId == source.Project);
        var report = Assert.Single(incident.ActiveReportIds);
        var owner = await db.Reports.Where(row => row.Id == report).Select(row => row.ReporterUserId).SingleAsync();
        await db.Defects.Where(row => row.Id == source.Defect)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, DefectStatus.Verified));
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(db.Database.GetConnectionString(), sql => sql.UseNetTopologySuite()).Options;
        return new(options, source.Pm, incident.Id, report, source.Defect, owner);
    }

    internal async Task PublishAsync(string status, int expected = 201)
    {
        await using var db = new RoadGuardDbContext(options);
        var repo = new CaseWorkflowRepository(db);
        static Task<bool> AllowProject(Guid project, CancellationToken token) => Task.FromResult(true);
        var snapshot = await repo.ReadAsync(pm, UserRoleCode.ProjectManager, caseId, AllowProject, default);
        var command = new CaseCommand(caseId, snapshot.Version, "publish", "Verified progress", ReportIds: [reportId],
            DefectIds: [defectId], EvidenceIds: [], RepairProgressStatus: status);
        try
        {
            var result = await new IdempotencyOperationService(db).ExecuteSerializableAsync(pm, null, "test.reporter-progress",
                Guid.NewGuid().ToString(), new string('a', 64), async token =>
                {
                    var written = await repo.ApplyAsync(pm, UserRoleCode.ProjectManager, command, AllowProject, null, null, token);
                    return (written.Publication!.Id, "{}");
                });
            Assert.Equal(201, expected);
            Assert.NotEqual(Guid.Empty, result.OperationId);
        }
        catch (CaseWorkflowException error)
        {
            Assert.Equal(expected, error.Status);
            Assert.Equal("repair_progress_not_ready", error.Code);
        }
    }

    internal async Task ReadAsync(string? expected)
    {
        await using var db = new RoadGuardDbContext(options);
        var reader = new ReporterLifecycleRepository(db, new ReporterReportRepository(db));
        Assert.Equal(expected, (await reader.ReadAsync(owner, reportId, default)).RepairProgress?.Status);
        Assert.Equal(expected, Assert.Single((await reader.ListAsync(owner, 20, null, default)).Items).RepairProgress?.Status);
    }
}
