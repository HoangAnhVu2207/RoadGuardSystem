using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Migrations;

public sealed class SurveyWarrantyScheduleMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture _fixture = new(createSpatialProbeSchema: false);

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task UpgradePreservesExistingSurveyPlanWithoutCreatingOccurrences()
    {
        await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261126090000_ReporterRoutingMap");
        var project = Guid.NewGuid();
        var road = Guid.NewGuid();
        var plan = Guid.NewGuid();
        var start = new DateTimeOffset(2027, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);
        const string requirements = "{}";
        db.Projects.Add(Project.Create(project, project.ToString(), "Legacy survey project", null, null, null, null, start));
        db.RoadSections.Add(RoadSection.Create(road, project, "legacy-survey"));
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT dbo.SurveyPlans (Id, ProjectId, RoadSectionId, PlannedStartAt, PlannedEndAt, SurveyType, Status, OutputRequirements)
            VALUES ({plan}, {project}, {road}, {start}, {end}, 1, 1, {requirements})
            """);

        await migrator.MigrateAsync();
        var preserved = await db.SurveyPlans.AsNoTracking().SingleAsync(row => row.Id == plan);
        Assert.Equal(start, preserved.PlannedStartAt);
        Assert.Equal(end, preserved.PlannedEndAt);
        Assert.Null(preserved.WarrantyId);
        Assert.Equal(0, await db.SurveyPlanOccurrences.CountAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
