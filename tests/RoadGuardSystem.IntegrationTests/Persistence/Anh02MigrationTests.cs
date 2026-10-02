using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Exports;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Persistence;

public sealed class Anh02MigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture=new(createSpatialProbeSchema:false);
    private const string Baseline="20261002120000_AnhHuySharedIntegration";
    private const string Latest="20261002151928_Anh02AiReportingExportRetention";
    public Task InitializeAsync()=>fixture.InitializeAsync();
    public Task DisposeAsync()=>fixture.DisposeAsync();
    private RoadGuardDbContext Db()=>new(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString,o=>o.UseNetTopologySuite()).Options);
    [Fact]
    public async Task Fresh_schema_and_empty_down_up_preserve_existing_project()
    {
        await using var db=Db();var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync();
        Assert.Contains(Latest,await db.Database.GetAppliedMigrationsAsync());
        Assert.Contains("20261003090000_Anh02AnalysisAttemptClosure", await db.Database.GetAppliedMigrationsAsync());
        var fenced = await db.Database.SqlQueryRaw<string>("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.TR_ProcessingAttempts_AppendOnly')) AS [Value]").SingleAsync();
        Assert.Contains("r.Stage='VIDEO_ANALYSIS'", fenced);
        await migrator.MigrateAsync(Latest);
        var previous = await db.Database.SqlQueryRaw<string>("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.TR_ProcessingAttempts_AppendOnly')) AS [Value]").SingleAsync();
        Assert.DoesNotContain("Anh02AiMockRuns", previous); Assert.Contains("append-only", previous);
        await migrator.MigrateAsync();
        Assert.Equal(fenced, await db.Database.SqlQueryRaw<string>("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.TR_ProcessingAttempts_AppendOnly')) AS [Value]").SingleAsync());
        var id=Guid.NewGuid();db.Projects.Add(Project.Create(id,id.ToString(),"Preserved before downgrade",null,null,null,null,DateTimeOffset.UtcNow));await db.SaveChangesAsync();
        await migrator.MigrateAsync(Baseline);Assert.DoesNotContain(Latest,await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal("Preserved before downgrade",(await db.Projects.AsNoTracking().SingleAsync(p=>p.Id==id)).Name);
        await migrator.MigrateAsync();Assert.Contains(Latest,await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal("Preserved before downgrade",(await db.Projects.AsNoTracking().SingleAsync(p=>p.Id==id)).Name);
    }
    [Fact]
    public async Task Prior_baseline_already_blocks_deep_legacy_downgrade()
    {
        await using var db=Db();var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(Baseline);
        Assert.DoesNotContain(Latest,await db.Database.GetAppliedMigrationsAsync());
        var error=await Assert.ThrowsAnyAsync<Exception>(()=>migrator.MigrateAsync("20260921134719_AddP230FlightSurveyIdentityImmutability"));
        Assert.Contains("Request root scope correction cannot be undone",error.Message);
    }
    [Fact]
    public async Task Upgrade_preserves_old_sources_and_populated_down_is_fail_closed()
    {
        await using var db=Db();var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync(Baseline);
        var project=Guid.NewGuid();db.Projects.Add(Project.Create(project,project.ToString(),"Upgrade survivor",null,null,null,null,DateTimeOffset.UtcNow));await db.SaveChangesAsync();
        await migrator.MigrateAsync();Assert.Equal("Upgrade survivor",(await db.Projects.AsNoTracking().SingleAsync(p=>p.Id==project)).Name);
        // Owned isolated raw fixture avoids inventing a live producer: snapshot immutability and Down only.
        var snapshot=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Anh02ExportSnapshots (Id,ProjectId,PayloadJson,Hash,CapturedAt) VALUES ({snapshot},{project},N'{{}}',{new string('a',64)},{DateTimeOffset.UtcNow})");
        var error=await Assert.ThrowsAnyAsync<Exception>(()=>migrator.MigrateAsync(Baseline));Assert.Contains("Cannot downgrade populated ANH-02",error.Message);
        Assert.Contains(Latest,await db.Database.GetAppliedMigrationsAsync());Assert.True(await db.Set<ExportSnapshot>().AnyAsync(s=>s.Id==snapshot));
        await Assert.ThrowsAnyAsync<Exception>(()=>db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Anh02ExportSnapshots WHERE Id={snapshot}"));
        Assert.True(await db.Set<ExportSnapshot>().AnyAsync(s=>s.Id==snapshot));
    }
}
