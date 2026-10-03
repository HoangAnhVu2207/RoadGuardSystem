using RoadGuardSystem.Services.Processing.Anh02;
namespace RoadGuardSystem.API.Workers;
public sealed class AiMockRunWorker(IServiceScopeFactory scopes, ILogger<AiMockRunWorker> logger, TimeProvider clock) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> Failure = LoggerMessage.Define(LogLevel.Warning,
        new EventId(1, "AiMockIterationFailed"), "AI mock worker iteration failed; the same durable run remains recoverable.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { Failure(logger, ex); }
            await Task.Delay(TimeSpan.FromSeconds(5), clock, stoppingToken);
        }
    }
}
