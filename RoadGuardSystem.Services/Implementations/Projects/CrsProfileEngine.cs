using RoadGuardSystem.DTOs.Projects;
namespace RoadGuardSystem.Services.Projects;

public static class CrsProfileEngine
{
    public static void Validate(CrsProfileInput profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Code) || profile.Code.Length > 80 || profile.SourceSrid < 0 || string.IsNullOrWhiteSpace(profile.Datum) || string.IsNullOrWhiteSpace(profile.Projection) || profile.AxisOrder is not ("EN" or "NE") || !double.IsFinite(profile.MetresPerUnit) || profile.MetresPerUnit <= 0 || string.IsNullOrWhiteSpace(profile.SourceReference) || !Hash(profile.SourceChecksum)) Fail("Profile definition and provenance must be explicit.");
        if (profile.Operation is { } operation)
        {
            if (string.IsNullOrWhiteSpace(operation.Method) || string.IsNullOrWhiteSpace(operation.Convention) || operation.Direction != "NATIVE_TO_WGS84" || string.IsNullOrWhiteSpace(operation.SourceReference) || operation.Parameters is null || operation.Parameters.Any(x => !double.IsFinite(x))) Fail("Operation identity, direction, parameters and source must be explicit.");
            if (operation.Method == "SAMPLE_AFFINE_TO_WGS84" && (!profile.SampleOnly || operation.Parameters.Length != 6)) Fail("Sample affine operation cannot be used for official real data.");
            if(operation.PublishedAccuracyMeters is { } accuracy && (!double.IsFinite(accuracy) || accuracy<=0 || string.IsNullOrWhiteSpace(operation.AccuracySource))) Fail("Operation accuracy requires a source.");
        }
        if(profile.Ellipsoid is { } ellipsoid && (!double.IsFinite(ellipsoid.SemiMajorAxisMeters) || ellipsoid.SemiMajorAxisMeters<=0 || !double.IsFinite(ellipsoid.InverseFlattening) || ellipsoid.InverseFlattening<=1 || string.IsNullOrWhiteSpace(ellipsoid.SourceReference))) Fail("Ellipsoid parameters require a source.");
        if(profile.ProjectionDefinition is { } projection && (projection.Parameters is null || projection.Parameters.Count==0 || projection.Parameters.Any(x=>string.IsNullOrWhiteSpace(x.Key) || !double.IsFinite(x.Value)) || string.IsNullOrWhiteSpace(projection.SourceReference))) Fail("Projection parameters require a source.");
        if (profile.SourcedToleranceMeters is { } tolerance && (!double.IsFinite(tolerance) || tolerance <= 0 || string.IsNullOrWhiteSpace(profile.ToleranceSource))) Fail("Accuracy tolerance requires a source.");
        if (profile.IndependentControls?.Any(c => c is null || !Finite(c.Native) || !Finite(c.Wgs84) || string.IsNullOrWhiteSpace(c.IndependentSource)) == true) Fail("Controls require independent provenance and finite coordinates.");
    }
    public static GeometryPoint NormalizeSource(GeometryPoint point, CrsProfileInput profile)
    {
        Validate(profile); if (!Finite(point)) Fail("Source coordinate is not finite.");
        var normalized=profile.AxisOrder == "EN" ? new GeometryPoint(point.X * profile.MetresPerUnit, point.Y * profile.MetresPerUnit) : new GeometryPoint(point.Y * profile.MetresPerUnit, point.X * profile.MetresPerUnit);
        if(!Finite(normalized)) Fail("Source normalization overflowed.");
        return normalized;
    }
    // Executable synthetic adapter only. Other pinned operation identities require an actual verified adapter;
    // no projection/datum parameters or controls are inferred from a province, EPSG label or UTM number.
    public static bool TryTransformCanonical(GeometryPoint point, CrsProfileInput profile, out GeometryPoint? wgs84)
    {
        Validate(profile); wgs84 = null;
        if (!Finite(point) || !profile.SampleOnly || profile.Operation?.Method != "SAMPLE_AFFINE_TO_WGS84") return false;
        var a = profile.Operation.Parameters;
        var candidate = new GeometryPoint(a[0] * point.X + a[1] * point.Y + a[2], a[3] * point.X + a[4] * point.Y + a[5]);
        if (!Finite(candidate) || candidate.X is < -180 or > 180 || candidate.Y is < -90 or > 90) Fail("Configured sample operation is outside WGS84 coordinate bounds.");
        wgs84 = candidate; return true;
    }
    public static GeometryDraftReadiness Readiness(GeometryDraftInput input, CrsProfileInput? profile)
    {
        var missing = new List<string>(); var errors = new List<string>();
        if(!double.IsFinite(input.StationOriginMeters) || !double.IsFinite(input.SurveyWidthMeters)) errors.Add("native_numeric_value_invalid");
        if(input.WidthProfile?.Any(x=>x is null || !double.IsFinite(x.FromOffsetMeters) || !double.IsFinite(x.ToOffsetMeters) || !double.IsFinite(x.WidthMeters))==true) errors.Add("native_width_invalid");
        if (input.NativeAlignment is null) missing.Add("nativeAlignment");
        if (profile is null) missing.Add("crsProfileRevision");
        if (input.RouteSystemId is null || input.RouteSystemId == Guid.Empty) missing.Add("routeSystemId");
        if (input.RouteKind is not ("MAIN" or "BRANCH")) missing.Add("routeKind");
        if (input.WidthProfile is null || input.WidthProfile.Length == 0 || input.SurveyWidthMeters <= 0) missing.Add("widthProfile");
        if (input.TessellationToleranceMeters is null) missing.Add("tessellationToleranceMeters");
        if (string.IsNullOrWhiteSpace(input.ChangeReason)) missing.Add("changeReason");
        if (input.RouteKind == "BRANCH" && (input.ParentRouteVersionId is null || input.JunctionOffsetMeters is null)) missing.Add("branchJunction");
        if (input.RouteKind == "MAIN" && (input.ParentRouteVersionId is not null || input.JunctionOffsetMeters is not null)) errors.Add("main_route_cannot_have_parent");
        if (input.DeclaredLengthMeters is { } declared && (!double.IsFinite(declared) || declared <= 0)) errors.Add("declared_length_invalid");
        try
        {
            if (profile is not null) Validate(profile);
            if (input.NativeAlignment is not null)
            {
                var alignment = NativeAlignment.Create(input.NativeAlignment);
                if(!double.IsFinite(input.StationOriginMeters+alignment.Length)) Fail("Station range overflowed.");
                if(input.WidthProfile is {Length:>0})
                {
                    double end=0;
                    foreach(var width in input.WidthProfile)
                    {
                        if(width is null || !double.IsFinite(width.FromOffsetMeters) || !double.IsFinite(width.ToOffsetMeters) || !double.IsFinite(width.WidthMeters) || width.FromOffsetMeters!=end || width.ToOffsetMeters<=end || width.ToOffsetMeters>alignment.Length || width.WidthMeters<=0 || width.WidthMeters>input.SurveyWidthMeters) Fail("Width intervals must cover canonical length inside survey width.");
                        end=width.ToOffsetMeters;
                    }
                    if(Math.Abs(end-alignment.Length)>1e-9*Math.Max(1,alignment.Length)) Fail("Width intervals must cover canonical length.");
                }
                if (input.TessellationToleranceMeters is { } tolerance) alignment.Extract(0, alignment.Length, tolerance);
                ValidateCalibration(input.ChainageCalibration, alignment.Length);
            }
        }
        catch (GeometryValidationException exception) { errors.Add(exception.Code); }
        return new(missing.Count == 0 && errors.Count == 0, missing.ToArray(), errors.ToArray(), "CANDIDATE", profile?.SampleOnly ?? false,
            ["OFFICIAL_CRS_OPERATION_AND_INDEPENDENT_CONTROL_NOT_VERIFIED"]);
    }
    public static void ValidateCalibration(ChainageCalibrationInput? calibration, double length)
    {
        if (calibration is null) return;
        if (string.IsNullOrWhiteSpace(calibration.SourceReference) || !Hash(calibration.SourceChecksum) || string.IsNullOrWhiteSpace(calibration.SelectedReason) || calibration.Controls is null || calibration.Controls.Length < 2 || calibration.Controls.Any(c => c is null || !double.IsFinite(c.GeometricOffsetMeters) || !double.IsFinite(c.StationMeters)) || calibration.Controls[0].GeometricOffsetMeters != 0 || calibration.Controls[^1].GeometricOffsetMeters != length || calibration.Controls.Zip(calibration.Controls.Skip(1)).Any(p => p.Second.GeometricOffsetMeters <= p.First.GeometricOffsetMeters || p.Second.StationMeters <= p.First.StationMeters)) Fail("Chainage calibration must be sourced, PM-selected and monotonically cover the geometric alignment.");
    }
    public static double StationAt(double offset, double origin, ChainageCalibrationInput? calibration, double length)
    {
        ValidateCalibration(calibration, length);
        if (!double.IsFinite(offset) || offset < 0 || offset > length || !double.IsFinite(origin)) Fail("Chainage offset is invalid.");
        if (calibration is null)
        {
            var station=origin+offset;
            if(!double.IsFinite(station)) Fail("Station range overflowed.");
            return station;
        }
        var pair = calibration.Controls.Zip(calibration.Controls.Skip(1)).First(p => offset <= p.Second.GeometricOffsetMeters);
        var fraction=(offset-pair.First.GeometricOffsetMeters)/(pair.Second.GeometricOffsetMeters-pair.First.GeometricOffsetMeters);
        var result=(1-fraction)*pair.First.StationMeters+fraction*pair.Second.StationMeters;
        if(!double.IsFinite(result)) Fail("Calibrated station overflowed.");
        return result;
    }
    private static bool Hash(string value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool Finite(GeometryPoint? point) => point is not null && double.IsFinite(point.X) && double.IsFinite(point.Y);
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string detail) => throw new GeometryValidationException("crs_profile_invalid", detail);
}
