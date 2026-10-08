using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Inspections;

public sealed class H3FieldMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);

    [Fact]
    public async Task FreshSchemaMakesUnknownNullableWhileKeepingLegacySourceMeaning()
    {
        await using var db = Db();
        var target = Assert.Single(db.Database.GetMigrations().Where(name => name.EndsWith("_BaselineCurrentSchema", StringComparison.Ordinal)));
        await db.GetService<IMigrator>().MigrateAsync();
        var nullableColumns = await db.Database.SqlQueryRaw<string>("""
            SELECT OBJECT_NAME(object_id)+'.'+name AS [Value]
            FROM sys.columns WHERE is_nullable=1 AND
                (object_id=OBJECT_ID('GroundTruthMeasurements') AND name IN ('Value','Location')
                 OR object_id=OBJECT_ID('FieldInspectionTasks') AND name='SurveyId')
            """).ToArrayAsync();
        Assert.Equal(new[] { "FieldInspectionTasks.SurveyId", "GroundTruthMeasurements.Location", "GroundTruthMeasurements.Value" },
            nullableColumns.Order(StringComparer.Ordinal));
        var uniqueOrigin = await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID('FieldInspectionOperationOrigins')
                AND is_unique=1 AND name LIKE '%ProjectId_OriginId%'
            """).SingleAsync();
        Assert.Equal(1, uniqueOrigin);
        var immutable = await db.Database.SqlQueryRaw<string>("""
            SELECT name AS [Value] FROM sys.triggers WHERE name IN
                ('TR_FieldTaskStartOrigins_Immutable','TR_FieldInspectionSubmissions_Immutable',
                 'TR_FieldInspectionOperationOrigins_Immutable','TR_FieldInspectionEvidenceLinks_Immutable')
            """).ToArrayAsync();
        Assert.Equal(4, immutable.Length);
        await db.GetService<IMigrator>().MigrateAsync();
        await db.GetService<IMigrator>().MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task UpgradePreservesKnownZeroAndLegacyGpsWithoutInventingUnknown()
    {
        await using var db = Db();
        var migrator = db.GetService<IMigrator>();
        var target = Assert.Single(db.Database.GetMigrations().Where(name => name.EndsWith("_BaselineCurrentSchema", StringComparison.Ordinal)));

        await migrator.MigrateAsync();
        var project = Guid.NewGuid(); var road = Guid.NewGuid(); var route = Guid.NewGuid();
        var session = Guid.NewGuid(); var measurement = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        db.Projects.Add(Project.Create(project, project.ToString(), "H3 populated legacy fixture", null, null, null, null, now));
        db.RoadSections.Add(RoadSection.Create(road, project, "legacy-research"));
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT RoadSectionVersions(Id,RoadSectionId,VersionNo,IsCurrent,Geometry,EffectiveFrom,ChangeReason)
            VALUES({route},{road},1,1,geometry::STGeomFromText('LINESTRING(0 0,100 0)',32648),{now},'legacy')
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT FieldInspectionSessions(Id,Purpose,FieldInspectionTaskId,ProjectId,RoadSectionVersionId,SurveyId,
                SessionCode,InspectorUserId,InspectorName,ConductedAt,WeatherCondition,Method,Status,EvidenceFileId)
            VALUES({session},2,NULL,{project},{route},NULL,{session.ToString()},NULL,'legacy researcher',{now},NULL,'manual',1,NULL)
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT GroundTruthMeasurements(Id,FieldInspectionSessionId,SampleId,RoadSectionVersionId,SurveyId,DefectId,
                MeasurementType,Value,Unit,Location,InstrumentName,InstrumentReference,MeasurementMethod,MeasuredBy,
                MeasuredAt,EvidenceFileId,Notes)
            VALUES({measurement},{session},'genuine-zero',{route},NULL,NULL,1,0,'mm',geography::Point(10,106,4326),
                'legacy ruler',NULL,'manual','legacy researcher',{now},NULL,'No camera in this historical research capture')
            """);
        var before = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await migrator.MigrateAsync();
        var after = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(before, after);
        Assert.Equal(0m, await db.Database.SqlQueryRaw<decimal>(
            "SELECT [Value] AS [Value] FROM GroundTruthMeasurements WHERE SampleId='genuine-zero'").SingleAsync());
        Assert.Equal("mm", await db.Database.SqlQueryRaw<string>(
            "SELECT [Unit] AS [Value] FROM GroundTruthMeasurements WHERE SampleId='genuine-zero'").SingleAsync());
        Assert.Equal(106d, await db.Database.SqlQueryRaw<double>(
            "SELECT Location.Long AS [Value] FROM GroundTruthMeasurements WHERE SampleId='genuine-zero'").SingleAsync());
        Assert.Equal(10d, await db.Database.SqlQueryRaw<double>(
            "SELECT Location.Lat AS [Value] FROM GroundTruthMeasurements WHERE SampleId='genuine-zero'").SingleAsync());
        Assert.Equal(4326, await db.Database.SqlQueryRaw<int>(
            "SELECT Location.STSrid AS [Value] FROM GroundTruthMeasurements WHERE SampleId='genuine-zero'").SingleAsync());
        Assert.Equal(2, await db.Database.SqlQueryRaw<int>(
            "SELECT CAST(Purpose AS int) AS [Value] FROM FieldInspectionSessions WHERE SessionCode=CAST(Id AS nvarchar(36))").SingleAsync());
        Assert.Null((await db.RoadSectionVersions.AsNoTracking().SingleAsync(row => row.Id == route)).CrsProfileRevisionId);
        Assert.False(db.Database.HasPendingModelChanges());
        var unknown = Guid.NewGuid();
        var missingEvidenceReason = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT GroundTruthMeasurements(Id,FieldInspectionSessionId,SampleId,RoadSectionVersionId,SurveyId,DefectId,
                    MeasurementType,Value,Unit,Location,InstrumentName,MeasurementMethod,MeasuredBy,MeasuredAt,Notes)
                VALUES({Guid.NewGuid()},{session},'invalid-known-null-reason',{route},NULL,NULL,1,0,'mm',
                    geography::Point(10,106,4326),'ruler','manual','researcher',{now},NULL)
                """));
        Assert.Equal(547, missingEvidenceReason.Number);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT GroundTruthMeasurements(Id,FieldInspectionSessionId,SampleId,RoadSectionVersionId,SurveyId,DefectId,
                MeasurementType,Value,ValueState,UnknownReason,Dimension,Unit,Location,LocationState,LocationReason,
                InstrumentName,MeasurementMethod,MeasuredBy,MeasuredAt,Notes)
            VALUES({unknown},{session},'unknown-cannot-downgrade',{route},NULL,NULL,1,NULL,'UNKNOWN','instrument failed',
                'LENGTH','mm',NULL,'UNKNOWN','GPS unavailable','ruler','manual','researcher',{now},'genuine unknown')
            """);

        Assert.Equal(after, await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM GroundTruthMeasurements WHERE SampleId='unknown-cannot-downgrade' AND [Value] IS NULL AND [Location] IS NULL").SingleAsync());
    }
}
