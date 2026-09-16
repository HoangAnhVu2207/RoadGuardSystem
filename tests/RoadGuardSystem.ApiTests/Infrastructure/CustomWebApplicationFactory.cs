using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace RoadGuardSystem.ApiTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for API integration/contract testing.
/// Dynamically registers the ApiTests assembly as an ApplicationPart so test probe
/// controllers are discovered without polluting production code.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;

    public CustomWebApplicationFactory(string environment = "Production")
    {
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(CustomWebApplicationFactory).Assembly);
        });
    }
}
