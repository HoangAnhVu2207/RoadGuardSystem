using RoadGuardSystem.API.Extensions;
using Xunit;
namespace RoadGuardSystem.ApiTests.Sandbox;

public sealed class SandboxWorkflowScenarioTests
{
    [Fact]
    public void SeedSwitchRemovedBeforeConfigurationParsingAndDuplicateRejected()
    {
        Assert.Equal(new[] { "--environment", "Development" }, SandboxSeedCommand.NormalizeArguments(["--sandbox-seed", "--environment", "Development"]));
        Assert.Throws<InvalidOperationException>(() => SandboxSeedCommand.NormalizeArguments(["--sandbox-seed", "--sandbox-seed"]));
    }
}
