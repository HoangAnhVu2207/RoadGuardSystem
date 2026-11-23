using FluentAssertions;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiWorkerContractTests
{
    [Fact]
    public void PixelBoxBecomesNormalizedXywh()
        => AiWorkerCoordinates.NormalizeBox([100, 50, 300, 150], 1000, 500)
            .Should().Equal(0.1m, 0.1m, 0.2m, 0.2m);

    [Theory]
    [InlineData(-1, 0, 10, 10)]
    [InlineData(10, 0, 10, 10)]
    [InlineData(20, 0, 10, 10)]
    [InlineData(0, 0, 1001, 10)]
    [InlineData(0, 0, 10, 501)]
    public void InvalidBoxesAreRejected(double x1, double y1, double x2, double y2)
    {
        Action act = () => AiWorkerCoordinates.NormalizeBox([x1, y1, x2, y2], 1000, 500);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NonFiniteAndInvalidDimensionsAreRejected()
    {
        Action nan = () => AiWorkerCoordinates.NormalizeBox([0, 0, double.NaN, 10], 100, 100);
        Action infinity = () => AiWorkerCoordinates.NormalizeBox([0, 0, 10, double.PositiveInfinity], 100, 100);
        Action dimensions = () => AiWorkerCoordinates.NormalizeBox([0, 0, 10, 10], 0, 100);
        nan.Should().Throw<ArgumentException>();
        infinity.Should().Throw<ArgumentException>();
        dimensions.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("0.0005", 1)]
    [InlineData("1.2345", 1235)]
    [InlineData("59", 59000)]
    public void TimestampUsesDecimalHalfUp(string seconds, int expected)
        => AiWorkerCoordinates.TimestampMilliseconds(decimal.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture))
            .Should().Be(expected);

    [Fact]
    public void InvalidTimestampDoesNotOverflowOrBecomeZero()
    {
        Action negative = () => AiWorkerCoordinates.TimestampMilliseconds(-0.1m);
        Action overflow = () => AiWorkerCoordinates.TimestampMilliseconds(decimal.MaxValue);
        negative.Should().Throw<ArgumentOutOfRangeException>();
        overflow.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BoxTooSmallForDecimalContractIsRejected()
    {
        Action act = () => AiWorkerCoordinates.NormalizeBox([0, 0, 1e-30, 1e-30], 1000, 500);
        act.Should().Throw<ArgumentException>();
    }
}
