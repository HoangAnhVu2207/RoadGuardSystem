using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoadGuardSystem.ApiTests.Infrastructure;

namespace HostedAcceptance;

// Configure ordinary Program before registration. Never replace services or manually add workers.
internal sealed class HostedFactory(Dictionary<string, string> settings) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (!AuthenticationSqlServerFixture.IsOwnedConnectionString(settings["RoadGuardDatabase:ConnectionString"]))
            throw new InvalidOperationException("Only an active owned SQL fixture is permitted.");
        builder.UseEnvironment("Testing");
        foreach (var entry in settings) builder.UseSetting(entry.Key, entry.Value);
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
