using RoadGuardSystem.BusinessObjects.Processing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class P234ValidationRunContractTests
{
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

}
