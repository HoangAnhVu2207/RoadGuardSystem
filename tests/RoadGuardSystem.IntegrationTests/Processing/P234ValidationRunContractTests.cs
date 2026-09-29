using RoadGuardSystem.BusinessObjects.Processing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class P234ValidationRunContractTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public P234ValidationRunContractTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public void CreateQueued_RejectsEmptyPairProvenance()
    {
        Assert.Throws<ArgumentException>(() => ValidationRun.CreateQueued(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "split-v1", "CRACK_WIDTH", "mm", "[]"));
    }

    [Fact]
    public void CreateQueued_RejectsNonArrayPairs()
    {
        Assert.Throws<ArgumentException>(() => ValidationRun.CreateQueued(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "split-v1", "CRACK_WIDTH", "mm", "{}"));
    }

    [Fact]
    public void Complete_RecordsMetricSummary()
    {
        var run = ValidationRun.CreateQueued(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "split-v1", "DEPRESSION_DEPTH", "mm", "[{\"groundTruthId\":\"11111111-1111-4111-8111-111111111111\",\"derivedMeasurementId\":\"22222222-2222-4222-8222-222222222222\"}]");

        run.Complete(2, 1, 0.25m, 0.75m, 0.9m, "[\"OUTLIER: sample-003\"]");

        Assert.Equal(ValidationRunStatus.Completed, run.Status);
        Assert.Equal(2, run.UsedCount);
        Assert.Equal(1, run.ExcludedCount);
        Assert.Equal(0.9m, run.Rmse);
    }

    [Fact]
    public void Complete_AllIncludedSamples_AllowsEmptyExclusionReasons()
    {
        var run = ValidationRun.CreateQueued(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "split-v1", "DEPRESSION_DEPTH", "mm", "[{\"groundTruthId\":\"11111111-1111-4111-8111-111111111111\",\"derivedMeasurementId\":\"22222222-2222-4222-8222-222222222222\"}]");

        run.Complete(1, 0, 0m, 2m, 2m, "[]");

        Assert.Equal(ValidationRunStatus.Completed, run.Status);
        Assert.Equal("[]", run.ExclusionReasonsJson);
    }

    [Fact]
    public async Task ProcessingFoundationMigration_CreatesValidationRunAndJobRowVersion()
    {
        await using var context = _fixture.CreateDbContext();
        var tableCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables WHERE [name] = 'ValidationRuns'").SingleAsync();
        var rowVersionCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('ProcessingJobs') AND [name] = 'RowVersion' AND system_type_id = 189").SingleAsync();

        tableCount.Should().Be(1);
        rowVersionCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidationProvenanceMigration_CreatesResearchTablesAndConcurrencyToken()
    {
        await using var context = _fixture.CreateDbContext();
        var tableCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables WHERE [name] IN ('DerivedMeasurements', 'MeasurementValidationSamples')")
            .SingleAsync();
        var rowVersionCount = await context.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID('ValidationRuns') AND [name] = 'RowVersion' AND system_type_id = 189")
            .SingleAsync();

        tableCount.Should().Be(2);
        rowVersionCount.Should().Be(1);
    }
}
