using RoadGuardSystem.API.Extensions;
using Xunit;
namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class HandoffBootstrapTests
{
    [Theory]
    [InlineData(0, true)] // Observed SQL restart pre-login transport failure.
    [InlineData(10054, true)]
    [InlineData(17142, true)]
    [InlineData(18401, true)] // SQL upgrading/startup, not invalid credentials.
    [InlineData(18456, false)]
    [InlineData(4060, false)]
    [InlineData(229, false)]
    [InlineData(262, false)]
    public void StartupRetryNeverIncludesAuthenticationOrPermissionFailures(int number, bool expected)
        => Assert.Equal(expected, HandoffBootstrap.IsStartupConnectionTransient(number));

    [Fact]
    public void HttpsOriginsOverridePersistedHttpOriginsWithoutChangingState()
    {
        var persisted = new[] { "http://localhost:5173" };
        Assert.Equal(new[] { "https://localhost:5173" },
            HandoffBootstrap.ResolveOrigins("https://localhost:5173", persisted));
        Assert.Equal(new[] { "http://localhost:5173" }, persisted);
        Assert.Equal(persisted, HandoffBootstrap.ResolveOrigins(null, persisted));
        Assert.Throws<InvalidOperationException>(() => HandoffBootstrap.ResolveOrigins("*", persisted));
    }
    [Theory]
    [InlineData("*")]
    [InlineData("https://untrusted.invalid")]
    [InlineData("http://localhost:5173/path")]
    [InlineData("http://localhost:5173/")]
    public void NonlocalOrMalformedCorsOriginsRejected(string origin)
        => Assert.Throws<InvalidOperationException>(() => HandoffBootstrap.ValidateOrigins(new[] { origin }));

    [Fact]
    public void ExplicitLoopbackOriginsAccepted()
        => Assert.Equal(new[] { "http://localhost:5173", "http://127.0.0.1:8081" },
            HandoffBootstrap.ValidateOrigins(new[] { "http://localhost:5173", "http://127.0.0.1:8081" }));

    [Theory]
    [InlineData("Production", "sql", "RoadGuard_FE_0123456789abcdef0123456789abcdef", "0123456789abcdef0123456789abcdef")]
    [InlineData("Development", ".\\HANHNAV", "RoadGuard_FE_0123456789abcdef0123456789abcdef", "0123456789abcdef0123456789abcdef")]
    [InlineData("Development", "sql", "RoadGuardSystem", "0123456789abcdef0123456789abcdef")]
    [InlineData("Development", "sql", "RoadGuard_FE_0123456789abcdef0123456789abcdef", "bad")]
    public void UnsafeTargetsRejected(string environment, string server, string database, string owner)
        => Assert.Throws<InvalidOperationException>(() => HandoffBootstrap.ValidateTarget(environment, server, database, owner));
    [Fact]
    public void DemoModelIsExplicitlySyntheticAndDeterministic()
    {
        var model = HandoffBootstrap.CreateDemoModel();
        Assert.Equal(Guid.Parse("72f84201-d033-4000-8000-000000000001"), model.Id);
        Assert.Equal("MOCK_AI_DEVELOPMENT_DATA", model.ModelName);
        Assert.Equal("fixture://local/synthetic-road-v1", model.ArtifactUri);
        Assert.Equal(model.ReleasedAt, HandoffBootstrap.CreateDemoModel().ReleasedAt);
    }
    [Fact]
    public void ExactOwnedDevelopmentTargetAccepted()
        => HandoffBootstrap.ValidateTarget("Development", "sql", "RoadGuard_FE_0123456789abcdef0123456789abcdef", "0123456789abcdef0123456789abcdef");
}
