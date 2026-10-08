using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

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
