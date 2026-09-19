using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class ThrowOnceCommandInterceptor : DbCommandInterceptor
{
    private readonly Func<string, bool> _matches;
    private int _armed = 1;

    public ThrowOnceCommandInterceptor(Func<string, bool> matches)
    {
        _matches = matches;
    }

    public int FailureCount { get; private set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (_matches(command.CommandText) && Interlocked.Exchange(ref _armed, 0) == 1)
        {
            FailureCount++;
            throw new TestTransientException("Injected transient command failure before SaveChanges.");
        }

        return ValueTask.FromResult(result);
    }
}
