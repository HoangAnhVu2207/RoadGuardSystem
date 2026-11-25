using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiVideoTelemetryRevisionTests
{
    private const string Intervals = "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10.5,\"longitude\":106.5}]";
    private static AiVideoTelemetryRevision Create(string json = Intervals, string kind = "OPERATOR_ESTIMATE", string? key = null)
        => AiVideoTelemetryRevision.Create(Guid.NewGuid(), Guid.NewGuid(), 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow,
            new string('a', 64), kind, json, 2000, key, kind == "EXTERNAL_SRT" ? new string('b', 64) : null,
            kind == "EXTERNAL_SRT" ? 100 : null, 0, "operator estimate", new string('c', 64), new string('d', 64));

    [Fact]
    public void EstimateRetainsExplicitProvenanceAndBoundedIntervals()
    {
        var value = Create();
        value.Kind.Should().Be("OPERATOR_ESTIMATE"); value.IntervalsJson.Should().Be(Intervals);
        value.OriginalSrtObjectKey.Should().BeNull(); value.PreviousId.Should().BeNull(); value.Number.Should().Be(1);
        typeof(AiVideoTelemetryRevision).GetProperties().Should().OnlyContain(p => p.SetMethod == null || !p.SetMethod.IsPublic);
    }

    [Theory]
    [InlineData("empty", "[]")]
    [InlineData("negative-start", "[{\"startMs\":-1,\"endMs\":1000,\"latitude\":10,\"longitude\":106}]")]
    [InlineData("past-end", "[{\"startMs\":0,\"endMs\":2001,\"latitude\":10,\"longitude\":106}]")]
    [InlineData("latitude", "[{\"startMs\":0,\"endMs\":1000,\"latitude\":91,\"longitude\":106}]")]
    [InlineData("longitude", "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10,\"longitude\":181}]")]
    [InlineData("overlap", "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10,\"longitude\":106},{\"startMs\":999,\"endMs\":1500,\"latitude\":10,\"longitude\":106}]")]
    public void RejectsInvalidGeometryAndTiming(string scenario, string json)
        => FluentActions.Invoking(() => Create(json)).Should().Throw<ArgumentException>(scenario);

    [Theory]
    [InlineData("../evidence.srt")]
    [InlineData("C:/evidence.srt")]
    [InlineData("/evidence.srt")]
    [InlineData("a\\evidence.srt")]
    public void RejectsUnscopedObjectKeys(string key) => FluentActions.Invoking(() => Create(kind: "EXTERNAL_SRT", key: key)).Should().Throw<ArgumentException>();

    [Fact]
    public void ExternalEvidencePreservesOriginalHashAndKey()
    {
        var value = Create(kind: "EXTERNAL_SRT", key: "private/telemetry/evidence.srt");
        value.OriginalSrtSha256.Should().Be(new string('b', 64)); value.OriginalSrtBytes.Should().Be(100);
    }
}
