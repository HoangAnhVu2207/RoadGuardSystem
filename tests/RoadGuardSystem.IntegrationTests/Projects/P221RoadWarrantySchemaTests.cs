using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Transactions;
using RoadGuardSystem.Repositories.Warranties;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

[Trait("TaskId", "P2-21")]
public sealed class P221RoadWarrantySchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory Srid32648Factory = new(
        new PrecisionModel(),
        SpatialConstants.UtmZone48NSrid);
    private static readonly GeometryFactory Srid32649Factory = new(
        new PrecisionModel(),
        SpatialConstants.UtmZone49NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P221RoadWarrantySchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-21: initial road version and current switch preserve prior geometry")]
    public async Task RoadSectionVersion_CreateAndSwitch_PreservesOldVersionAndOneCurrentMarker()
    {
        await using var context = _fixture.CreateDbContext();
        var (_, section, initialVersion) = await CreateRoadAsync(context, "P221-ROAD-A");
        var nextVersion = CreateVersion(section.Id, 2, true, SpatialConstants.UtmZone48NSrid);
        var service = new RoadSectionVersionPersistenceService(context, new RoadGuardTransactionService(context));

        await service.AddVersionAndMakeCurrentAsync(section.Id, nextVersion);

        context.ChangeTracker.Clear();
        var versions = await context.RoadSectionVersions
            .AsNoTracking()
            .Where(version => version.RoadSectionId == section.Id)
            .OrderBy(version => version.VersionNo)
            .ToListAsync();

        versions.Should().HaveCount(2);
        versions.Should().ContainSingle(version => version.Id == initialVersion.Id && !version.IsCurrent);
        versions.Should().ContainSingle(version => version.Id == nextVersion.Id && version.IsCurrent);
        versions[0].Geometry.Coordinates.Should().HaveCount(2);
    }

    [Fact(DisplayName = "P2-21: SQL Server rejects non-LineString geometry")]
    public async Task RoadSectionVersion_NonLineStringGeometry_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var (_, section, _) = await CreateRoadAsync(context, "P221-ROAD-GEOMETRY");
        var versionId = Guid.NewGuid();

        var insert = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [RoadSectionVersions]
                ([Id], [RoadSectionId], [VersionNo], [IsCurrent], [Geometry], [EffectiveFrom], [ChangeReason])
            VALUES ({versionId}, {section.Id}, {2}, {false},
                geometry::Point(588500, 2325000, 32648), SYSDATETIMEOFFSET(), {"Invalid shape"})
            """);

        await insert.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-21: SQL trigger preserves immutable road version history")]
    public async Task RoadSectionVersion_HistoryMutationAndDelete_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var (_, _, version) = await CreateRoadAsync(context, "P221-ROAD-IMMUTABLE");

        var update = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [RoadSectionVersions] SET [ChangeReason] = {"Changed"} WHERE [Id] = {version.Id}");
        var delete = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [RoadSectionVersions] WHERE [Id] = {version.Id}");

        await update.Should().ThrowAsync<SqlException>();
        await delete.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-21: warranty persists a scoped road stage and rejects cross-project road scope")]
    public async Task Warranty_ScopedRoad_RoundTripsAndCrossProjectRoadIsRejected()
    {
        await using var context = _fixture.CreateDbContext();
        var (project, section, _) = await CreateRoadAsync(context, "P221-ROAD-WARRANTY");
        var validWarranty = CreateWarranty(project.Id, section.Id, WarrantyScope.RoadSection);
        var warrantyService = new WarrantyPersistenceService(context);

        await warrantyService.CreateAsync(validWarranty);

        (await context.Warranties.AsNoTracking().SingleAsync(warranty => warranty.Id == validWarranty.Id))
            .RoadSectionId.Should().Be(section.Id);

        var (otherProject, otherSection, _) = await CreateRoadAsync(context, "P221-ROAD-OTHER");
        var crossProjectWarranty = CreateWarranty(project.Id, otherSection.Id, WarrantyScope.RoadSection);
        var createCrossProject = () => warrantyService.CreateAsync(crossProjectWarranty);

        await createCrossProject.Should().ThrowAsync<InvalidOperationException>();
        otherProject.Id.Should().NotBe(project.Id);
    }

    [Fact(DisplayName = "P2-21: SQL Server rejects invalid warranty dates and source file foreign keys")]
    public async Task Warranty_InvalidDateAndSourceFile_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var project = CreateProject(SpatialConstants.UtmZone48NSrid);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var invalidDate = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Warranties]
                ([Id], [ProjectId], [HandoverDate], [WarrantyStartDate], [WarrantyEndDate], [Scope], [Status])
            VALUES ({Guid.NewGuid()}, {project.Id}, {new DateOnly(2026, 1, 1)},
                {new DateOnly(2026, 2, 1)}, {new DateOnly(2026, 1, 31)}, {1}, {1})
            """);
        var missingSourceFile = () => new WarrantyPersistenceService(context).CreateAsync(
            CreateWarranty(project.Id, null, WarrantyScope.Project, Guid.NewGuid()));

        await invalidDate.Should().ThrowAsync<SqlException>();
        await missingSourceFile.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-21: migration downgrades to P2-07 and reapplies road and warranty schema")]
    public async Task MigrationLifecycle_DowngradesToP207AndReapplies()
    {
        var fixture = new SqlServerTestFixture();
        await fixture.InitializeAsync();
        try
        {
            await using (var baseline = fixture.CreateDbContext())
            {
                await baseline.Database.EnsureDeletedAsync();
            }

            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite())
                .Options;
            await using var context = new RoadGuardDbContext(options);
            await context.Database.MigrateAsync();
            (await CountP221TablesAsync(context)).Should().Be(3);

            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20260920140643_AddNotificationPersistenceBoundary");
            (await CountP221TablesAsync(context)).Should().Be(0);

            await context.Database.MigrateAsync();
            (await CountP221TablesAsync(context)).Should().Be(3);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private async Task<(Project Project, RoadSection Section, RoadSectionVersion Version)> CreateRoadAsync(
        RoadGuardDbContext context,
        string code)
    {
        var project = CreateProject(SpatialConstants.UtmZone48NSrid);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, code);
        var version = CreateVersion(section.Id, 1, true, SpatialConstants.UtmZone48NSrid);
        var service = new RoadSectionVersionPersistenceService(context, new RoadGuardTransactionService(context));
        await service.CreateInitialAsync(section, version);
        return (project, section, version);
    }

    private static Project CreateProject(int engineeringUtmSrid) => new()
    {
        Id = Guid.NewGuid(),
        ProjectCode = $"P221-{Guid.NewGuid():N}",
        Name = "P2-21 SQL fixture project",
        EngineeringUtmSrid = engineeringUtmSrid,
        Status = ProjectStatus.Active,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static RoadSectionVersion CreateVersion(
        Guid roadSectionId,
        int versionNo,
        bool isCurrent,
        int srid)
    {
        var factory = srid == SpatialConstants.UtmZone48NSrid ? Srid32648Factory : Srid32649Factory;
        return RoadSectionVersion.Create(
            Guid.NewGuid(),
            roadSectionId,
            versionNo,
            isCurrent,
            factory.CreateLineString(new[]
            {
                new Coordinate(588500 + versionNo, 2325000),
                new Coordinate(588600 + versionNo, 2325100)
            }),
            DateTimeOffset.UtcNow,
            "P2-21 fixture geometry version");
    }

    private static Warranty CreateWarranty(
        Guid projectId,
        Guid? roadSectionId,
        WarrantyScope scope,
        Guid? sourceDocumentId = null)
        => Warranty.Create(
            Guid.NewGuid(),
            projectId,
            roadSectionId,
            null,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            120_000m,
            scope,
            "P2-21 fixture warranty terms",
            sourceDocumentId,
            WarrantyStatus.Active);

    private static Task<int> CountP221TablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('RoadSections', 'RoadSectionVersions', 'Warranties')
                """)
            .SingleAsync();
}
