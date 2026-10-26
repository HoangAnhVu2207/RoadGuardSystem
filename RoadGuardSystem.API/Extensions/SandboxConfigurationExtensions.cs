using Microsoft.AspNetCore.DataProtection;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.API.Extensions;

public static class SandboxConfigurationExtensions
{
    public static IServiceCollection AddRoadGuardSandbox(this IServiceCollection services,
        IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!configuration.GetValue("Sandbox:Enabled", false)) return services;
        var host = configuration["Sandbox:MailHost"];
        var keys = configuration["Sandbox:KeyPath"];
        if (!string.Equals(environmentName, "Development", StringComparison.Ordinal) ||
            !string.Equals(host, "mailpit", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(keys))
            throw new InvalidOperationException("Sandbox requires Development, local Mailpit and a persistent key directory.");
        services.Configure<SandboxMailOptions>(options => { options.Enabled = true; options.Host = host!; });
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys))
            .SetApplicationName("RoadGuardSystem");
        services.AddHostedService<SandboxStorageInitializer>();
        return services;
    }
}
