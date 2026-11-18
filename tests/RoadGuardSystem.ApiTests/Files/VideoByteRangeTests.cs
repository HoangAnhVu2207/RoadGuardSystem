using RoadGuardSystem.API.Files;
using Xunit;

namespace RoadGuardSystem.ApiTests.Files;

public sealed class VideoByteRangeTests
{
    [Theory]
    [InlineData("bytes=0-9", 0, 9)]
    [InlineData("bytes=8-", 8, 99)]
    [InlineData("bytes=-8", 92, 99)]
    [InlineData("bytes=-200", 0, 99)]
    [InlineData("bytes=50-999999999", 50, 99)]
    public void AcceptsSingleRangesWithoutOverflow(string header, long first, long last)
    {
        Assert.True(VideoByteRange.TryParse(header, 100, out var actualFirst, out var actualLast));
        Assert.Equal(first, actualFirst); Assert.Equal(last, actualLast);
    }
    [Theory]
    [InlineData("bytes=100-")]
    [InlineData("bytes=-0")]
    [InlineData("bytes=2-1")]
    [InlineData("bytes=0-1,3-4")]
    [InlineData("bytes=9223372036854775808-")]
    [InlineData("items=0-1")]
    [InlineData("bytes=a-")]
    public void RejectsInvalidAndUnsupportedMultipleRanges(string header)
        => Assert.False(VideoByteRange.TryParse(header, 100, out _, out _));
}
