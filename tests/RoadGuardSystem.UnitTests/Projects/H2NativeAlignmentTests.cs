using FluentAssertions;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2NativeAlignmentTests
{
    private static NativeAlignmentInput QuarterCircle() => new(Guid.NewGuid(),
        [new("ARC", new(10, 0), new(0, 10), new(0, 0), 10, 0, Math.PI / 2)]);

    [Fact] public void Canonical_arc_length_is_independent_of_rendering_chords()
    {
        var alignment = NativeAlignment.Create(QuarterCircle());
        alignment.Length.Should().BeApproximately(5 * Math.PI, 1e-12);
        var coarse = alignment.Extract(0, alignment.Length, 1);
        var fine = alignment.Extract(0, alignment.Length, .001);
        coarse.Length.Should().BeLessThan(alignment.Length);
        fine.Length.Should().BeGreaterThan(coarse.Length);
        alignment.Length.Should().BeApproximately(5 * Math.PI, 1e-12);
    }

    [Fact] public void Arc_position_tangent_and_ranges_are_analytic()
    {
        var alignment = NativeAlignment.Create(QuarterCircle());
        var p = alignment.PointAt(alignment.Length / 2);
        p.X.Should().BeApproximately(Math.Sqrt(50), 1e-10);
        p.Y.Should().BeApproximately(Math.Sqrt(50), 1e-10);
        var tangent = alignment.TangentAt(0);
        tangent.X.Should().BeApproximately(0, 1e-12); tangent.Y.Should().Be(1);
        alignment.ArcRanges.Should().ContainSingle().Which.Radius.Should().Be(10);
    }

    [Fact] public void Clockwise_arc_has_clockwise_unit_tangent()
    {
        var alignment = NativeAlignment.Create(new(Guid.NewGuid(), [new("ARC", new(10, 0), new(0, -10), new(0, 0), 10, 0, -Math.PI / 2)]));
        alignment.TangentAt(0).Y.Should().Be(-1);
        alignment.PointAt(alignment.Length).Y.Should().BeApproximately(-10, 1e-10);
    }

    [Fact] public void Mixed_line_arc_extract_pins_native_profile_and_exact_endpoints()
    {
        var profile = Guid.NewGuid();
        var alignment = NativeAlignment.Create(new(profile, [new("LINE", new(-10, 0), new(10, 0)), new("ARC", new(10, 0), new(0, 10), new(0, 0), 10, 0, Math.PI / 2)]));
        alignment.CrsProfileRevisionId.Should().Be(profile);
        var segment = alignment.Extract(10, 25, .01);
        segment.StartPoint.X.Should().BeApproximately(0, 1e-10);
        segment.EndPoint.X.Should().BeApproximately(10 * Math.Cos(.5), 1e-10);
        segment.EndPoint.Y.Should().BeApproximately(10 * Math.Sin(.5), 1e-10);
    }

    [Fact] public void Disconnected_primitives_are_rejected()
    {
        var action = () => NativeAlignment.Create(new(Guid.NewGuid(), [new("LINE", new(0, 0), new(10, 0)), new("LINE", new(11, 0), new(20, 0))]));
        action.Should().Throw<GeometryValidationException>();
    }

    [Fact] public void Arc_endpoints_must_match_analytic_definition()
    {
        var input = QuarterCircle();
        var action = () => NativeAlignment.Create(input with { Primitives = [input.Primitives[0] with { End = new(0, 11) }] });
        action.Should().Throw<GeometryValidationException>();
    }

    [Theory] [InlineData(-1)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void Invalid_offsets_are_rejected(double offset)
    {
        var action = () => NativeAlignment.Create(QuarterCircle()).PointAt(offset);
        action.Should().Throw<GeometryValidationException>();
    }

    [Fact] public void Missing_profile_cannot_be_native_but_legacy_adapter_is_explicit()
    {
        var action = () => NativeAlignment.Create(new(Guid.Empty, [new("LINE", new(0, 0), new(10, 0))]));
        action.Should().Throw<GeometryValidationException>();
        var legacy = NativeAlignment.FromLegacy(GeometryEngine.Line([new(0, 0), new(10, 0)], 32648));
        legacy.IsLegacy.Should().BeTrue(); legacy.CrsProfileRevisionId.Should().Be(Guid.Empty);
        legacy.Extract(0, 10, .1).SRID.Should().Be(32648);
    }

    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(double.NaN)]
    public void Rendering_requires_explicit_positive_tolerance(double tolerance)
    {
        var alignment = NativeAlignment.Create(QuarterCircle());
        var action = () => alignment.Extract(0, alignment.Length, tolerance);
        action.Should().Throw<GeometryValidationException>();
    }
}
