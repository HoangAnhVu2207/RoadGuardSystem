using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.API.Extensions;
using RoadGuardSystem.Services.Options;
using RoadGuardSystem.Services.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class SandboxConfigurationTests
{
    [Theory]
    [InlineData("Production", "mailpit", "/keys")]
    [InlineData("Testing", "mailpit", "/keys")]
    [InlineData("Development", "smtp.gmail.com", "/keys")]
    [InlineData("Development", "mailpit", "")]
    public void UnsafeSandboxConfigurationIsRejected(string environment, string host, string keys)
    {
        var config = Configuration(host, keys);
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRoadGuardSandbox(config, environment));
    }

    [Fact]
    public void ExplicitDevelopmentSandboxUsesLocalCaptureOnly()
    {
        var services = new ServiceCollection();
        services.AddRoadGuardSandbox(Configuration("mailpit", "/keys"), "Development");
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SandboxMailOptions>>().Value;
        Assert.True(options.Enabled);
        Assert.Equal("mailpit", options.Host);
    }

    [Fact]
    public void OrdinaryProductionConfigurationDoesNotEnableSandbox()
    {
        var services = new ServiceCollection();
        services.AddRoadGuardSandbox(new ConfigurationBuilder().Build(), "Production");
        Assert.Empty(services);
    }

    [Fact]
    public void CaptureTransportCannotBeRedirectedToExternalSmtp()
    {
        Assert.Throws<InvalidOperationException>(() => new GmailIdentityMessageSender(
            Options.Create(new IdentityOnboardingOptions()),
            Options.Create(new SandboxMailOptions { Enabled = true, Host = "smtp.gmail.com" })));
    }

    [Fact]
    public async Task OrdinaryTransportStillRefusesMissingCredentials()
    {
        var sender = new GmailIdentityMessageSender(Options.Create(new IdentityOnboardingOptions()));
        Assert.False(await sender.SendReporterOtpAsync("synthetic@example.test", "synthetic-placeholder"));
    }

    private static IConfiguration Configuration(string host, string keys) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sandbox:Enabled"] = "true",
            ["Sandbox:MailHost"] = host,
            ["Sandbox:KeyPath"] = keys
        }).Build();
}
