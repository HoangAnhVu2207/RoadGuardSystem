using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.API.BackgroundServices;

public sealed partial class ValidationRunWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ValidationRunWorker> _logger;

    public ValidationRunWorker(IServiceScopeFactory scopeFactory, ILogger<ValidationRunWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IProcessingV2Repository>();
                if (await repository.CompleteNextValidationRunAsync(stoppingToken))
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogFailure(_logger, exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Validation run worker failed.")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
