using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reports;

public sealed class ReportMatchingLocationMigrationTests
{
    [Fact]
    public async Task UpgradeFromImmediatePredecessorPreservesLegacyReportsAndFencesImmutableHistory()
    {
        const string previous = "20261123093100_InitialTemporarySafetyResponsibility";
        await using var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAtMigrationAsync(previous);
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var reportId = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyDefectSourceLink>().Where(row => row.DefectId == source.Defect)
            .Select(row => row.ReportSourceId).SingleAsync();
        var report = await db.Reports.AsNoTracking().SingleAsync(row => row.Id == reportId);
        var before = await db.Reports.AsNoTracking().CountAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync();
        Assert.Equal(before, await db.Reports.CountAsync());
        var preserved = await db.Reports.AsNoTracking().SingleAsync(row => row.Id == report.Id);
        Assert.Equal(report.Description, preserved.Description);
        Assert.Equal(report.ReceivedAt, preserved.ReceivedAt);
        Assert.All(preserved.OriginalEvidence, evidence => Assert.Null(evidence.CaptureMetadata));
        Assert.Empty(await db.Set<ReportMatchingLocation>().ToArrayAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        var schema = await SandboxSeedGuard.ReadSchemaAsync(db);
        Assert.Equal("20261125072411_TemporarySafetyResolution", schema.Migrations[^1]);
        Assert.Contains("dbo.ReportMatchingLocations", schema.Tables);
        // Empty downgrade is supported without touching legacy data.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync();
        var caseId = await db.Set<RoadGuardSystem.Repositories.Models.Huy01.HuyCaseReportLink>()
            .Where(row => row.ReportId == report.Id && row.EndedAt == null).Select(row => row.CaseId).SingleAsync();
        var confirmation = ReportMatchingLocation.Create(report.Id, 1, caseId, source.Project,
            source.Pm, DateTimeOffset.UtcNow, "Isolated schema immutability fixture", source.Route, source.Set,
            "fixture-version", "fixture-report-version", "fixture-case-version", null,
            [await db.RoadSegments.Where(row => row.SegmentSetId == source.Set).Select(row => row.Id).SingleAsync()], null, null, null);
        db.Add(confirmation); await db.SaveChangesAsync();
        var mutation = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE ReportMatchingLocations SET Reason=N'changed' WHERE Id={confirmation.Id}"));
        Assert.Equal(51141, mutation.Number);
        var downgrade = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(previous));
        Assert.Equal(51142, downgrade.Number);
        Assert.Equal(1, await db.Set<ReportMatchingLocation>().CountAsync());
        // EF commits each newer Down separately before reaching this historical fence.
        // Reapply the empty additive safety migration and verify the final head.
        await migrator.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(before, await db.Reports.CountAsync());
    }
}
