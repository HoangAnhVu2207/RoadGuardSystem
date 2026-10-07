using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Services.Reporting;
using Xunit;
namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class LD08StatisticsConsumerTests
{
    [Fact]
    public void StaleObligationQuantityCannotBecomeCompleteBecauseAnotherScopeOfTheSameDefectIsValid()
    {
        var project = Guid.NewGuid(); var defect = Guid.NewGuid();
        var capture = new ReportingCaptureDto(new("test", project, DateTimeOffset.UtcNow, "current", new(), [], [], "SERIALIZABLE"), [], [], [], new("AVAILABLE", []));
        var statistics = new DefectStatisticsSnapshot(project, [new(defect, "version")], 1, 1, 1, 0, [],
            [new(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), "Area", "m\u00b2", 12, "KNOWN", Guid.NewGuid(), "TEST_ONLY", null)],
            ["STATISTICS_SOURCE_STALE"], [], "version");
        var metric = Assert.Single(DefectStatisticsCaptureConsumer.Apply(capture, statistics).Summary.Metrics.Where(row => row.Code == "sourcedAllocatedQuantity"));
        Assert.Null(metric.Value); Assert.Equal("PARTIAL", metric.Availability); Assert.Contains("STATISTICS_SOURCE_STALE", metric.ReasonCodes);
    }
}
