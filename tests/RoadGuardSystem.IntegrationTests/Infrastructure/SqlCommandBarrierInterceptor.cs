using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class SqlCommandBarrierInterceptor : DbCommandInterceptor
{
    private readonly Func<string, bool> _matches;
    private readonly int _participantCount;
    private readonly ConcurrentDictionary<Guid, byte> _arrivals = new();
    private readonly TaskCompletionSource _allArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SqlCommandBarrierInterceptor(Func<string, bool> matches, int participantCount = 2)
    {
        _matches = matches;
        _participantCount = participantCount;
    }

    public int ArrivalCount => _arrivals.Count;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await WaitForCompetingCommandAsync(command, eventData, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await WaitForCompetingCommandAsync(command, eventData, cancellationToken);
        return result;
    }

    private async Task WaitForCompetingCommandAsync(
        DbCommand command,
        CommandEventData eventData,
        CancellationToken cancellationToken)
    {
        if (!_matches(command.CommandText) || !_arrivals.TryAdd(eventData.CommandId, 0))
        {
            return;
        }

        if (_arrivals.Count >= _participantCount)
        {
            _allArrived.TrySetResult();
        }

        await _allArrived.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
    }
}
