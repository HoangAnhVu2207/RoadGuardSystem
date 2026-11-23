using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.API.Extensions;
using RoadGuardSystem.API.Workers;
using Xunit;

namespace RoadGuardSystem.ApiTests.Deployment;

public sealed class ServerDeploymentTests
{
    [Fact]
    public void Disabled_does_not_register_server_services()
    {
        var services = new ServiceCollection();
        services.AddRoadGuardServerDeployment(Config(false), "Production");
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("Development", "https://192.168.1.11:8443")]
    [InlineData("Production", "*")]
    [InlineData("Production", "http://192.168.1.11:5173")]
    [InlineData("Production", "https://example.com/path")]
    [InlineData("Production", "https://example.com/")]
    [InlineData("Production", "https://user@example.com")]
    [InlineData("Production", "https://*.example.com")]
    [InlineData("Production", "")]
    public void Invalid_server_configuration_fails_closed(string environment, string origin)
    {
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection()
            .AddRoadGuardServerDeployment(Config(true, origin), environment));
    }

    [Fact]
    public void Production_registers_live_upload_worker_and_dependency_health_check()
    {
        var services = new ServiceCollection();
        services.AddRoadGuardServerDeployment(Config(true), "Production");
        Assert.Single(services.Where(x => x.ServiceType == typeof(IHostedService) &&
            x.ImplementationType == typeof(UploadVerificationWorker)));
        Assert.Contains(services, x => x.ServiceType == typeof(ServerDependencyHealthCheck));
    }

    [Theory]
    [InlineData("Sandbox:Enabled", "true")]
    [InlineData("Anh02:MockEnabled", "true")]
    [InlineData("Handoff:Enabled", "true")]
    [InlineData("ServerDeployment:KeyPath", "relative-keys")]
    public void Unsafe_composition_is_rejected(string key, string value)
    {
        var config = Config(true);
        config[key] = value;
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection()
            .AddRoadGuardServerDeployment(config, "Production"));
    }

    [Theory]
    [InlineData("Development", true, "initialize")]
    [InlineData("Production", false, "initialize")]
    [InlineData("Production", true, "reset")]
    public async Task Maintenance_rejects_unapproved_modes_before_accessing_dependencies(string environment, bool enabled, string command)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerDeployment:Enabled"] = enabled.ToString(),
            ["ServerDeployment:Maintenance"] = command
        });
        await using var app = builder.Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => ServerDeployment.TryExecuteMaintenanceAsync(app));
    }

    [Fact]
    public async Task Normal_startup_does_not_access_maintenance_dependencies()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        await using var app = builder.Build();
        Assert.Null(await ServerDeployment.TryExecuteMaintenanceAsync(app));
    }

    private static IConfiguration Config(bool enabled, string origin = "https://192.168.1.11:8443")
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerDeployment:Enabled"] = enabled.ToString(),
            ["ServerDeployment:AllowedOrigins:0"] = origin,
            ["ServerDeployment:KeyPath"] = Path.GetTempPath()
        }).Build();
}
