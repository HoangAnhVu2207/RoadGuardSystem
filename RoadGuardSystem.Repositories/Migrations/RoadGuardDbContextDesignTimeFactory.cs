using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RoadGuardSystem.Repositories.Migrations;

public sealed class RoadGuardDbContextDesignTimeFactory : IDesignTimeDbContextFactory<RoadGuardDbContext>
{
    private const string ConnectionVariable = "ROADGUARD_MIGRATION_CONNECTION_STRING";

    public RoadGuardDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{ConnectionVariable} must be configured for design-time migration commands.");
        }

        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new RoadGuardDbContext(options);
    }
}
