using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

public sealed class ReporterRepairProgressMigrationTests
{
    [Fact]
    public async Task PreviousMigrationPreservesLegacyPublicationAndMatchesSandboxCatalog()
    {
        const string previous = "20261113051802_CrewFieldMeasurementDimensions";
        const string target = "20261113113000_ReporterRepairProgress";
        var fixture = new AuthenticationSqlServerFixture();
        try
        {
            await fixture.InitializeAtMigrationAsync(previous);
            await using var db = fixture.CreateDbContext();
            Assert.Equal(previous, (await db.Database.GetAppliedMigrationsAsync()).Last());
            var source = await GenuineProtectedSource.Seed(db, fixture);
            var incident = await db.IncidentCases.SingleAsync(row => row.ProjectId == source.Project);
            var reportId = Assert.Single(incident.ActiveReportIds);
            var owner = await db.Reports.Where(row => row.Id == reportId).Select(row => row.ReporterUserId).SingleAsync();
            var publicationId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            const string summary = "Legacy summary";
            var recipients = System.Text.Json.JsonSerializer.Serialize(new[] { reportId });
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO CasePublications(Id, CaseId, RecipientReportIdsJson, DefectIdsJson, EvidenceIdsJson, Summary, PublishedAt)
                VALUES ({publicationId}, {incident.Id}, {recipients}, N'[]', N'[]', {summary}, {now});
                INSERT INTO CasePublicationRecipients(PublicationId, ReportId) VALUES ({publicationId}, {reportId});
                """);

            await db.GetService<IMigrator>().MigrateAsync(target);
            var recipient = await db.Set<HuyPublicationRecipient>().SingleAsync(row => row.PublicationId == publicationId);
            Assert.Null(recipient.RepairProgressStatus);
            Assert.Null(recipient.RepairAcceptanceFingerprint);
            var report = await new ReporterLifecycleRepository(db, new ReporterReportRepository(db)).ReadAsync(owner, reportId, default);
            Assert.Null(report.RepairProgress);
            var update = Assert.Single(report.PublicUpdates);
            Assert.Equal(publicationId, update.Id);
            Assert.Equal(summary, update.Summary);

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                dir = dir.Parent ?? throw new InvalidOperationException("Repository root not found.");
            var expected = SandboxSeedCatalog.Load(Path.Combine(dir.FullName, "sandbox", "seeds", "v1")).Schema;
            var actual = await SandboxSeedGuard.ReadSchemaAsync(db);
            Assert.Equal(expected.Signature, actual.Signature);
            Assert.Equal(expected.Migrations, actual.Migrations);
            Assert.Equal(expected.Tables, actual.Tables);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }
}
