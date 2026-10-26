using RoadGuardSystem.Services.Processing.Anh02;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class Task34SyntheticMultiframeTests
{
    [Fact]
    public void Multiclass_frames_are_deterministic_bounded_and_distinct()
    {
        Assert.True(SyntheticAiFixture.Supported("synthetic-multiclass-v1"));
        var frames = SyntheticAiFixture.Samples("synthetic-multiclass-v1");
        Assert.Equal(2, frames.Count);
        Assert.Equal(new[] { "CRACK", "POTHOLE" }, frames.Select(x => x.TypeCode));
        Assert.Equal(new long[] { 500, 1500 }, frames.Select(x => x.TimestampMilliseconds));
        Assert.All(frames, f =>
        {
            Assert.InRange(f.TimestampMilliseconds, 0, SyntheticAiFixture.DurationMilliseconds - 1);
            Assert.InRange(f.Confidence, 0m, 1m);
            Assert.Equal(4, f.Box.Length);
            Assert.True(f.Box[0] + f.Box[2] <= 1m && f.Box[1] + f.Box[3] <= 1m);
        });
        var run = Guid.NewGuid(); var source = Guid.NewGuid();
        Assert.NotEqual(SyntheticAiFixture.Identity(run, $"frame:{source:D}:1"), SyntheticAiFixture.Identity(run, $"frame:{source:D}:2"));
    }

    [Fact]
    public void Legacy_fixture_shape_and_unsupported_versions_are_preserved()
    {
        Assert.Empty(SyntheticAiFixture.Samples("synthetic-empty-v1"));
        var legacy = Assert.Single(SyntheticAiFixture.Samples("synthetic-road-v1"));
        Assert.Equal("CRACK", legacy.TypeCode);
        Assert.Equal(500, legacy.TimestampMilliseconds);
        Assert.False(SyntheticAiFixture.Supported("production-yolo"));
        Assert.Throws<ArgumentException>(() => SyntheticAiFixture.Samples("production-yolo"));
    }
}
