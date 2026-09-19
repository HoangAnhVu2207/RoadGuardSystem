using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class PauseOnceCommandInterceptor : DbCommandInterceptor
{
    private readonly Func<string, bool> _matches;
    private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _armed = 1;

    public PauseOnceCommandInterceptor(Func<string, bool> matches)
    {
        _matches = matches;
    }

    public Task Reached => _reached.Task;

    public void Release() => _release.TrySetResult();

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (_matches(command.CommandText) && Interlocked.Exchange(ref _armed, 0) == 1)
        {
            _reached.TrySetResult();
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        }

        return result;
    }
}
