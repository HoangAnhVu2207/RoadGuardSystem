using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2PavementFootprintTests
{
    private static NativeAlignment Line() => NativeAlignment.Create(new(Guid.NewGuid(), [new("LINE", new(0, 0), new(10, 0))]));
    private static PavementGeometryPreview Plan() => PavementFootprintEngine.Planned(Line(), new(new(4, 5, [new(0, 10, 12)]), 0.01));

    [Fact]
    public void StraightSlabsHaveClosedNativeFootprintsAndAnalyticDimensions()
    {
        var plan = Plan();
        Assert.Equal(6, plan.Slabs.Length);
        Assert.All(plan.Slabs, slab => { Assert.Equal(slab.Footprint[0], slab.Footprint[^1]); Assert.Equal(5, slab.LengthMeters); Assert.Equal(4, slab.WidthMeters); });
        Assert.Equal(new GeometryPoint(0, -6), plan.Slabs[0].Footprint[0]);
    }

    [Fact]
    public void CurveLengthIsAnalyticAndFootprintRendersBothOffsets()
    {
        var length = 10 * Math.PI / 2;
        var alignment = NativeAlignment.Create(new(Guid.NewGuid(), [new("ARC", new(10, 0), new(0, 10), new(0, 0), 10, 0, Math.PI / 2)]));
        var input = new PavementPlanCreateInput(new(2, (decimal)length, [new(0, (decimal)length, 4)]), 0.01);
        var plan = PavementFootprintEngine.Planned(alignment, input);
        Assert.Equal(2, plan.Slabs.Length);
        Assert.All(plan.Slabs, slab => { Assert.True(slab.Footprint.Length > 10); Assert.Equal(length, slab.LengthMeters!.Value, 10); });
    }

    [Fact]
    public void InvalidInnerCurveRadiusIsRejected()
    {
        var alignment = NativeAlignment.Create(new(Guid.NewGuid(), [new("ARC", new(5, 0), new(0, 5), new(0, 0), 5, 0, Math.PI / 2)]));
        var length = (decimal)alignment.Length;
        Assert.Throws<ArgumentException>(() => PavementFootprintEngine.Planned(alignment, new(new(4, length, [new(0, length, 12)]), 0.01)));
    }

    [Fact]
    public void UnknownAsBuiltDimensionsRemainNullAndGapsAreExplicit()
    {
        var planned = Plan();
        var first = planned.Slabs[0];
        var built = PavementFootprintEngine.AsBuilt(planned, new(Guid.NewGuid(), [new("captured-1", 1, first.Footprint, null, null, "CUSTOM_TRANSITION", "PM_SURVEY")], "One captured slab"));
        Assert.Null(built.Slabs[0].LengthMeters); Assert.Null(built.Slabs[0].WidthMeters);
        Assert.True(built.RenderedGapAreaSquareMeters > 0);
        Assert.Contains("AS_BUILT_GAPS", built.Warnings);
    }

    [Fact]
    public void OverlapsAreReportedWithoutSilentlyTrimmingEvidence()
    {
        var planned = Plan(); var first = planned.Slabs[0];
        var built = PavementFootprintEngine.AsBuilt(planned, new(Guid.NewGuid(), [
            new("one", 1, first.Footprint, 5, 4, null, "PM_SURVEY"),
            new("two", 1, first.Footprint, 5, 4, null, "PM_SURVEY")], "Measured overlap"));
        Assert.Equal(2, built.Slabs.Length); Assert.True(built.RenderedOverlapAreaSquareMeters > 0);
        Assert.Contains("AS_BUILT_OVERLAPS", built.Warnings);
    }

    [Fact]
    public void MalformedCustomFootprintAndUnknownWithoutReasonAreRejected()
    {
        var planned = Plan(); var first = planned.Slabs[0];
        Assert.Throws<ArgumentException>(() => PavementFootprintEngine.AsBuilt(planned, new(Guid.NewGuid(), [new("bad", 1, [new(0, 0), new(1, 0)], null, null, "UNKNOWN", "PM_SURVEY")], "bad")));
        Assert.Throws<ArgumentException>(() => PavementFootprintEngine.AsBuilt(planned, new(Guid.NewGuid(), [new("bad", 1, first.Footprint, null, null, null, "PM_SURVEY")], "bad")));
        Assert.Throws<ArgumentException>(() => PavementFootprintEngine.AsBuilt(planned, new(Guid.NewGuid(), [new("bad", 99, first.Footprint, 5, 4, null, "PM_SURVEY")], "bad")));
    }
    [Fact]
    public void NullCustomSlabFailsAsValidation()
        => Assert.Throws<ArgumentException>(() => PavementFootprintEngine.AsBuilt(Plan(), new(Guid.NewGuid(), [null!], "Malformed slab")));
    [Fact]
    public void NativeRoundTripEndpointNormalizesNumericConversionButNotRealCoverageGap()
    {
        var alignment = NativeAlignment.Create(new(Guid.NewGuid(), [new("ARC", new(10, 0), new(0, 10), new(0, 0), 10, 0, Math.PI / 2)]));
        var serializedLength = decimal.Parse(alignment.Length.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
        var result = PavementFootprintEngine.Planned(alignment, new(new(2, 5, [new(0, serializedLength, 4)]), 0.01));
        Assert.Equal((decimal)alignment.Length, result.Plan!.AnalyticLengthMeters);
        Assert.Throws<ArgumentException>(() => PavementFootprintEngine.Planned(alignment, new(new(2, 5, [new(0, serializedLength - 0.001m, 4)]), 0.01)));
    }
}
