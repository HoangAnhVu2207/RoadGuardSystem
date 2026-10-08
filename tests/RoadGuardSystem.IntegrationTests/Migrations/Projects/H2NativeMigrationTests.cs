using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class H2NativeMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString, s => s.UseNetTopologySuite()).Options);
    [Fact]
    public async Task Fresh_and_populated_upgrade_preserve_legacy_geometry_receipts_and_model()
    {
        await using var db = Db(); var migrator = db.GetService<IMigrator>();
        var discovered = db.Database.GetMigrations().ToArray();
        var latest = Assert.Single(discovered.Where(x => x.EndsWith("_BaselineCurrentSchema", StringComparison.Ordinal)));
        Assert.False(db.Database.HasPendingModelChanges());
        await migrator.MigrateAsync();
        var triggers = await db.Database.SqlQueryRaw<string>("SELECT name AS [Value] FROM sys.triggers WHERE name LIKE '%Pavement%' OR name LIKE '%CrsProfile%' OR name LIKE '%NativeRoute%' OR name='TR_RoadSectionVersions_ProfileScope' OR name LIKE '%GeometryMapPublications%' OR name LIKE '%GeometryLocationImpact%'").ToArrayAsync();
        Assert.Equal(13, triggers.Length);

        await migrator.MigrateAsync();
        var project = Guid.NewGuid(); var actor = Guid.NewGuid(); var section = Guid.NewGuid(); var route = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        db.Projects.Add(Project.Create(project, project.ToString(), "legacy survivor", null, null, null, null, now));
        await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM Roles WHERE Code='SUPERVISOR') INSERT Roles(Code,Name,NormalizedName,IsActive) VALUES('SUPERVISOR','Supervisor','SUPERVISOR',1)");
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(), DisplayName = "fixture", RoleCode = UserRoleCode.Supervisor, Status = UserStatus.Active, CreatedAt = now, PasswordHash = "not-a-login" });
        db.RoadSections.Add(RoadSection.Create(section, project, "legacy"));
        var receipt = IdempotencyRecord.Create(actor, project, "H2.fixture", "preserved", new string('a', 64), route, "{}", now); db.Add(receipt);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT RoadSectionVersions(Id,RoadSectionId,VersionNo,IsCurrent,Geometry,EffectiveFrom,ChangeReason) VALUES({route},{section},1,1,geometry::STGeomFromText('LINESTRING(0 0,100 0)',32648),{now},'legacy')");
        var history = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await migrator.MigrateAsync(); db.ChangeTracker.Clear();
        var preserved = await db.RoadSectionVersions.AsNoTracking().SingleAsync(x => x.Id == route);
        Assert.Null(preserved.CrsProfileRevisionId); Assert.Equal(32648, preserved.Geometry.SRID); Assert.Equal(100, preserved.Geometry.Length);
        Assert.Equal(receipt.OutcomeJson, (await db.Set<IdempotencyRecord>().AsNoTracking().SingleAsync(x => x.Id == receipt.Id)).OutcomeJson);
        Assert.Equal(history, await db.Database.GetAppliedMigrationsAsync());
        var profile = new CrsProfileRevision { Id = Guid.NewGuid(), ProjectId = project, Code = "candidate", Revision = 1, SourceSrid = 0, SampleOnly = true, PayloadJson = "{}", CreatedBy = actor, CreatedAt = now }; db.Add(profile); await db.SaveChangesAsync();
        var system = new RoadRouteSystem { Id = Guid.NewGuid(), ProjectId = project, Code = "fixture", Name = "fixture" }; db.Add(system);
        var firstRoad = RoadSection.Create(Guid.NewGuid(), project, "first"); var secondRoad = RoadSection.Create(Guid.NewGuid(), project, "second"); db.AddRange(firstRoad, secondRoad); await db.SaveChangesAsync();
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT RoadSectionVersions(Id,RoadSectionId,VersionNo,IsCurrent,Geometry,EffectiveFrom,ChangeReason,CrsProfileRevisionId) VALUES({first},{firstRoad.Id},1,1,geometry::STGeomFromText('LINESTRING(0 0,100 0)',0),{now},'candidate',{profile.Id}),({second},{secondRoad.Id},1,1,geometry::STGeomFromText('LINESTRING(0 0,100 0)',0),{now},'candidate',{profile.Id})");
        var cycle = await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT NativeRouteVersionFacts(RoadSectionVersionId,ProjectId,CrsProfileRevisionId,RouteSystemId,RouteKind,ParentRouteVersionId,JunctionOffsetMeters,CanonicalLengthMeters,SampleOnly) VALUES({first},{project},{profile.Id},{system.Id},'BRANCH',{second},0,100,1),({second},{project},{profile.Id},{system.Id},'BRANCH',{first},0,100,1)"));
        Assert.Contains("topology cycle", cycle.Message); Assert.False(await db.Set<NativeRouteVersionFacts>().AnyAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE CrsProfileRevisions SET Code='mutated' WHERE Id={profile.Id}"));
        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RoadSectionVersions SET CrsProfileRevisionId={profile.Id} WHERE Id={route}"));

        await migrator.MigrateAsync();
        Assert.Equal(discovered, await db.Database.GetAppliedMigrationsAsync());
        await using var check = Db(); Assert.False(check.Database.HasPendingModelChanges());
        var finalLegacy = await check.RoadSectionVersions.AsNoTracking().SingleAsync(x => x.Id == route);
        Assert.Null(finalLegacy.CrsProfileRevisionId); Assert.Equal(32648, finalLegacy.Geometry.SRID); Assert.Equal(100, finalLegacy.Geometry.Length);
        Assert.Equal(receipt.OutcomeJson, (await check.Set<IdempotencyRecord>().AsNoTracking().SingleAsync(x => x.Id == receipt.Id)).OutcomeJson);
    }
}
