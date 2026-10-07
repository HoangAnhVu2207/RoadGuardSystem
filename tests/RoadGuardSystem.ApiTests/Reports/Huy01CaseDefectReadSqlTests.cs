using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Services.Implementations.Integration;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

[Trait("Package", "HUY-01")]
public sealed class Huy01CaseDefectReadSqlTests(AuthenticationSqlServerFixture fixture)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task Capture_UsesPersistedVersionsAndConclusionsAndFailsClosedAfterRoleRevocation()
    {
        var pm = await fixture.CreateUserAsync($"case-read-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var reporter = await fixture.CreateUserAsync($"case-source-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var now = DateTimeOffset.UtcNow;
        var projectId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var conclusionId = Guid.NewGuid();
        await using (var db = fixture.CreateDbContext())
        {
            db.Projects.Add(Project.Create(projectId, $"CASE-READ-{Guid.NewGuid():N}", "Case reader", null,
                32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), projectId, pm.Id,
                new DateOnly(2026, 1, 1)));
            db.Files.Add(StoredFile.Create(fileId, $"private/case-read-{fileId:N}.jpg", "source.jpg", "image/jpeg",
                4, new string('a', 64), reporter.Id, now, null));
            db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), fileId, reporter.Id, now));
            var report = Report.Create(reportId, reporter.Id, "Case reader source", now,
                [VerifiedEvidenceReference.Create(evidenceId, fileId, "file-version-1", reporter.Id)]);
            var incident = IncidentCase.CreateUnassigned(caseId, reportId, now);
            incident.Triage(projectId, CaseVerificationMethod.ExistingEvidence, "Verified source", now);
            db.Reports.Add(report);
            db.IncidentCases.Add(incident);
            db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = caseId, ReportId = reportId, StartedAt = now });
            db.Entry(report).Property<long>("Revision").CurrentValue = 1;
            db.Entry(incident).Property<long>("Revision").CurrentValue = 1;
            await db.SaveChangesAsync();

            var conclusion = CaseConclusion.Create(conclusionId, CaseConclusionOutcome.NoDefect, [], [evidenceId],
                "No defect after review", now);
            db.Set<CaseConclusion>().Add(conclusion);
            db.Entry(conclusion).Property<Guid>("CaseId").CurrentValue = caseId;
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateDbContext())
        {
            var snapshot = await new CaseDefectReadReader(new RoadGuardSystem.Repositories.Implementations.Reporting.ReportingRepository(db)).CaptureAsync(pm.Id, UserRoleCode.ProjectManager,
                projectId, new ReportingFiltersDto());
            Assert.NotNull(snapshot);
            Assert.Equal("anh-huy.case-defect.v1", snapshot.SchemaVersion);
            var caseFact = Assert.Single(snapshot.Cases, fact => fact.CaseId == caseId);
            Assert.Equal(Convert.ToBase64String(await db.IncidentCases.AsNoTracking().Where(c => c.Id == caseId)
                    .Select(c => EF.Property<byte[]>(c, "RowVersion")).SingleAsync()),
                caseFact.Version);
            Assert.Contains(caseFact.CurrentReports, fact => fact.ReportId == reportId && fact.Version != "UNAVAILABLE");
            Assert.Contains(caseFact.Conclusions, fact => fact.Id == conclusionId);
            var capture = Capture(projectId);
            var consumed = CaseDefectCaptureConsumer.Apply(capture, snapshot);
            Assert.Null(Assert.Single(consumed.Summary.Metrics).Value);
            Assert.NotNull(consumed.CaseDefectFacts);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new CaseDefectReadReader(new RoadGuardSystem.Repositories.Implementations.Reporting.ReportingRepository(db)).CaptureAsync(
                pm.Id, UserRoleCode.ProjectManager, Guid.NewGuid(), new ReportingFiltersDto()));
        }

        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={false} WHERE [Code]={UserRoleCode.ProjectManager.ToDbCode()}");
        try
        {
            await using var db = fixture.CreateDbContext();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new CaseDefectReadReader(new RoadGuardSystem.Repositories.Implementations.Reporting.ReportingRepository(db)).CaptureAsync(
                pm.Id, UserRoleCode.ProjectManager, projectId, new ReportingFiltersDto()));
        }
        finally
        {
            await using var db = fixture.CreateDbContext();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Roles] SET [IsActive]={true} WHERE [Code]={UserRoleCode.ProjectManager.ToDbCode()}");
        }
    }

    private static ReportingCaptureDto Capture(Guid projectId) => new(
        new ProjectSummaryV1("anh02.reporting.v1", projectId, DateTimeOffset.UtcNow, "pilot-reporting.v1", new(),
            [new ReportingMetricDto("casesByStatus", new(), null, "count", null, null, false,
                "UNAVAILABLE", ["HUY01_REPORTING_READER_UNAVAILABLE"], [])], [], "SERIALIZABLE"),
        [], [], [], new("PARTIAL", ["HUY_TIMELINE_READER_UNAVAILABLE"]));
}
