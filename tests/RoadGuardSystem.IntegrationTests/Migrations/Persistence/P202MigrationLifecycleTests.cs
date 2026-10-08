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
    [Fact(DisplayName = "P2-02 Positive: current EF model matches the latest migration snapshot")]
    public async Task CurrentModel_HasNoPendingMigrationChanges()
    {
        await using var context = CreateContext("Server=localhost;Database=model_only;Trusted_Connection=True;TrustServerCertificate=True");
        context.Database.HasPendingModelChanges().Should().BeFalse();
    }

    private static RoadGuardDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseNetTopologySuite())
            .Options;
        return new RoadGuardDbContext(options);
    }

}
