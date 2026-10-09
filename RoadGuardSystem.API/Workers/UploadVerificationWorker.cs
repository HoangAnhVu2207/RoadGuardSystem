using RoadGuardSystem.Services.Files;

namespace RoadGuardSystem.API.Workers;

public sealed class UploadVerificationWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly Action<ILogger, Exception?> LogIterationFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, "UploadVerificationIterationFailed"),
        "Upload verification worker iteration failed.");
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UploadVerificationWorker> _logger;

    public UploadVerificationWorker(IServiceScopeFactory scopeFactory, ILogger<UploadVerificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        var round = new UploadVerificationRound();
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IUploadService>()
                    .ProcessOneVerificationInRoundAsync(round, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogIterationFailure(_logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
