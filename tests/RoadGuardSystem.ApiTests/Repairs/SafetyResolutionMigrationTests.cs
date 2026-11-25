using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

public sealed class SafetyResolutionMigrationTests
{
    [Fact]
    public async Task ImmediateUpgradePreservesLegacyObligationsWithoutInventingResolution()
    {
        await using var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAtMigrationAsync("20261125053521_ReportMatchingLocationConfirmation");
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        // Insert a legacy package using its then-current shape: the new column does
        // not exist yet. No business resolution is manufactured by this fixture.
        var package = Guid.NewGuid(); var obligation = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPackages(Id,ProjectId,DefectId,MutationRevision) VALUES({package},{source.Project},{source.Defect},0)");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligations(Id,ProjectId,DefectId,Kind,Mandatory,PackageId) VALUES({obligation},{source.Project},{source.Defect},2,1,{package})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairActualScopes(Id,PhysicalRoadId,LocationVersion,RouteLabel,[From],[To],OffsetFrom,OffsetTo,ObligationId) VALUES({Guid.NewGuid()},{source.Road},N'legacy-frame',N'legacy-road',1,2,0,1,{obligation})");
        var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync();
        var row = await db.RepairObligations.AsNoTracking().SingleAsync(o => o.Id == obligation);
        Assert.Null(row.SafetyResolutionId); Assert.False(row.IsResolved);
        Assert.Equal(RepairObligationKind.TemporarySafety, row.Kind); Assert.True(row.Mandatory);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync()); Assert.False(db.Database.HasPendingModelChanges());
        var schema = await SandboxSeedGuard.ReadSchemaAsync(db);
        Assert.Equal("20261125072411_TemporarySafetyResolution", schema.Migrations[^1]);
        Assert.Equal(198, schema.Tables.Length);
        await migrator.MigrateAsync("20261125053521_ReportMatchingLocationConfirmation"); await migrator.MigrateAsync();
        Assert.Null(await db.RepairObligations.Where(o => o.Id == obligation).Select(o => o.SafetyResolutionId).SingleAsync());
    }
}
