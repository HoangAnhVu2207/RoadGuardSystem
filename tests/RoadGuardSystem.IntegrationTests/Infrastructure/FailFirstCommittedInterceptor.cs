using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class FailFirstCommittedInterceptor : DbTransactionInterceptor
{
    private int _remainingFailures = 1;

    public int FailureCount { get; private set; }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _remainingFailures, 0) == 1)
        {
            FailureCount++;
            throw new TestTransientException("Injected transient failure after transaction commit.");
        }

        return Task.CompletedTask;
    }
}
