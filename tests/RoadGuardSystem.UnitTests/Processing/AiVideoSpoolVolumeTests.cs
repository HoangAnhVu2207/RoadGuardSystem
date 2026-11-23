using FluentAssertions;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiVideoSpoolVolumeTests
{
    [Fact]
    public void SpoolCapacityUsesDeepestContainingMountInsteadOfFilesystemRoot()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var mount = Path.Combine(root, "volumes", "ai");
        var nested = Path.Combine(mount, "spool-volume");
        AiVideoSpoolVolume.ResolvePath(Path.Combine(nested, "spool"), [root, mount, nested])
            .Should().Be(nested);
    }

    [Fact]
    public void SiblingMountWithSamePrefixDoesNotOwnSpool()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var mount = Path.Combine(root, "volumes", "ai");
        AiVideoSpoolVolume.ResolvePath(Path.Combine(root, "volumes", "ai-other", "spool"), [root, mount])
            .Should().Be(root);
    }

    [Fact]
    public void MountRootItselfMatchesWithTrailingSeparator()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var mount = Path.Combine(root, "volumes", "ai");
        AiVideoSpoolVolume.ResolvePath(mount + Path.DirectorySeparatorChar, [root, mount + Path.DirectorySeparatorChar])
            .Should().Be(mount);
    }

    [Fact]
    public void MissingContainingMountFailsClosed()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var action = () => AiVideoSpoolVolume.ResolvePath(Path.Combine(root, "spool"), [Path.Combine(root, "elsewhere")]);
        action.Should().Throw<IOException>();
    }
}
