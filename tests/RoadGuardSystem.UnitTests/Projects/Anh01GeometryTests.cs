using FluentAssertions;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;
namespace RoadGuardSystem.UnitTests.Projects;

public sealed class Anh01GeometryTests
{
    private static GeometryDraftInput Input(double length = 250) => new("COORDINATES", 32648, 1000, "survey route",
        [new(500000, 1200000), new(500000 + length, 1200000)], null, null, null, [new(0, length, 7)], 9);
    [Theory]
    [InlineData("KEEP", 4, 200)]
    [InlineData("MERGE_PREVIOUS", 3, 100)]
    public void Remainder_preserves_total_length_and_station(string mode, int boundsCount, double lastStart)
    {
        var input = Input(); var line = GeometryEngine.Line(input.Coordinates!, 32648);
        var result = GeometryEngine.Segments(Guid.NewGuid(), line, 1000, new(100, mode));
        result.BoundariesMeters.Should().HaveCount(boundsCount);
        result.Segments[^1].FromOffsetMeters.Should().Be(lastStart);
        result.Segments.Sum(x => x.LengthMeters).Should().Be(250);
        result.Segments[^1].EndStationMeters.Should().Be(1250);
    }
    [Fact]
    public void Explicit_boundaries_are_authority()
    {
        var result = GeometryEngine.Segments(Guid.NewGuid(), GeometryEngine.Line(Input().Coordinates!, 32648), 1000, new(100, "KEEP", [0, 20, 250]));
        result.Segments[0].LengthMeters.Should().Be(20); result.Segments[1].LengthMeters.Should().Be(230);
    }
    [Fact]
    public void Width_profile_rejects_gap()
    {
        var input = Input() with { WidthProfile = [new(0, 100, 7), new(101, 250, 7)] };
        var action = () => GeometryEngine.Preview(input, 32648); action.Should().Throw<GeometryValidationException>().Which.Code.Should().Be("geometry_validation_failed");
    }
    [Fact]
    public void Source_crs_is_not_relabelled()
    {
        var action = () => GeometryEngine.Preview(Input() with { SourceCrs = 4326 }, 32648);
        action.Should().Throw<GeometryValidationException>().Which.Code.Should().Be("unsupported_crs");
    }
    [Fact]
    public void Buffers_use_total_width_halves()
    {
        var result = GeometryEngine.Preview(Input(200), 32648);
        result.LengthMeters.Should().Be(200); result.RoadSurface.Type.Should().Be("Polygon"); result.SurveyArea.Type.Should().Be("Polygon");
        var road = (double[][][])result.RoadSurface.Coordinates;
        road[0].Min(p => p[1]).Should().Be(1199996.5); road[0].Max(p => p[1]).Should().Be(1200003.5);
    }
    [Fact]
    public void Duplicate_boundaries_rejected()
    {
        var action = () => GeometryEngine.Segments(Guid.NewGuid(), GeometryEngine.Line(Input().Coordinates!, 32648), 0, new(100, "KEEP", [0, 100, 100, 250]));
        action.Should().Throw<GeometryValidationException>();
    }
}
