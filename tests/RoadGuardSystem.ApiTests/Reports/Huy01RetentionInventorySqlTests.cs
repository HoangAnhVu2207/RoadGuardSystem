using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Repositories.Implementations.Retention;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed class Huy01RetentionInventorySqlTests(AuthenticationSqlServerFixture fixture)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    [Trait("Package", "HUY-01")]
    public async Task ConclusionEvidencePreservesProjectObligationButIncompleteSchemaNeverClaimsFullInventory()
    {
        var actor = await fixture.CreateUserAsync($"retention-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var now = DateTimeOffset.UtcNow;
        var projectId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var caseId = Guid.NewGuid();
        var conclusionId = Guid.NewGuid();
        await using (var db = fixture.CreateDbContext())
        {
            db.Projects.Add(Project.Create(projectId, $"RET-{Guid.NewGuid():N}", "Retention source", null,
                32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now));
            db.Files.Add(StoredFile.Create(fileId, $"private/retention-{fileId:N}.jpg", "source.jpg", "image/jpeg",
                4, new string('a', 64), actor.Id, now, null));
            db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), fileId, actor.Id, now));
            var report = Report.Create(reportId, actor.Id, "Retention source", now,
                [VerifiedEvidenceReference.Create(evidenceId, fileId, "verified-version", actor.Id)]);
            var incident = IncidentCase.CreateUnassigned(caseId, reportId, now);
            incident.Triage(projectId, CaseVerificationMethod.ExistingEvidence, "Project assigned", now);
            db.Reports.Add(report);
            db.IncidentCases.Add(incident);
            db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = caseId, ReportId = reportId, StartedAt = now });
            db.Entry(report).Property<long>("Revision").CurrentValue = 1;
            db.Entry(incident).Property<long>("Revision").CurrentValue = 1;
            await db.SaveChangesAsync();
            await using (var plainDb = fixture.CreateDbContext())
            {
                var plain = await new Huy01RetentionInventoryContributor(plainDb).ReadAsync(fileId, default);
                Assert.False(plain.Complete);
                Assert.Contains(plain.References, reference => reference.Kind == "REPORT_EVIDENCE" && reference.Id == evidenceId);
                Assert.Contains(plain.References, reference => reference.Kind == "CASE_REPORT_LINK" && reference.ProjectId == projectId);
            }
            var conclusion = CaseConclusion.Create(conclusionId, CaseConclusionOutcome.NoDefect, [], [evidenceId], "Reviewed evidence", now);
            db.Set<CaseConclusion>().Add(conclusion);
            db.Entry(conclusion).Property<Guid>("CaseId").CurrentValue = caseId;
            db.Set<HuyConclusionEvidence>().Add(new() { ConclusionId = conclusionId, EvidenceId = evidenceId,
                SourceReportId = reportId, OriginalEvidenceId = evidenceId });
            await db.SaveChangesAsync();
        }

        await using var readerDb = fixture.CreateDbContext();
        var reader = new Huy01RetentionInventoryContributor(readerDb);
        var first = await reader.ReadAsync(fileId, default);
        var again = await reader.ReadAsync(fileId, default);
        Assert.Equal("HUY", first.Name);
        Assert.False(first.Complete);
        Assert.Contains(first.References, reference => reference.Kind == "CASE_CONCLUSION_EVIDENCE" &&
            reference.Id == conclusionId && reference.ProjectId == projectId);
        Assert.Equal(first.References, again.References);
        Assert.Contains(fileId, await reader.KnownProjectFilesAsync(projectId, default));
        var priorLinkVersion = Assert.Single(first.References.Where(reference => reference.Kind == "CASE_REPORT_LINK")).SourceVersion;
        await using (var db = fixture.CreateDbContext())
        {
            var link = await db.Set<HuyCaseReportLink>().SingleAsync(row => row.ReportId == reportId);
            link.EndedAt = now.AddMinutes(1);
            await db.SaveChangesAsync();
        }
        Assert.Contains(fileId, await reader.KnownProjectFilesAsync(projectId, default));
        var closed = await reader.ReadAsync(fileId, default);
        Assert.NotEqual(priorLinkVersion, Assert.Single(closed.References.Where(reference => reference.Kind == "CASE_REPORT_LINK")).SourceVersion);
        Assert.False((await reader.ReadAsync(Guid.NewGuid(), default)).Complete);
    }
}
