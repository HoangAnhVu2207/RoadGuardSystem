using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using System.Text.Json;
using Xunit;

namespace RoadGuardSystem.ApiTests.Cases;

public sealed class ReporterRoutingMigrationTests
{
    [Fact]
    public async Task UpgradeFromPreviousMigrationAddsEmptyImmutableHistoryAndPreservesLegacyProject()
    {
        var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAtMigrationAsync("20261125072411_TemporarySafetyResolution");
        try
        {
            var id = Guid.NewGuid();
            await using (var old = sql.CreateDbContext())
            {
                old.Projects.Add(Project.Create(id, "ROUTING-UPGRADE-" + id.ToString("N"), "Existing project survives", null, 32648,
                    null, null, DateTimeOffset.UtcNow));
                await old.SaveChangesAsync();
                await old.GetService<IMigrator>().MigrateAsync();
            }
            await using var upgraded = sql.CreateDbContext();
            var differences = upgraded.GetService<IMigrationsModelDiffer>().GetDifferences(
                upgraded.GetService<IModelRuntimeInitializer>().Initialize(upgraded.GetService<IMigrationsAssembly>().ModelSnapshot!.Model, true).GetRelationalModel(),
                upgraded.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model.GetRelationalModel());
            Assert.True(differences.Count == 0, string.Join("; ", differences.Select(d =>
                d.GetType().Name + ":" + JsonSerializer.Serialize(d, d.GetType()))));
            Assert.Empty(await upgraded.Database.GetPendingMigrationsAsync());
            Assert.Equal("Existing project survives", (await upgraded.Projects.SingleAsync(p => p.Id == id)).Name);
            Assert.False(await upgraded.Set<CaseRoutingEvent>().AnyAsync());
            var definition = await upgraded.Database.SqlQuery<string>($"SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.TR_CaseRoutingEvents_Immutable')) AS [Value]").SingleAsync();
            Assert.Contains("THROW 51560", definition, StringComparison.Ordinal);
            var schema = await SandboxSeedGuard.ReadSchemaAsync(upgraded);
            Assert.Contains("dbo.CaseRoutingEvents", schema.Tables);
            var destination = Environment.GetEnvironmentVariable("RG_ROUTING_SCHEMA_EVIDENCE");
            if (destination is not null) await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(schema));
        }
        finally { await sql.DisposeAsync(); }
    }
}
