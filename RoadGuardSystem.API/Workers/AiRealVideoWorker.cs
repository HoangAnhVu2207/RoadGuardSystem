using RoadGuardSystem.Services.Processing.Worker;

namespace RoadGuardSystem.API.Workers;

public sealed class AiRealVideoWorker(IServiceScopeFactory scopes, ILogger<AiRealVideoWorker> logger, TimeProvider clock) : BackgroundService
{
    private readonly string _owner = "be-real-" + Guid.NewGuid().ToString("N");
    private static readonly Action<ILogger, Exception?> Failure = LoggerMessage.Define(LogLevel.Warning,
        new EventId(1, "AiRealIterationFailed"), "REAL AI iteration failed; durable work remains eligible for recovery.");
    private static readonly Action<ILogger, Exception?> SourcePending = LoggerMessage.Define(LogLevel.Information,
        new EventId(2, "AiRealSourcePending"), "REAL AI source pending: GEOMETRY_NOT_READY; source remains eligible for preparation.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Resolve once before entering the retry loop so invalid runtime settings fail startup.
        using (var readiness = scopes.CreateScope()) _ = readiness.ServiceProvider.GetRequiredService<AiVideoCoordinator>().LogicalConfigurationIdentity;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<AiVideoCoordinator>().TickAsync(_owner, stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (AiVideoSourceNotReadyException) { SourcePending(logger, null); }
            catch (Exception) { Failure(logger, null); } // Never log exception text containing object paths, service tokens or media metadata.
            await Task.Delay(TimeSpan.FromSeconds(5), clock, stoppingToken);
        }
    }
}
