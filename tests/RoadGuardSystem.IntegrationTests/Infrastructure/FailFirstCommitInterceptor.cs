using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class FailFirstCommitInterceptor : DbTransactionInterceptor
{
    private int _remainingFailures = 1;

    public int FailureCount { get; private set; }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _remainingFailures, 0) == 1)
        {
            FailureCount++;
            throw new TestTransientException("Injected transient failure before transaction commit.");
        }

        return ValueTask.FromResult(result);
    }
}
