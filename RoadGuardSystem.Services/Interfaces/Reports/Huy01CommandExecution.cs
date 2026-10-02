using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Services.Reports;

internal static class Huy01CommandExecution
{
    // A competing same-key mutation can commit while this handler is waiting
    // for its parent/head lock. A stale exception is observed only after the
    // shared transaction has rolled back. Re-enter the shared guarded lookup;
    // the fallback handler only rethrows and cannot create a second effect.
    internal static async Task<IdempotencyOperationResult> ExecuteAsync(
        Func<Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>>, Task<IdempotencyOperationResult>> execute,
        Func<CancellationToken, Task<(Guid OperationId, string OutcomeJson)>> handler, Func<Exception, bool> stale)
    {
        Exception? handlerFailure = null;
        try
        {
            return await execute(async token =>
            {
                try { return await handler(token); }
                catch (Exception exception) { handlerFailure = exception; throw; }
            });
        }
        catch (Exception exception) when (ReferenceEquals(exception, handlerFailure) && stale(exception))
        {
            return await execute(_ => Task.FromException<(Guid, string)>(exception));
        }
    }
}
