using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding.Sandbox;

namespace RoadGuardSystem.IntegrationTests.Seeding;

internal static class SandboxSeedTestSupport
{
    internal static RoadGuardDbContext CreateSafeContext(SqlServerTestFixture fixture) => new(
        new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString,
            o => o.UseNetTopologySuite().EnableRetryOnFailure()).Options);

    internal static async Task<SandboxSeedTarget> CreateOwnedTargetAsync(SqlServerTestFixture fixture)
    {
        var identity = fixture.GetOwnedSeedDatabaseIdentity();
        await using var db = CreateSafeContext(fixture);
        await db.Database.MigrateAsync();
        // Ownership derives from the live fixture, never from an arbitrary supplied connection.
        await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_addextendedproperty @name=N'RoadGuardSandboxOwner', @value={identity.OwnerId.ToString("N")}");
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'));";
        var server = (string)(await command.ExecuteScalarAsync())!;
        return new("Test", identity.DatabaseName, server, identity.OwnerId, identity.ContainerId);
    }
}
