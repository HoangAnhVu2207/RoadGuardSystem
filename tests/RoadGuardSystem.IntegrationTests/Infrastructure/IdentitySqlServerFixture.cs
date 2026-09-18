using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class IdentitySqlServerFixture : IAsyncLifetime
{
    private readonly SqlServerTestFixture _database = new();

    public string ConnectionString => _database.ConnectionString;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        await using (var baselineContext = _database.CreateDbContext())
        {
            await baselineContext.Database.EnsureDeletedAsync();
        }

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public RoadGuardDbContext CreateDbContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(ConnectionString, x => x.UseNetTopologySuite())
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        return new RoadGuardDbContext(optionsBuilder.Options);
    }

    public IIdentityRepository CreateRepository(RoadGuardDbContext context)
    {
        return new IdentityRepository(context);
    }

    public async Task SeedRolesAsync(RoadGuardDbContext context)
    {
        var step = new IdentityRoleSeedStep();
        await step.SeedAsync(context, CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }
}
