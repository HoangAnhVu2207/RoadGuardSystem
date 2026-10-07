using System.Security.Cryptography;
using System.Text.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.LinearReferencing;
using NetTopologySuite.Operation.Union;
using RoadGuardSystem.DTOs.Projects;
namespace RoadGuardSystem.Services.Projects;

public sealed class GeometryValidationException(string code, string detail) : Exception(detail) { public string Code { get; } = code; }
public static class GeometryEngine
{
    public static LineString Line(GeometryPoint[] points, int srid)
    {
        if (points.Length < 2 || points.Length > 100000 || points.Any(p => p is null || !double.IsFinite(p.X) || !double.IsFinite(p.Y))) Fail("geometry_validation_failed", "A finite line with at least two points is required.");
        var line = new GeometryFactory(new PrecisionModel(), srid).CreateLineString(points.Select(p => new Coordinate(p.X, p.Y)).ToArray());
        if (!line.IsSimple || !line.IsValid || line.Length <= 0 || line.Coordinates.Zip(line.Coordinates.Skip(1)).Any(p => p.First.Equals2D(p.Second))) Fail("geometry_validation_failed", "Line must be simple and have positive length without duplicate adjacent vertices.");
        return line;
    }
    public static GeometryPreview Preview(GeometryDraftInput input, int engineeringSrid)
    {
        if (input.SourceKind == "NATIVE_ALIGNMENT") return NativePreview(input);
        if (engineeringSrid is not (32648 or 32649) || input.SourceCrs != engineeringSrid) Fail("unsupported_crs", "A verified CRS transform adapter is required for this source CRS.");
        if (input.SourceKind != "COORDINATES" || input.Coordinates is null || input.SourceFileId is not null || input.TrackIndex is not null || input.TrackSegmentIndex is not null) Fail("geometry_validation_failed", "Coordinates source requires coordinates and no GPX reference.");
        var line = Line(input.Coordinates!, engineeringSrid);
        if (!double.IsFinite(input.StationOriginMeters) || !double.IsFinite(input.StationOriginMeters + line.Length) || string.IsNullOrWhiteSpace(input.ChangeReason) || !double.IsFinite(input.SurveyWidthMeters) || input.SurveyWidthMeters <= 0 || input.WidthProfile is null || input.WidthProfile.Length == 0 || input.WidthProfile.Any(w => w is null)) Fail("geometry_validation_failed", "Station, change reason and a complete width profile are required.");
        double end = 0;
        var pieces = new List<Geometry>(); var indexed = new LengthIndexedLine(line);
        foreach (var width in input.WidthProfile!)
        {
            if (!double.IsFinite(width.FromOffsetMeters) || !double.IsFinite(width.ToOffsetMeters) || !double.IsFinite(width.WidthMeters) || width.WidthMeters <= 0 || width.WidthMeters > input.SurveyWidthMeters || width.FromOffsetMeters != end || width.ToOffsetMeters <= end || width.ToOffsetMeters > line.Length) Fail("geometry_validation_failed", "Width profile must cover the line without gaps or overlaps, inside the survey width.");
            pieces.Add(indexed.ExtractLine(width.FromOffsetMeters, width.ToOffsetMeters).Buffer(width.WidthMeters / 2)); end = width.ToOffsetMeters;
        }
        if (Math.Abs(end - line.Length) > 1e-7) Fail("geometry_validation_failed", "Width profile must end at the line length.");
        return new(input.SourceCrs, engineeringSrid, line.Length, Shape(line), null, Shape(UnaryUnionOp.Union(pieces)), Shape(line.Buffer(input.SurveyWidthMeters / 2)), ["WGS84_TRANSFORM_NOT_CONFIGURED"]);
    }
    public static SegmentPreview Segments(Guid routeId, LineString line, double origin, SegmentDefinition definition)
    {
        if (definition.NativeAlignment is not null) return NativeSegments(routeId, origin, definition);
        if (!double.IsFinite(definition.TargetLengthMeters) || definition.TargetLengthMeters <= 0 || definition.RemainderMode is not ("KEEP" or "MERGE_PREVIOUS")) Fail("segment_validation_failed", "Target length and remainder mode are invalid.");
        var bounds = definition.BoundariesMeters?.ToList() ?? [0];
        if (definition.BoundariesMeters is null)
        {
            if (line.Length / definition.TargetLengthMeters > 100000) Fail("segment_validation_failed", "Too many segments.");
            for (var offset = definition.TargetLengthMeters; offset < line.Length; offset += definition.TargetLengthMeters) bounds.Add(offset);
            if (definition.RemainderMode == "MERGE_PREVIOUS" && bounds.Count > 1 && line.Length % definition.TargetLengthMeters != 0) bounds.RemoveAt(bounds.Count - 1);
            bounds.Add(line.Length);
        }
        if (bounds.Count < 2 || bounds.Count > 100001 || bounds[0] != 0 || Math.Abs(bounds[^1] - line.Length) > 1e-7 || bounds.Any(x => !double.IsFinite(x)) || bounds.Zip(bounds.Skip(1)).Any(x => x.Second <= x.First)) Fail("segment_validation_failed", "Boundaries must increase strictly from zero to line length.");
        var indexed = new LengthIndexedLine(line);
        var segments = bounds.Zip(bounds.Skip(1)).Select((p, index) => new SegmentGeometry(null, index + 1, p.First, p.Second, origin + p.First, origin + p.Second, p.Second - p.First, Shape(indexed.ExtractLine(p.First, p.Second)), null)).ToArray();
        return new(routeId, line.Length, bounds.ToArray(), segments, Hash(new { routeId, definition, boundaries = bounds }));
    }
    private static GeometryPreview NativePreview(GeometryDraftInput input)
    {
        var readiness = CrsProfileEngine.Readiness(input, input.ResolvedCrsProfile);
        if (!readiness.Ready) Fail("geometry_metadata_incomplete", "Native draft is not ready: " + string.Join(',', readiness.MissingFields.Concat(readiness.Errors)));
        if (input.Coordinates is not null || input.SourceFileId is not null || input.TrackIndex is not null || input.TrackSegmentIndex is not null) Fail("native_alignment_invalid", "Native analytic input cannot mix legacy coordinates/GPX references.");
        var alignment = NativeAlignment.Create(input.NativeAlignment!);
        var line = alignment.Extract(0, alignment.Length, input.TessellationToleranceMeters!.Value);
        if (!line.IsSimple || !line.IsValid) Fail("native_alignment_invalid", "Alignment rendering must be simple.");
        var pieces = new List<Geometry>(); double end = 0;
        foreach (var width in input.WidthProfile)
        {
            if (width is null || !double.IsFinite(width.FromOffsetMeters) || !double.IsFinite(width.ToOffsetMeters) || !double.IsFinite(width.WidthMeters) || width.WidthMeters <= 0 || width.WidthMeters > input.SurveyWidthMeters || width.FromOffsetMeters != end || width.ToOffsetMeters <= end || width.ToOffsetMeters > alignment.Length) Fail("geometry_validation_failed", "Width profile must cover analytic length without gaps/overlaps.");
            if (alignment.ArcRanges.Any(a => a.FromOffsetMeters < width.ToOffsetMeters && a.ToOffsetMeters > width.FromOffsetMeters && a.Radius <= input.SurveyWidthMeters / 2)) Fail("geometry_validation_failed", "Offset reaches or crosses an arc centre.");
            pieces.Add(alignment.Extract(width.FromOffsetMeters, width.ToOffsetMeters, input.TessellationToleranceMeters.Value).Buffer(width.WidthMeters / 2)); end = width.ToOffsetMeters;
        }
        if (Math.Abs(end - alignment.Length) > 1e-9 * Math.Max(1, alignment.Length)) Fail("geometry_validation_failed", "Width profile must end at canonical analytic length.");
        GeometryShape? wgs = null;
        var transformed = new List<double[]>();
        foreach (var coordinate in line.Coordinates)
        {
            if (!CrsProfileEngine.TryTransformCanonical(new(coordinate.X, coordinate.Y), input.ResolvedCrsProfile!, out var point)) { transformed.Clear(); break; }
            transformed.Add([point!.X, point.Y]);
        }
        if (transformed.Count > 0) wgs = new("LineString", transformed.ToArray());
        return new(input.SourceCrs, line.SRID, alignment.Length, Shape(line), wgs, Shape(UnaryUnionOp.Union(pieces)), Shape(line.Buffer(input.SurveyWidthMeters / 2)), readiness.Warnings,
            input.NativeAlignment!.CrsProfileRevisionId, input.ResolvedCrsProfile!.SampleOnly);
    }
    private static SegmentPreview NativeSegments(Guid routeId, double origin, SegmentDefinition definition)
    {
        var alignment = NativeAlignment.Create(definition.NativeAlignment!);
        var tolerance = definition.TessellationToleranceMeters ?? 0;
        if (!double.IsFinite(tolerance) || tolerance <= 0) Fail("segment_validation_failed", "Native segments require a rendering tolerance.");
        if (!double.IsFinite(definition.TargetLengthMeters) || definition.TargetLengthMeters <= 0 || definition.RemainderMode is not ("KEEP" or "MERGE_PREVIOUS")) Fail("segment_validation_failed", "Target length/remainder mode is invalid.");
        var bounds = definition.BoundariesMeters?.ToList() ?? [0];
        if (definition.BoundariesMeters is null)
        {
            if (alignment.Length / definition.TargetLengthMeters > 100000) Fail("segment_validation_failed", "Too many segments.");
            for (var offset = definition.TargetLengthMeters; offset < alignment.Length; offset += definition.TargetLengthMeters) bounds.Add(offset);
            if (definition.RemainderMode == "MERGE_PREVIOUS" && bounds.Count > 1 && alignment.Length % definition.TargetLengthMeters != 0) bounds.RemoveAt(bounds.Count - 1);
            bounds.Add(alignment.Length);
        }
        if (bounds.Count is < 2 or > 100001 || bounds[0] != 0 || bounds[^1] != alignment.Length || bounds.Any(x => !double.IsFinite(x)) || bounds.Zip(bounds.Skip(1)).Any(p => p.Second <= p.First)) Fail("segment_validation_failed", "Boundaries must strictly cover analytic length.");
        var parts = bounds.Zip(bounds.Skip(1)).Select((p, index) => new SegmentGeometry(null, index + 1, p.First, p.Second,
            CrsProfileEngine.StationAt(p.First, origin, definition.ChainageCalibration, alignment.Length), CrsProfileEngine.StationAt(p.Second, origin, definition.ChainageCalibration, alignment.Length),
            p.Second - p.First, Shape(alignment.Extract(p.First, p.Second, tolerance)), null)).ToArray();
        return new(routeId, alignment.Length, bounds.ToArray(), parts, Hash(new { routeId, definition, boundaries = bounds }));
    }
    public static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
    public static GeometryShape Shape(Geometry geometry) => geometry switch
    {
        LineString l => new("LineString", l.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray()),
        Polygon p => new("Polygon", new[] { p.ExteriorRing.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray() }.Concat(Enumerable.Range(0, p.NumInteriorRings).Select(i => p.GetInteriorRingN(i).Coordinates.Select(c => new[] { c.X, c.Y }).ToArray())).ToArray()),
        MultiPolygon m => new("MultiPolygon", Enumerable.Range(0, m.NumGeometries).Select(i => Shape(m.GetGeometryN(i)).Coordinates).ToArray()),
        _ => throw new GeometryValidationException("geometry_validation_failed", "Unsupported geometry output.")
    };
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string code, string detail) => throw new GeometryValidationException(code, detail);
}
