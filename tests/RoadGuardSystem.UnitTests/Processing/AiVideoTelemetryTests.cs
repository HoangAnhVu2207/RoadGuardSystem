using FluentAssertions;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiVideoTelemetryTests
{
    private const string Cue = "1\n00:00:00,000 --> 00:00:01,000\nGPS(106.5, 10.5, 0)\n";
    [Theory]
    [InlineData("\n\n")]
    [InlineData("\n\n\n")]
    [InlineData("\n \n\t\n")]
    public void AcceptsMultipleBlankLinesBetweenExtractedCues(string separator)
    {
        var second = "2\n00:00:01,000 --> 00:00:02,000\nGPS(106.5, 10.5, 0)";
        AiVideoTelemetry.ParseSrt(Cue.TrimEnd() + separator + second, 2000, 0)
            .Should().HaveCount(2);
    }
    [Fact]
    public void DjiLongitudeComesFirstAndCoverageIsHalfOpen()
    {
        var cues = AiVideoTelemetry.ParseSrt(Cue, 2000, 500);
        cues.Should().ContainSingle().Which.Should().Be(new AiDroneInterval(500, 1500, 10.5m, 106.5m));
        AiVideoTelemetry.At(cues, 499).Should().BeNull();
        AiVideoTelemetry.At(cues, 500).Should().NotBeNull();
        AiVideoTelemetry.At(cues, 1500).Should().BeNull();
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void RejectsShiftOutsideVideo(long offset)
        => FluentActions.Invoking(() => AiVideoTelemetry.ParseSrt(Cue, 2000, offset)).Should().Throw<ArgumentException>();
    [Fact]
    public void RejectsOverlapsMalformedCoordinatesAndAmbiguousGps()
    {
        foreach (var srt in new[] { Cue + "\n" + Cue, Cue.Replace("10.5", "91"), Cue.Replace("GPS", "noGPS"), Cue.Replace("106.5, 10.5, 0", "NaN, 10.5, 0"), Cue + "GPS(1,2,0)" })
            FluentActions.Invoking(() => AiVideoTelemetry.ParseSrt(srt, 2000, 0)).Should().Throw<ArgumentException>();
    }
    [Fact]
    public void EstimatesRequireBoundedIntervalValidPinAndReason()
    {
        AiVideoTelemetry.Estimate(100, 500, 10, 106, "Operator estimate", 1000).Should().Be(new AiDroneInterval(100, 500, 10, 106));
        FluentActions.Invoking(() => AiVideoTelemetry.Estimate(0, 1001, 10, 106, "estimate", 1000)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AiVideoTelemetry.Estimate(0, 500, 91, 106, "estimate", 1000)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => AiVideoTelemetry.Estimate(0, 500, 10, 106, "", 1000)).Should().Throw<ArgumentException>();
    }
}
