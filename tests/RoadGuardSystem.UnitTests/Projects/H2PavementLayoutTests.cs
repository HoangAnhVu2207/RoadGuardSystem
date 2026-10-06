using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2PavementLayoutTests
{
    [Theory]
    [InlineData(4, 3)]
    [InlineData(6, 2)]
    public void ExactEqualStripsPreserveWidth(decimal stripWidth, int stripCount)
    {
        var plan = PavementLayoutEngine.Plan(10, new(stripWidth, 5, [new(0, 10, 12)]));
        Assert.Equal(2 * stripCount, plan.Cells.Length);
        Assert.All(plan.Cells, cell => Assert.Equal(stripWidth, cell.ToLateralMeters - cell.FromLateralMeters));
        Assert.Equal(12, plan.Cells.Where(c => c.LongitudinalRow == 1).Sum(c => c.ToLateralMeters - c.FromLateralMeters));
    }

    [Fact]
    public void UnevenFinalStripIsRejected()
        => Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(10, new(5, 5, [new(0, 10, 12)])));

    [Fact]
    public void DecimalWidthDoesNotUseFloatingTolerance()
    {
        var plan = PavementLayoutEngine.Plan(1, new(0.1m, 1, [new(0, 1, 0.3m)]));
        Assert.Equal(3, plan.Cells.Length);
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(1, new(0.1m, 1, [new(0, 1, 0.30001m)])));
    }

    [Fact]
    public void TerminalResidualIsExplicitAndIndependentOfOneHundredMeterSegments()
    {
        var plan = PavementLayoutEngine.Plan(103, new(4, 6, [new(0, 103, 12)]));
        var residual = plan.Cells.Where(c => c.IsTerminalResidual).ToArray();
        Assert.Equal(3, residual.Length);
        Assert.All(residual, c => { Assert.Equal(102, c.FromOffsetMeters); Assert.Equal(103, c.ToOffsetMeters); });
        Assert.DoesNotContain(plan.Cells, c => c.FromOffsetMeters == 100 || c.ToOffsetMeters == 100);
    }

    [Fact]
    public void WidthTransitionSplitsCellsWithoutResettingLongitudinalGrid()
    {
        var plan = PavementLayoutEngine.Plan(12, new(2, 5, [new(0, 7, 12), new(7, 12, 8)]));
        Assert.Equal(20, plan.Cells.Length);
        Assert.Contains(plan.Cells, c => c.FromOffsetMeters == 5 && c.ToOffsetMeters == 7 && c.IsWidthTransitionFragment);
        Assert.Contains(plan.Cells, c => c.FromOffsetMeters == 7 && c.ToOffsetMeters == 10 && c.LongitudinalRow == 2);
        Assert.Equal(4, plan.Cells.Count(c => c.IsTerminalResidual));
    }

    [Theory]
    [InlineData(0, 5, 10)]
    [InlineData(4, 0, 10)]
    [InlineData(4, 5, 0)]
    public void InvalidDimensionsAreRejected(decimal strip, decimal slab, decimal length)
        => Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(length, new(strip, slab, [new(0, length, 12)])));

    [Fact]
    public void GapOverlapAndMissingCoverageAreRejected()
    {
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(10, new(4, 5, [new(0, 4, 12), new(5, 10, 12)])));
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(10, new(4, 5, [new(0, 6, 12), new(5, 10, 12)])));
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(10, new(4, 5, [new(0, 9, 12)])));
    }

    [Fact]
    public void PreviewIsDeterministicAndBounded()
    {
        var input = new PavementLayoutInput(4, 5, [new(0, 10, 12)]);
        Assert.Equal(PavementLayoutEngine.Plan(10, input).Cells, PavementLayoutEngine.Plan(10, input).Cells);
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(1000000, new(1, 1, [new(0, 1000000, 12)])));
    }

    [Fact]
    public void ExtremeDecimalRatiosAreRejectedBeforeOverflowOrAllocation()
    {
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(decimal.MaxValue, new(1, 0.0000000000000000000000000001m, [new(0, decimal.MaxValue, 12)])));
        Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(1, new(0.0000000000000000000000000001m, 1, [new(0, 1, decimal.MaxValue)])));
    }
    [Fact]
    public void NullWidthIntervalFailsAsValidation()
        => Assert.Throws<ArgumentException>(() => PavementLayoutEngine.Plan(10, new(4, 5, [null!])));
}
