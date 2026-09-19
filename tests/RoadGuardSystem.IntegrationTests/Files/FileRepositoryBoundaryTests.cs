using FluentAssertions;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("TaskId", "P2-04")]
public sealed class FileRepositoryBoundaryTests
{
    [Fact]
    public void RepositoryBoundary_ExistsWithoutPublishingHttpContracts()
    {
        var assembly = typeof(RoadGuardDbContext).Assembly;

        assembly.GetType("RoadGuardSystem.Repositories.Files.IFileRepository").Should().NotBeNull();
        assembly.GetType("RoadGuardSystem.Repositories.Files.FileRepository").Should().NotBeNull();
        assembly.GetType("RoadGuardSystem.Repositories.Files.StoreFileRequest").Should().NotBeNull();
        assembly.GetType("RoadGuardSystem.Repositories.Storage.IFileContentStore")!
            .GetMethod("DeleteAsync").Should().BeNull("P2-04 must not publish a general hard-delete boundary");
        assembly.GetTypes().Where(type => type.Namespace?.Contains("Controllers", StringComparison.Ordinal) == true)
            .Should().BeEmpty();
    }
}
