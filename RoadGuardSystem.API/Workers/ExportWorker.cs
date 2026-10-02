using RoadGuardSystem.Services.Exports;
using Microsoft.Extensions.Options;

namespace RoadGuardSystem.API.Workers;

public sealed class ExportWorker(IServiceScopeFactory scopes, ILogger<ExportWorker> logger, TimeProvider clock, IOptions<ExportOptions> options) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> Failure = LoggerMessage.Define(LogLevel.Warning, new EventId(1, "ExportWorkerFailed"), "Export worker failed; durable lease permits recovery.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(stoppingToken)) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { Failure(logger, ex); }
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.Value.WorkerPollSeconds)), clock, stoppingToken);
        }
    }
}
