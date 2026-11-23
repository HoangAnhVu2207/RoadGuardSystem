using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing.Worker;
using RoadGuardSystem.API.Workers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Processing.Anh02;

namespace RoadGuardSystem.API.Extensions;

public static class AiRealWorkerExtensions
{
    // Explicit opt-in: no model, secret read or network access when disabled.
    public static IServiceCollection AddAiRealWorker(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new AiWorkerRuntimeOptions();
        configuration.GetSection("AiRealWorker").Bind(options);
        services.AddScoped<IAiVideoWorkRepository, AiVideoWorkRepository>();
        services.AddScoped<IAiVideoTelemetryRepository, AiVideoTelemetryRepository>();
        services.AddScoped<AiVideoTelemetryService>();
        services.AddScoped<IRealAiCandidateRepository, RealAiCandidateRepository>();
        services.AddScoped<RealAiCandidateFactsReader>();
        services.RemoveAll<IAiCandidateFactsReader>();
        services.AddScoped<IAiCandidateFactsReader>(provider => new CompositeAiCandidateFactsReader(
            provider.GetRequiredService<RealAiCandidateFactsReader>(), provider.GetRequiredService<Anh02AiService>()));
        if (!options.Enabled) return services;
        if (!Path.IsPathFullyQualified(options.ServiceTokenFile)) throw new InvalidOperationException("AI worker requires an absolute secret file.");
        var token = File.ReadAllText(options.ServiceTokenFile).TrimEnd('\r', '\n');
        services.AddSingleton(options);
        services.AddSingleton<WorkerTransport>();
        services.AddSingleton(provider => new AiWorkerClient(provider.GetRequiredService<WorkerTransport>().Client,
            new Uri(options.WorkerEndpoint), token, options.TransportTimeout));
        services.AddSingleton<IAiWorkerMediaProbe>(new AiWorkerMediaProbe(options.FfprobePath));
        services.AddScoped<AiVideoCoordinator>();
        services.AddHostedService<AiRealVideoWorker>();
        return services;
    }

    private sealed class WorkerTransport : IDisposable
    {
        public HttpClient Client { get; } = AiWorkerClient.CreateHttpClient();
        public void Dispose() => Client.Dispose();
    }
}
