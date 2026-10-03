using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RoadGuardSystem.ApiTests.Infrastructure;

// Test-only barrier at first receipt read: all HTTP/service preflight checks have completed.
internal sealed class Anh02ReceiptRevocationInterceptor(Func<CancellationToken, Task> revoke) : DbCommandInterceptor
{
    public bool Armed { get; set; }
    public int Calls { get; private set; }
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (Armed && command.CommandText.Contains("FROM [IdempotencyRecords]", StringComparison.Ordinal))
        { Armed = false; Calls++; await revoke(cancellationToken); }
        return result;
    }
}
