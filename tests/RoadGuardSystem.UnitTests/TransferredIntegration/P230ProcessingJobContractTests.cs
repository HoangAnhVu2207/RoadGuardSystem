using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class P230ProcessingJobContractTests
{
    [Fact]
    public void CreateQueued_ValidImmutableManifest_ProducesQueuedJob()
    {
        var job = ProcessingJob.CreateQueued(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('a', 64),
            "{}",
            "MOCK");

        Assert.Equal(ProcessingJobStatus.Queued, job.Status);
        Assert.Equal(new string('a', 64), job.ManifestHash);
        Assert.Equal("MOCK", job.Mode);
    }

    [Theory]
    [InlineData("REAL ")]
    [InlineData("invalid")]
    public void CreateQueued_InvalidMode_Rejects(string mode)
    {
        Assert.Throws<ArgumentException>(() => ProcessingJob.CreateQueued(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), "{}", mode));
    }
}
