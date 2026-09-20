using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-02")]
public sealed class P202MigrationLifecycleTests
{
    [Fact(DisplayName = "P2-02 Positive: migration applies to empty database downgrades and reapplies")]
    public async Task Migration_AppliesDowngradesAndReapplies()
    {
        var fixture = new SqlServerTestFixture();
        await fixture.InitializeAsync();
        try
        {
            await using (var baseline = fixture.CreateDbContext())
            {
                await baseline.Database.EnsureDeletedAsync();
            }

            await using var context = CreateContext(fixture.ConnectionString);
            await context.Database.MigrateAsync();
            (await CountP202TablesAsync(context)).Should().Be(4);

            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(Migration.InitialDatabase);
            (await CountP202TablesAsync(context)).Should().Be(0);

            await context.Database.MigrateAsync();
            (await CountP202TablesAsync(context)).Should().Be(4);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static RoadGuardDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseNetTopologySuite())
            .Options;
        return new RoadGuardDbContext(options);
    }

    private static async Task<int> CountP202TablesAsync(RoadGuardDbContext context)
        => await context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('AuditLogs', 'IdempotencyRecords', 'OutboxMessages', 'ConsumerEffectReceipts')
                """)
            .SingleAsync();
}
