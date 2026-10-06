using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Implementations.Retention;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Retention;

public sealed class H4H5RetentionInventorySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task InventoryReadsAllMappedRepairOfflineTablesWithoutInventingReferences()
    {
        await using var db = sql.CreateDbContext();
        var inventory = new Huy02InspectionRetentionContributor(db);
        var unknown = await inventory.ReadAsync(Guid.NewGuid(), default);
        Assert.True(unknown.Complete); Assert.Empty(unknown.References);
        Assert.Empty(await inventory.KnownProjectFilesAsync(Guid.NewGuid(), default));
    }
}
