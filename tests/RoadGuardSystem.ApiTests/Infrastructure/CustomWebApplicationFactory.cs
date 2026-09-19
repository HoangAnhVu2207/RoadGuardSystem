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

    public CustomWebApplicationFactory()
        : this("Production")
    {
    }

    internal CustomWebApplicationFactory(string environment)
    {
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting(
            "RoadGuardDatabase:ConnectionString",
            "Server=localhost;Database=RoadGuard_PlatformTests;Integrated Security=true;Encrypt=true;TrustServerCertificate=false");
        builder.UseSetting("RoadGuardDatabase:EnableSensitiveDataLogging", "false");
        builder.UseSetting("Jwt:Issuer", "roadguard-platform-tests");
        builder.UseSetting("Jwt:Audience", "roadguard-platform-clients");
        builder.UseSetting("Jwt:ActiveKeyId", "platform-test-current");
        builder.UseSetting(
            "Jwt:SigningKeys:platform-test-current",
            Convert.ToBase64String(Enumerable.Range(33, 32).Select(value => (byte)value).ToArray()));
        builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "10");
        builder.UseSetting("Jwt:SessionLifetimeHours", "8");
        builder.UseSetting("Jwt:RefreshTokenLifetimeDays", "30");
        builder.UseSetting(
            "PasswordChangeFingerprint:Key",
            Convert.ToBase64String(Enumerable.Repeat((byte)91, 32).ToArray()));

        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(CustomWebApplicationFactory).Assembly);
        });
    }
}
