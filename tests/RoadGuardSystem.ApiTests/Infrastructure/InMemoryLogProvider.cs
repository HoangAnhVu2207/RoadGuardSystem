using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal sealed class InMemoryLogProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public string CombinedEntries => string.Join(Environment.NewLine, _entries);

    public ILogger CreateLogger(string categoryName) => new SinkLogger(_entries);

    public void Dispose()
    {
    }

    private sealed class SinkLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _entries;

        public SinkLogger(ConcurrentQueue<string> entries)
        {
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Enqueue(formatter(state, exception));
            if (exception is not null)
            {
                _entries.Enqueue(exception.ToString());
            }
        }
    }
}
