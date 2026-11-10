using NetTopologySuite.Geometries;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2PavementFootprintTests
{
    private static NativeAlignment Line() => NativeAlignment.Create(new(Guid.NewGuid(), [new("LINE", new(0, 0), new(10, 0))]));
    private static PavementGeometryPreview Plan() => PavementFootprintEngine.Planned(Line(), new(new(4, 5, [new(0, 10, 12)]), 0.01));

    [Fact]
    public void SuppliedPolylineRetainsEveryCellAndValidSharedBoundaries()
    {
        var fixture = RoadGuardSystem.TestFixtures.PavementOffsetJoinRegression.Coordinates;
        var factory = new GeometryFactory(new PrecisionModel(), 32648);
        var alignment = NativeAlignment.FromLegacy(factory.CreateLineString(
            fixture.Select(point => new Coordinate(point[0], point[1])).ToArray()));
        var input = new PavementPlanCreateInput(new(3.5m, 5, [new(0, RoadGuardSystem.TestFixtures.PavementOffsetJoinRegression.LengthMeters, 7)]), 0.1);
        var result = PavementFootprintEngine.Planned(alignment, input);
        Assert.Equal(282, result.Slabs.Length);
        Assert.Equal(282, result.Plan!.Cells.Length);
        var polygons = result.Slabs.Select(slab => factory.CreatePolygon(
            slab.Footprint.Select(point => new Coordinate(point.X, point.Y)).ToArray())).ToArray();
        for (var index = 0; index < result.Slabs.Length; index++)
        {
            var slab = result.Slabs[index];
            var cell = result.Plan.Cells[index];
            Assert.Equal(cell.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), slab.Key);
            Assert.Equal(cell.Sequence, slab.PlannedSequence);
            Assert.Equal((double)(cell.ToOffsetMeters - cell.FromOffsetMeters), slab.LengthMeters);
            Assert.Equal(3.5, slab.WidthMeters);
            Assert.True(polygons[index].IsValid);
            Assert.True(polygons[index].Area > 0);
            if (cell.Strip == 1) AssertSharedEdge(polygons[index], polygons[index + 1]);
            if (index >= 2) AssertSharedEdge(polygons[index], polygons[index - 2]);
        }
    }

    [Fact]
    public void CornerExactlyAtSlabBoundaryUsesOneCommonCrossSection()
    {
        var alignment = NativeAlignment.Create(new(Guid.NewGuid(), [
            new("LINE", new(0, 0), new(10, 0)),
            new("LINE", new(10, 0), new(10, 10))]));
        var preview = PavementFootprintEngine.Planned(alignment, new(new(2, 10, [new(0, 20, 4)]), 0.1));
        Assert.Equal(4, preview.Slabs.Length);
        Assert.Contains(new GeometryPoint(12, -2), preview.Slabs[0].Footprint);
        Assert.Contains(new GeometryPoint(12, -2), preview.Slabs[2].Footprint);
        var factory = new GeometryFactory();
        var polygons = preview.Slabs.Select(slab => factory.CreatePolygon(
            slab.Footprint.Select(point => new Coordinate(point.X, point.Y)).ToArray())).ToArray();
        AssertSharedEdge(polygons[0], polygons[2]);
        AssertSharedEdge(polygons[1], polygons[3]);
    }

    [Fact]
    public void TangentLineAndArcHaveACommonCutAndRetainAnalyticLength()
    {
        var length = 10 + 10 * Math.PI / 2;
        var alignment = NativeAlignment.Create(new(Guid.NewGuid(), [
            new("LINE", new(0, 0), new(10, 0)),
            new("ARC", new(10, 0), new(20, 10), new(10, 10), 10, -Math.PI / 2, Math.PI / 2)]));
        var preview = PavementFootprintEngine.Planned(alignment, new(new(2, 10, [new(0, (decimal)length, 4)]), 0.1));
        Assert.Equal(length, (double)preview.Plan!.AnalyticLengthMeters, 10);
        var factory = new GeometryFactory();
        var polygons = preview.Slabs.Select(slab => factory.CreatePolygon(
            slab.Footprint.Select(point => new Coordinate(point.X, point.Y)).ToArray())).ToArray();
        AssertSharedEdge(polygons[0], polygons[2]);
        AssertSharedEdge(polygons[1], polygons[3]);
    }

    private static void AssertSharedEdge(Polygon first, Polygon second)
    {
        var shared = first.Intersection(second);
        Assert.Equal(0, shared.Area, 8);
        Assert.True(shared.Length > 0);
    }

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
