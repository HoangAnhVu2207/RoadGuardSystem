using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
        => CreateDbContext(enableTestRetries: false, interceptors);

    public RoadGuardDbContext CreateRetryingDbContext(params IInterceptor[] interceptors)
        => CreateDbContext(enableTestRetries: true, interceptors);

    private RoadGuardDbContext CreateDbContext(bool enableTestRetries, params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(ConnectionString, x => x.UseNetTopologySuite())
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        if (enableTestRetries)
        {
            optionsBuilder.ReplaceService<IExecutionStrategyFactory, TestRetryingExecutionStrategyFactory>();
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

internal sealed class TestRetryingExecutionStrategyFactory : IExecutionStrategyFactory
{
    private readonly ExecutionStrategyDependencies _dependencies;

    public TestRetryingExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public IExecutionStrategy Create() => new TestRetryingExecutionStrategy(_dependencies);
}

internal sealed class TestRetryingExecutionStrategy : ExecutionStrategy
{
    public TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : base(dependencies, maxRetryCount: 2, maxRetryDelay: TimeSpan.Zero)
    {
    }

    protected override bool ShouldRetryOn(Exception exception) => exception is TestTransientException;
}

internal sealed class TestTransientException : Exception
{
    public TestTransientException(string message)
        : base(message)
    {
    }
}
