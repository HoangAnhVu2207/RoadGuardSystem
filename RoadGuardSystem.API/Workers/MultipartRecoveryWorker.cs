using RoadGuardSystem.Repositories.Files;

namespace RoadGuardSystem.API.Workers;

public sealed class MultipartRecoveryWorker(IServiceScopeFactory scopes, ILogger<MultipartRecoveryWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogFailure = LoggerMessage.Define(LogLevel.Warning,
        new EventId(1, "MultipartRecoveryFailed"), "Multipart recovery iteration failed; durable work remains retryable.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IUploadRepository>().RecoverMultipartsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { LogFailure(logger, null); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
