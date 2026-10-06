using RoadGuardSystem.Services.Retention;
namespace RoadGuardSystem.API.Workers;

public sealed class RetentionEvaluationWorker(IServiceScopeFactory scopes, ILogger<RetentionEvaluationWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> Failure = LoggerMessage.Define(LogLevel.Error, new EventId(1, "RetentionEvaluationFailed"), "Retention evaluation worker iteration failed.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IRetentionService>().ProcessOneAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { Failure(logger, exception); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
