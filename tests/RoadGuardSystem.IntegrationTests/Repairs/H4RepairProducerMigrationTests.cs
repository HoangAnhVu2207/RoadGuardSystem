using RoadGuardSystem.Repositories.Implementations.Reporting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4RepairProducerMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, options => options.UseNetTopologySuite()).Options);

    private static readonly string[] NewTables = ["RepairFieldTaskBindings","RepairMeasurementAssessments",
        "RepairAssessmentMeasurements","RepairAssessmentEvidence","RepairExecutionStarts","RepairExecutionFinishes",
        "RepairAttemptSubmissionLinks","RepairAttemptReviews","RepairItemLifecycleEvents","RepairNormalSuccessors",
        "RepairEligibilityAssessments","RepairEligibilityWarrantySources","RepairEligibilityHandoverSources",
        "RepairSafetyMonitoring","RepairSafetyChecks","RepairSafetyCheckEvidence","RepairDangerWarnings","RepairDangerAcknowledgements"];

    [Fact]
    public async Task ActualProducerGuardsAndEmptyDowngradePreservePopulatedCore()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        var modelDifferences = db.GetService<IMigrationsModelDiffer>().GetDifferences(
            db.GetService<IModelRuntimeInitializer>().Initialize(
                db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model, designTime: true).GetRelationalModel(),
            db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel());
        Assert.True(modelDifferences.Count == 0, string.Join("; ", modelDifferences.Select(value =>
            value.GetType().Name + ":" + System.Text.Json.JsonSerializer.Serialize(value, value.GetType()))));
        foreach (var table in NewTables)
        {
            var name = "TR_" + table + (table == "RepairSafetyMonitoring" ? "_Scope" : "_Immutable");
            Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE name={name} AND is_disabled=0").SingleAsync());
        }
        var facts = await SeedCore(db);
        await db.Database.MigrateAsync();
        Assert.Equal(2, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM RepairItems WHERE ProjectId={facts.Project}").SingleAsync());
        await db.Database.MigrateAsync();
        Assert.Null(await db.Set<RepairObligation>().Where(value => value.Id == facts.Obligation).Select(value => value.CurrentRepairItemId).SingleAsync());
    }

    [Fact]
    public async Task CurrentItemPinCannotBorrowAnotherActualObligation()
    {
        await using var db = Db(); await db.Database.MigrateAsync(); var facts = await SeedCore(db);
        // Controlled source facts exercise schema invariants, not command authorization.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET CurrentRepairItemId={facts.Item} WHERE Id={facts.Obligation}");
        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE RepairObligations SET CurrentRepairItemId={facts.OtherItem} WHERE Id={facts.Obligation}"));
        Assert.Equal(51310, error.Number);
        Assert.Equal(facts.Item, await db.Set<RepairObligation>().Where(value => value.Id == facts.Obligation).Select(value => value.CurrentRepairItemId).SingleAsync());
    }

    [Fact]
    public async Task PopulatedProducerPinDowngradeRefusesBeforeErasingCoreHistory()
    {
        await using var db = Db(); await db.Database.MigrateAsync(); var facts = await SeedCore(db);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET CurrentRepairItemId={facts.Item} WHERE Id={facts.Obligation}");

        Assert.Equal(facts.Item, await db.Set<RepairObligation>().Where(value => value.Id == facts.Obligation).Select(value => value.CurrentRepairItemId).SingleAsync());
        Assert.Equal(2, await db.Set<RepairItem>().CountAsync(value => value.ProjectId == facts.Project));
    }

    [Fact]
    public async Task UnpinnedLegacyItemsDoNotInventOneCurrentItemOrCountAllHistoricalItems()
    {
        await using var db = Db(); await db.Database.MigrateAsync(); var facts = await SeedCore(db);
        var obligation = await db.Set<RepairObligation>().SingleAsync(value => value.Id == facts.Obligation);
        var additional = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, facts.Actor,
            UserRoleCode.ProjectManager, DateTimeOffset.UtcNow, new("controlled ambiguous legacy plan", "v1"));
        // Controlled legacy retired state exercises read completeness, not a cancellation command.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET State=8 WHERE Id={facts.Item}");
        var package = await db.RepairPackages.Include(value => value.Items).Include(value => value.Obligations).SingleAsync(value => value.ProjectId == facts.Project);
        package.AddItem(additional); db.Add(additional); await db.SaveChangesAsync();
        var capture = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(facts.Actor, facts.Project, new(), default);
        Assert.Contains("REPAIR_CURRENT_ITEM_PIN_NOT_VERIFIED", capture.MissingReasons);
        Assert.DoesNotContain(capture.Items, value => value.ObligationId == facts.Obligation);
        Assert.Single(capture.Items);
    }

    [Fact]
    public async Task ExplicitCurrentItemPinExcludesHistoricalSiblingFromCurrentStock()
    {
        await using var db = Db(); await db.Database.MigrateAsync(); var facts = await SeedCore(db);
        var obligation = await db.Set<RepairObligation>().SingleAsync(value => value.Id == facts.Obligation);
        var additional = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, RepairMode.Normal, facts.Actor,
            UserRoleCode.ProjectManager, DateTimeOffset.UtcNow, new("controlled selected source plan", "v1"));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET State=8 WHERE Id={facts.Item}");
        var package = await db.RepairPackages.Include(value => value.Items).Include(value => value.Obligations).SingleAsync(value => value.ProjectId == facts.Project);
        package.AddItem(additional); db.Add(additional); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET CurrentRepairItemId={additional.Id} WHERE Id={facts.Obligation}");
        var capture = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(facts.Actor, facts.Project, new(), default);
        Assert.Empty(capture.MissingReasons); Assert.Equal(2, capture.Items.Length);
        Assert.Equal(additional.Id, Assert.Single(capture.Items.Where(value => value.ObligationId == facts.Obligation)).Id);
        Assert.True(await db.Set<RepairItem>().AnyAsync(value => value.Id == facts.Item));
    }

    [Fact]
    public async Task LegacyDefectWithoutObligationInventoryCannotEstablishCompleteRepairStock()
    {
        await using var db = Db(); await db.Database.MigrateAsync(); var facts = await SeedCore(db);
        var source = await db.Defects.SingleAsync(value => value.ProjectId == facts.Project);
        db.Defects.Add(Defect.Create(Guid.NewGuid(), source.ProjectId!.Value, source.RoadSectionVersionId!.Value, null, source.DefectTypeCode,
            null, DefectSeverity.Low, DefectStatus.Open, source.Geometry!, DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        var capture = await new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db, TimeProvider.System)).CaptureAsync(facts.Actor, facts.Project, new(), default);
        Assert.Contains("REPAIR_OBLIGATION_INVENTORY_NOT_VERIFIED", capture.MissingReasons);
        Assert.Equal(2, capture.Items.Length);
    }

    private sealed record Facts(Guid Project, Guid Obligation, Guid Item, Guid OtherItem, Guid Actor);
    private static async Task<Facts> SeedCore(RoadGuardDbContext db)
    {
        var now = DateTimeOffset.UtcNow; var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Producer migration controlled fixture", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "R"); var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0, 0), new(10, 0)]), now, "sampleOnly identity fixture; no official CRS claim");
        var type = DefectType.Create("HP" + Guid.NewGuid().ToString("N"), "Controlled source");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open, geometry.CreatePoint(new Coordinate(5, 0)), now);
        if (!await db.Roles.AnyAsync(value => value.Code == UserRoleCode.ProjectManager)) db.Add(new ApplicationRole(UserRoleCode.ProjectManager, "PM"));
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), PasswordHash = "fixture", DisplayName = "controlled historical actor", RoleCode = UserRoleCode.ProjectManager, CreatedAt = now };
        db.AddRange(project, road, route, type, defect, actor); await db.SaveChangesAsync();
        db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor.Id,
            DateOnly.FromDateTime(now.UtcDateTime))); await db.SaveChangesAsync();
        var first = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true, RepairActualScope.Create(Guid.NewGuid(), road.Id, "frame-v1", "R", 1, 2, 0, 1));
        var second = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true, RepairActualScope.Create(Guid.NewGuid(), road.Id, "frame-v1", "R", 3, 4, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), project.Id, defect.Id, [first, second]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), first, RepairMode.Normal, actor.Id, UserRoleCode.ProjectManager, now, new("controlled plan", "v1"));
        var other = RepairItem.ProposeWithPlan(Guid.NewGuid(), second, RepairMode.Normal, actor.Id, UserRoleCode.ProjectManager, now, new("controlled plan", "v1"));
        package.AddItem(item); package.AddItem(other); db.Add(package); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        return new(project.Id, first.Id, item.Id, other.Id, actor.Id);
    }
}
