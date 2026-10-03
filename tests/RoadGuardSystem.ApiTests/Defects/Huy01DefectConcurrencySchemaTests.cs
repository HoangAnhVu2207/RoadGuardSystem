using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Defects;

[Trait("Package", "HUY-01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy01DefectConcurrencySchemaTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task FreshMigration_ProvidesDefectRowVersion()
    {
        await using var db = fixture.CreateDbContext();
        var length = await db.Database.SqlQuery<int?>($"SELECT CAST(COL_LENGTH('dbo.Defects', 'RowVersion') AS int) AS [Value]")
            .SingleAsync();
        Assert.Equal(8, length);
    }

    [Fact]
    public async Task BaselineUpgrade_PreservesDefectAndRejectsStaleUpdate()
    {
        var baseline = new AuthenticationSqlServerFixture();
        try
        {
            await baseline.InitializeAtMigrationAsync("20261003082408_Huy01SessionTransport");
            var defectId = Guid.NewGuid();
            await using (var db = baseline.CreateDbContext())
            {
                db.DefectTypes.Add(DefectType.Create("UPGRADE_TEST", "Upgrade test"));
                await db.SaveChangesAsync();
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO [Defects] ([Id],[DefectTypeCode],[Severity],[Status]) VALUES ({defectId},{"UPGRADE_TEST"},{(byte)1},{(byte)1})");
                await db.Database.MigrateAsync();
            }

            await using var first = baseline.CreateDbContext();
            await using var second = baseline.CreateDbContext();
            var one = await first.Defects.SingleAsync(item => item.Id == defectId);
            var two = await second.Defects.SingleAsync(item => item.Id == defectId);
            Assert.Equal("UPGRADE_TEST", one.DefectTypeCode);
            Assert.Equal(8, first.Entry(one).Property<byte[]>("RowVersion").CurrentValue?.Length);
            first.Entry(one).Property(item => item.ReportedAt).CurrentValue = DateTimeOffset.UtcNow;
            await first.SaveChangesAsync();
            second.Entry(two).Property(item => item.ReportedAt).CurrentValue = DateTimeOffset.UtcNow.AddSeconds(1);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await baseline.DisposeAsync();
        }
    }
}
