using FluentAssertions;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Reporting;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class Anh02ReportingDefinitionTests
{
    [Fact]
    public void DistinctSegmentsAndBandsKeepNumeratorAndZeroDenominatorExplicit()
    {
        var route = Guid.NewGuid(); var set = Guid.NewGuid(); var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var facts = new ReportingFacts([], [new(route, set, a), new(route, set, b), new(route, set, c)],
            [new(Guid.NewGuid(), route, set, a, "SURFACE", Guid.NewGuid(), Guid.NewGuid()),
             new(Guid.NewGuid(), route, set, a, "SURFACE", Guid.NewGuid(), Guid.NewGuid()),
             new(Guid.NewGuid(), route, set, b, "SURFACE", Guid.NewGuid(), Guid.NewGuid()),
             new(Guid.NewGuid(), route, set, c, "RIGHT_EDGE", Guid.NewGuid(), Guid.NewGuid())], [], [], [], [], [], []);
        var capture = ReportingDefinitions.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, new(), facts);
        var surface = capture.Summary.Metrics.Single(x => x.Code == "baselineCoverageByBand" && x.Dimensions.Band == "SURFACE");
        surface.Numerator.Should().Be(2); surface.Denominator.Should().Be(3); surface.Value.Should().Be(200m / 3m);
        var empty = ReportingDefinitions.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, new(), new([], [], [], [], [], [], [], [], []));
        empty.Summary.Metrics.Where(x => x.Code == "baselineCoverageByBand").Should().OnlyContain(x => x.Value == null && x.ReasonCodes.Contains("EMPTY_DENOMINATOR"));
    }

    [Fact]
    public void MissingConsumersAreNullAndLegacyTasksRemainSeparateFromActualStatuses()
    {
        var facts = new ReportingFacts([new(Guid.NewGuid(), "NEW_ASSIGNED", null, "v1", false), new(Guid.NewGuid(), "COMPLETED", Guid.NewGuid(), "v2", false),
            new(Guid.NewGuid(), "NEW_ASSIGNED", null, "v3", true)], [], [], [], [], [], [], [], []);
        var capture = ReportingDefinitions.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, new(), facts);
        capture.Summary.Metrics.Where(x => x.Code is "reportsReceived" or "casesByStatus" or "defectsByStatus" or "repairItemsByStatus" or "repairAcceptanceRate")
            .Should().OnlyContain(x => x.Value == null && x.Availability == "UNAVAILABLE");
        capture.Summary.Metrics.Single(x => x.Code == "legacyUnclassified").Value.Should().Be(1);
        capture.Summary.Metrics.Single(x => x.Code == "surveyTasksByStatus" && x.Dimensions.Status == "NEW_ASSIGNED").Value.Should().Be(1);
    }

    [Fact]
    public void BytesUseDistinctFilesAndInt64AndStockMetricsDoNotPretendToUsePeriod()
    {
        var file = new ReportingFileDto(Guid.NewGuid(), "v", new string('a', 64), 5L * 1024 * 1024 * 1024, "video/mp4");
        var facts = new ReportingFacts([], [], [], [file, file], [], [], [], [], []);
        var capture = ReportingDefinitions.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, new(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow), facts);
        var metric = capture.Summary.Metrics.Single(x => x.Code == "verifiedSourceBytes");
        metric.Value.Should().Be(file.SizeBytes); metric.PeriodApplicable.Should().BeFalse(); capture.Files.Should().HaveCount(1);
    }

    [Fact]
    public void CursorIsFilterBoundAndRejectsMalformedOrChangedFilters()
    {
        var hash = ReportingCursor.FilterHash(Guid.NewGuid(), new(), "timeline", "SurveyRequest", Guid.NewGuid());
        var cursor = ReportingCursor.Encode(hash, DateTimeOffset.UtcNow, Guid.NewGuid());
        ReportingCursor.TryDecode(cursor, hash, out _).Should().BeTrue();
        ReportingCursor.TryDecode(cursor, "changed", out _).Should().BeFalse();
        ReportingCursor.TryDecode("bad", hash, out _).Should().BeFalse();
    }

    [Fact]
    public void UnknownLegacySpatialAttributionCannotBecomeAvailableZero()
    {
        var capture = ReportingDefinitions.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, new(SegmentSetId: Guid.NewGuid()),
            new([], [], [], [], [], [], [], ["LEGACY_SPATIAL_SCOPE_UNAVAILABLE"], []));
        var legacy = capture.Summary.Metrics.Single(m => m.Code == "legacyUnclassified");
        legacy.Value.Should().BeNull(); legacy.Availability.Should().Be("UNAVAILABLE"); legacy.ReasonCodes.Should().Contain("LEGACY_SPATIAL_SCOPE_UNAVAILABLE");
    }
}
