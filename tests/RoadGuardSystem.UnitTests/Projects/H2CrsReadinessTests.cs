using FluentAssertions;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;
namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2CrsReadinessTests
{
    private static CrsProfileInput Sample() => new("sample", 0, "CANDIDATE_SAMPLE_DATUM", "CANDIDATE_SAMPLE_PROJECTION", "EN", 1, "sample fixture", new string('a', 64));
    [Fact] public void Candidate_profile_does_not_invent_a_transform_or_accuracy()
    {
        CrsProfileEngine.Validate(Sample());
        CrsProfileEngine.TryTransformCanonical(new(0, 0), Sample(), out _).Should().BeFalse();
    }
    [Fact] public void Axis_and_units_are_explicitly_normalized()
    {
        CrsProfileEngine.NormalizeSource(new(2, 3), Sample() with { AxisOrder = "NE", MetresPerUnit = .01 }).Should().Be(new GeometryPoint(.03, .02));
    }
    [Fact] public void Configured_sample_affine_is_executable_but_not_an_official_datum_operation()
    {
        var profile = Sample() with { Operation = new("SAMPLE_AFFINE_TO_WGS84", "NONE", "NATIVE_TO_WGS84", [.001, 0, 105, 0, .001, 10], "sample fixture") };
        CrsProfileEngine.TryTransformCanonical(new(100, 200), profile, out var p).Should().BeTrue();
        p.Should().Be(new GeometryPoint(105.1, 10.2));
        var real = profile with { SampleOnly = false };
        var action = () => CrsProfileEngine.Validate(real);
        action.Should().Throw<GeometryValidationException>();
    }
    [Fact] public void Partial_draft_reports_missing_configuration_without_fabricating_geometry()
    {
        var input = new GeometryDraftInput("NATIVE_ALIGNMENT", 0, 0, "partial", null, null, null, null, [], 0);
        var readiness = CrsProfileEngine.Readiness(input, null);
        readiness.Ready.Should().BeFalse();
        readiness.MissingFields.Should().Contain(["nativeAlignment", "crsProfileRevision", "routeSystemId", "widthProfile", "tessellationToleranceMeters"]);
    }
    [Fact] public void Calibrated_chainage_is_distinct_from_geometric_length()
    {
        var calibration = new ChainageCalibrationInput("approved source", new string('b', 64), [new(0, 1000), new(100, 1120)], "PM selected");
        CrsProfileEngine.StationAt(25, 0, calibration, 100).Should().Be(1030);
        CrsProfileEngine.StationAt(25, 1000, null, 100).Should().Be(1025);
    }
    [Fact] public void Null_calibration_control_is_a_validation_error()
    {
        var calibration=new ChainageCalibrationInput("source",new string('b',64),[null!,new(100,100)],"selected");
        var action=()=>CrsProfileEngine.ValidateCalibration(calibration,100);
        action.Should().Throw<GeometryValidationException>();
    }
    [Fact] public void Source_unit_product_overflow_is_rejected()
    {
        var action=()=>CrsProfileEngine.NormalizeSource(new(double.MaxValue,1),Sample() with {MetresPerUnit=2});
        action.Should().Throw<GeometryValidationException>();
    }
    [Fact] public void Nonfinite_draft_values_never_report_ready()
    {
        var input=new GeometryDraftInput("NATIVE_ALIGNMENT",0,double.NaN,"reason",null,null,null,null,[new(0,100,double.NaN)],double.NaN);
        var readiness=CrsProfileEngine.Readiness(input,Sample());
        readiness.Ready.Should().BeFalse(); readiness.Errors.Should().Contain("native_numeric_value_invalid");
    }
    [Fact] public void Optional_sourced_geodetic_parameters_are_preserved_and_validated()
    {
        var profile=Sample() with {Ellipsoid=new(1,2,"synthetic test only"),ProjectionDefinition=new(new Dictionary<string,double>{{"fixture_parameter",1}},"synthetic test only")};
        CrsProfileEngine.Validate(profile);
        var invalid=()=>CrsProfileEngine.Validate(profile with {Ellipsoid=profile.Ellipsoid with {SemiMajorAxisMeters=double.NaN}});
        invalid.Should().Throw<GeometryValidationException>();
    }
    [Fact] public void Extreme_finite_calibration_interpolates_without_intermediate_overflow()
    {
        var calibration=new ChainageCalibrationInput("source",new string('a',64),[new(0,-double.MaxValue),new(100,double.MaxValue)],"selected");
        CrsProfileEngine.StationAt(50,0,calibration,100).Should().Be(0);
        var invalid=()=>CrsProfileEngine.StationAt(double.MaxValue,double.MaxValue,null,double.MaxValue);
        invalid.Should().Throw<GeometryValidationException>();
    }
}
