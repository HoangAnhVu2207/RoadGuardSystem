using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;

namespace RoadGuardSystem.API.Workers;

public sealed class H6NotificationWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<H6NotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly Action<ILogger, Exception?> LogIterationFailure = LoggerMessage.Define(
        LogLevel.Error, new EventId(6101, nameof(H6NotificationWorker)),
        "H6 notification worker iteration failed; durable work remains retryable.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var run = Guid.NewGuid();
        var scheduled = NotificationCalendarPolicy.NextWeeklyReview(clock.GetUtcNow());
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IH6NotificationDispatchRepository>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<H6NotificationDispatchService>();
                var unknown = scope.ServiceProvider.GetRequiredService<IH6NotificationUnknownAuditRepository>();
                var observed = clock.GetUtcNow().ToUniversalTime();
                NotificationScheduledCallbackWitness? witness = null;
                if (observed >= scheduled)
                {
                    witness = new(run, scheduled, observed, observed < scheduled.AddMinutes(1));
                    scheduled = NotificationCalendarPolicy.NextWeeklyReview(observed);
                }
                await repository.ObserveCalendarAsync(run, witness, stoppingToken);
                await repository.ObserveClocksAsync(stoppingToken);
                await repository.RetryUnresolvedAsync(stoppingToken);
                await unknown.AuditUnregisteredAsync(stoppingToken);
                for (var i = 0; i < 100; i++)
                    if ((await dispatcher.ProcessOneAsync(stoppingToken)).Status == "IDLE") break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                LogIterationFailure(logger, exception);
            }
            await Task.Delay(Interval, clock, stoppingToken);
        }
    }
}
