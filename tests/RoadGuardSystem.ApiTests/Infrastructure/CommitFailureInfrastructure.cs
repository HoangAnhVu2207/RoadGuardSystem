using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using RoadGuardSystem.Repositories;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal static class CommitFailureDbContext
{
    public static RoadGuardDbContext Create(string connectionString, DbTransactionInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseNetTopologySuite())
            .ReplaceService<IExecutionStrategyFactory, CommitFailureExecutionStrategyFactory>()
            .AddInterceptors(interceptor)
            .Options;
        return new RoadGuardDbContext(options);
    }
}

internal abstract class FailOnceTransactionInterceptor : DbTransactionInterceptor
{
    private int _remainingFailures = 1;

    public int FailureCount { get; private set; }

    protected void FailOnce(string message)
    {
        if (Interlocked.Exchange(ref _remainingFailures, 0) == 1)
        {
            FailureCount++;
            throw new CommitFailureTransientException(message);
        }
    }
}

internal sealed class FailOnceBeforeCommitInterceptor : FailOnceTransactionInterceptor
{
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        FailOnce("Injected transient failure before transaction commit.");
        return ValueTask.FromResult(result);
    }
}

internal sealed class FailOnceAfterCommitInterceptor : FailOnceTransactionInterceptor
{
    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        FailOnce("Injected transient failure after transaction commit.");
        return Task.CompletedTask;
    }
}

internal sealed class CommitFailureExecutionStrategyFactory : IExecutionStrategyFactory
{
    private readonly ExecutionStrategyDependencies _dependencies;

    public CommitFailureExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public IExecutionStrategy Create() => new CommitFailureExecutionStrategy(_dependencies);
}

internal sealed class CommitFailureExecutionStrategy : ExecutionStrategy
{
    public CommitFailureExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : base(dependencies, maxRetryCount: 2, maxRetryDelay: TimeSpan.Zero)
    {
    }

    protected override bool ShouldRetryOn(Exception exception) =>
        exception is CommitFailureTransientException;
}

internal sealed class CommitFailureTransientException : Exception
{
    public CommitFailureTransientException(string message)
        : base(message)
    {
    }
}
