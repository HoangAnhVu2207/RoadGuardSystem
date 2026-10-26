using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Cases;
using Xunit;
namespace RoadGuardSystem.ApiTests.Cases;

public sealed class PublicPublicationMigrationTests
{
    [Fact]
    public async Task AdditiveUpgradeRetainsExistingIdentityAndRepeatedMigrationIsSafe()
    {
        var sql = new AuthenticationSqlServerFixture();
        try
        {
            await sql.InitializeAtMigrationAsync("20261009043010_AllowVerifiedDefectPostRepairTasks");
            var actor = await sql.CreateUserAsync("c1-upgrade-" + Guid.NewGuid().ToString("N"), "Current1!");
            await using (var db = sql.CreateDbContext())
            {
                Assert.DoesNotContain("20261022074647_C1ControlledPublicPublication", await db.Database.GetAppliedMigrationsAsync());
                await db.GetService<IMigrator>().MigrateAsync();
                Assert.True(await db.Users.AnyAsync(x => x.Id == actor.Id));
                Assert.Empty(await db.Set<PublicPublication>().ToArrayAsync());
                await db.GetService<IMigrator>().MigrateAsync();
                Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
                Assert.True(await db.Users.AnyAsync(x => x.Id == actor.Id));
            }
        }
        finally { await sql.DisposeAsync(); }
    }
}
