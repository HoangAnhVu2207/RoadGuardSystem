using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RoadGuardSystem.API.Extensions;
using RoadGuardSystem.API.Workers;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Startup;

public sealed class ServerDeploymentTests
{
    private static IConfiguration Config(string origin = "https://fe.example.com", string? keyPath = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerDeployment:Enabled"] = "true",
            ["ServerDeployment:AllowedOrigins:0"] = origin,
            ["ServerDeployment:KeyPath"] = keyPath ?? Path.GetTempPath()
        }).Build();

    [Fact]
    public void DisabledCompositionDoesNotRequireProductionOrRegisterDependencies()
    {
        var services = new ServiceCollection();
        services.AddRoadGuardServerDeployment(new ConfigurationBuilder().Build(), "Development");
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("http://fe.example.com")]
    [InlineData("https://*.example.com")]
    [InlineData("https://fe.example.com/")]
    [InlineData("https://fe.example.com/path")]
    [InlineData("https://fe.example.com?query=1")]
    [InlineData("https://user@fe.example.com")]
    [InlineData("")]
    public void RejectsNonExplicitHttpsOrigin(string origin) =>
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRoadGuardServerDeployment(Config(origin), "Production"));

    [Fact]
    public void RequiresProductionAndAbsolutePersistentKeyPath()
    {
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRoadGuardServerDeployment(Config(), "Development"));
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRoadGuardServerDeployment(Config(keyPath: "relative"), "Production"));
    }

    [Fact]
    public void ExplicitPolicyAndKeysAreRegisteredWithoutAnExtraVerifier()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRoadGuardServerDeployment(Config(), "Production");
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(ServerDeployment.CorsPolicy)!;
        Assert.True(policy.SupportsCredentials);
        Assert.True(policy.IsOriginAllowed("https://fe.example.com"));
        Assert.False(policy.IsOriginAllowed("https://denied.example.com"));
        Assert.Equal("RoadGuardSystem.Server", provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
        var repository = Assert.IsType<FileSystemXmlRepository>(provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository);
        Assert.Equal(new DirectoryInfo(Path.GetTempPath()).FullName, repository.Directory.FullName);
        Assert.DoesNotContain(services, x => x.ImplementationType == typeof(UploadVerificationWorker));
    }

    [Fact]
    public async Task ProductionProgramComposesOneVerifierAndRealWorkerWithoutStartupMigration()
    {
        var token = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(token, new string('t', 48));
            await using var factory = new ServerFactory(token);
            using var client = factory.CreateClient();
            Assert.Equal(1, factory.Verifiers);
            Assert.Equal(1, factory.RealWorkers);
            // Disconnected SQL configured by the fixture means startup migration would fail.
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/api/v1/projects")
            {
                Headers = { { "Origin", "https://fe.example.com" }, { "Access-Control-Request-Method", "GET" } }
            });
            Assert.Contains("https://fe.example.com", response.Headers.GetValues("Access-Control-Allow-Origin"));
        }
        finally { File.Delete(token); }
    }

    private sealed class ServerFactory(string token) : CustomWebApplicationFactory("Production")
    {
        public int Verifiers { get; private set; }
        public int RealWorkers { get; private set; }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices((_, services) =>
            {
                Verifiers = services.Count(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(UploadVerificationWorker));
                RealWorkers = services.Count(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(AiRealVideoWorker));
            });
            base.ConfigureWebHost(builder);
            builder.UseSetting("ServerDeployment:Enabled", "true");
            builder.UseSetting("ServerDeployment:AllowedOrigins:0", "https://fe.example.com");
            builder.UseSetting("ServerDeployment:KeyPath", Path.GetTempPath());
            builder.UseSetting("MinioStorage:Endpoint", "127.0.0.1:1");
            builder.UseSetting("AiRealWorker:Enabled", "true");
            builder.UseSetting("AiRealWorker:ServiceTokenFile", token);
        }
    }
}
