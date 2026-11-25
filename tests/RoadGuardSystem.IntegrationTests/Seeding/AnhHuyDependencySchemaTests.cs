using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Seeding;

public sealed class AnhHuyDependencySchemaTests
{
    [Fact]
    public async Task UpgradeFromNovemberServerSchema_PreservesProjectAndDefect()
    {
        string[] serverMigrations =
        [
            "20261008171739_BaselineCurrentSchema",
            "20261009043010_AllowVerifiedDefectPostRepairTasks",
            "20261022074647_C1ControlledPublicPublication",
            "20261022181921_B2NotificationSupervisorAssignment"
        ];
        await VerifyUpgradePreservesLegacyRows(serverMigrations);
    }

    [Fact]
    public async Task UpgradeFrom6769253_PreservesProjectAndDefect()
    {
        // Git tree at 6769253448c3ac915e2dce9bd7982e74071de29a.
        string[] serverMigrations =
        [
            "20261008171739_BaselineCurrentSchema",
            "20261009043010_AllowVerifiedDefectPostRepairTasks",
            "20261022074647_C1ControlledPublicPublication",
            "20261022181921_B2NotificationSupervisorAssignment",
            "20261110043224_RoadGeometrySubmission",
            "20261110122600_GeometrySubmissionReviewRounds",
            "20261113051802_CrewFieldMeasurementDimensions",
            "20261113113000_ReporterRepairProgress",
            "20261117063514_ReporterPasswordReset",
            "20261117101500_ProjectHandoverStaging",
            "20261117183505_WebPmCoordinationMetadata",
            "20261119161424_AddAiWorkerTransportLedger",
            "20261119164737_AddAiVideoTelemetryRevisions",
            "20261119170649_AddAiVideoAttemptOutcomes",
            "20261123093000_FieldTaskMeasurementDimensions",
            "20261123093100_InitialTemporarySafetyResponsibility"
        ];
        await VerifyUpgradePreservesLegacyRows(serverMigrations);
    }

    private static async Task VerifyUpgradePreservesLegacyRows(string[] serverMigrations)
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options;
            await using var db = new RoadGuardDbContext(options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(serverMigrations[^1]);
            (await db.Database.GetAppliedMigrationsAsync()).Should().Equal(serverMigrations);

            var projectId = Guid.NewGuid();
            var defectId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Projects (Id, ProjectCode, Name, Status, CreatedAt)
                    VALUES ({projectId}, 'RG_UPGRADE', N'Synthetic road project', 1, SYSDATETIMEOFFSET());
                INSERT INTO DefectTypes (Code, Name, IsActive)
                    VALUES ('RG_UPGRADE', N'Synthetic defect type', 1);
                INSERT INTO Defects (Id, ProjectId, DefectTypeCode, Status, Severity)
                    VALUES ({defectId}, {projectId}, 'RG_UPGRADE', 1, 1);
                """);

            await migrator.MigrateAsync();

            (await db.Database.GetAppliedMigrationsAsync()).Should().Equal(db.Database.GetMigrations());
            db.Database.HasPendingModelChanges().Should().BeFalse();
            var project = await db.Projects.AsNoTracking().SingleAsync(row => row.Id == projectId);
            project.ProjectCode.Should().Be("RG_UPGRADE");
            project.Name.Should().Be("Synthetic road project");
            var defect = await db.Set<Defect>().AsNoTracking().SingleAsync(row => row.Id == defectId);
            defect.ProjectId.Should().Be(projectId);
            defect.DefectTypeCode.Should().Be("RG_UPGRADE");
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefectVersion_FreshAndBaseline_PreservesNullableLegacyAndDetectsStaleWrites(bool baseline)
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false);
        await fixture.InitializeAsync();
        try
        {
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString,
                s => s.UseNetTopologySuite()).Options;
            await using var db = new RoadGuardDbContext(options);
            var migrator = db.GetService<IMigrator>();
            (await db.Database.GetAppliedMigrationsAsync()).Should().BeEmpty();
            await migrator.MigrateAsync(baseline ? "20261008171739_BaselineCurrentSchema" : null);
            if (!baseline)
            {
                (await db.Database.GetAppliedMigrationsAsync()).Should().Equal(db.Database.GetMigrations());
                db.Database.HasPendingModelChanges().Should().BeFalse();
            }
            var id = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO DefectTypes (Code,Name,IsActive) VALUES ('RG_SCHEMA','Synthetic schema probe',1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Defects (Id,DefectTypeCode,Status,Severity) VALUES ({id},'RG_SCHEMA',1,1)");
            if (baseline) await migrator.MigrateAsync();
            var row = await db.Set<Defect>().SingleAsync(d => d.Id == id);
            row.ProjectId.Should().BeNull(); row.RoadSectionVersionId.Should().BeNull();
            var version = db.Entry(row).Property<byte[]>("RowVersion").CurrentValue!;
            version.Should().HaveCount(8);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Defects SET Severity=2 WHERE Id={id}");
            row.Assess("RG_SCHEMA", null, DefectSeverity.High, Guid.NewGuid(), "Synthetic concurrency probe");
            var stale = () => db.SaveChangesAsync();
            await stale.Should().ThrowAsync<DbUpdateConcurrencyException>();
            db.ChangeTracker.Clear();
            (await db.Set<Defect>().Where(d => d.Id == id).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync())
                .Should().NotEqual(version);
            await migrator.MigrateAsync("20261008171739_BaselineCurrentSchema");
            db.ChangeTracker.Clear();
            (await db.Set<Defect>().SingleAsync(d => d.Id == id)).ProjectId.Should().BeNull();
            await migrator.MigrateAsync();
            (await db.Set<Defect>().Where(d => d.Id == id).Select(d => EF.Property<byte[]>(d, "RowVersion")).SingleAsync())
                .Should().HaveCount(8);
        }
        finally { await fixture.DisposeAsync(); }
    }
}
