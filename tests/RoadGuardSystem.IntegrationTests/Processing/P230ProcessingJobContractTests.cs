using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
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

public sealed class P230ProcessingJobSqlServerTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P230ProcessingJobSqlServerTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProcessingJobs_BlockIndex_AllowsModelVariants()
    {
        await using var context = _fixture.CreateDbContext();
        var unique = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(is_unique AS int) AS [Value] FROM sys.indexes WHERE object_id = OBJECT_ID('ProcessingJobs') AND name = 'IX_ProcessingJobs_Block'")
            .SingleAsync();

        unique.Should().Be(0);
    }
}
