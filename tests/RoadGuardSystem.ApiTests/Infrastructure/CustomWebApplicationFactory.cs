using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace RoadGuardSystem.ApiTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for API integration/contract testing.
/// Dynamically registers the ApiTests assembly as an ApplicationPart so test probe
/// controllers are discovered without polluting production code.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string DisconnectedDatabase = "Server=127.0.0.1,1;Database=RoadGuard_PlatformTests_NoConnection;Integrated Security=true;Encrypt=true;TrustServerCertificate=false";
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
            DisconnectedDatabase);
        builder.UseSetting("RoadGuardDatabase:InitializeOnStartup", "false");
        builder.UseSetting("RoadGuardDatabase:SeedDevelopmentUsers", "false");
        builder.UseSetting("MinioStorage:Endpoint", "");
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

        builder.ConfigureServices((context, services) =>
        {
            // Development local configuration must never make these HTTP-only tests migrate or seed a database.
            var configuration = context.Configuration;
            configuration["RoadGuardDatabase:ConnectionString"] = DisconnectedDatabase;
            configuration["RoadGuardDatabase:InitializeOnStartup"] = "false";
            configuration["RoadGuardDatabase:SeedDevelopmentUsers"] = "false";
            configuration["MinioStorage:Endpoint"] = "";
            if (configuration.GetValue<bool>("RoadGuardDatabase:InitializeOnStartup") ||
                configuration.GetValue<bool>("RoadGuardDatabase:SeedDevelopmentUsers") ||
                configuration["RoadGuardDatabase:ConnectionString"] != DisconnectedDatabase ||
                !string.IsNullOrWhiteSpace(configuration["MinioStorage:Endpoint"]))
            {
                throw new InvalidOperationException("Platform test host isolation was overridden; refusing startup.");
            }

            services.RemoveAll<IHostedService>();
            services.AddControllers()
                .AddApplicationPart(typeof(CustomWebApplicationFactory).Assembly);
        });
    }
}
