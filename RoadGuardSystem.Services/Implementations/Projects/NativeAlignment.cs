using NetTopologySuite.Geometries;
using RoadGuardSystem.DTOs.Projects;

namespace RoadGuardSystem.Services.Projects;

// Native projected E/N metres. Sampling is a rendering/query approximation, never the canonical length.
public sealed class NativeAlignment
{
    private readonly NativeAlignmentPrimitive[] primitives;
    private readonly double[] ends;
    private readonly int srid;
    public Guid CrsProfileRevisionId { get; }
    public bool IsLegacy { get; }
    public double Length => ends[^1];
    public IReadOnlyList<NativeArcRange> ArcRanges { get; } = [];
    public IReadOnlyList<double> PrimitiveBoundaries => new[] { 0d }.Concat(ends).ToArray();

    private NativeAlignment(NativeAlignmentInput input, bool legacy)
    {
        if ((!legacy && input.CrsProfileRevisionId == Guid.Empty) || input.SpatialSrid < 0 || input.Primitives is null || input.Primitives.Length is < 1 or > 100000)
            Fail("Profile and a bounded primitive sequence are required.");
        CrsProfileRevisionId = input.CrsProfileRevisionId; IsLegacy = legacy; srid = input.SpatialSrid;
        primitives = input.Primitives.ToArray(); ends = new double[primitives.Length];
        var arcs = new List<NativeArcRange>(); double offset = 0;
        for (var index = 0; index < primitives.Length; index++)
        {
            var p = primitives[index];
            if (p is null || !Finite(p.Start) || !Finite(p.End)) Fail("Finite native coordinates are required.");
            if (index > 0 && !Same(primitives[index - 1].End, p.Start)) Fail("Primitive sequence has a gap.");
            double length;
            if (p.Kind == "LINE")
            {
                if (p.Center is not null || p.Radius is not null || p.StartAngleRadians is not null || p.SweepRadians is not null) Fail("LINE cannot contain ARC parameters.");
                length = Distance(p.Start, p.End);
            }
            else if (p.Kind == "ARC")
            {
                if (!Finite(p.Center) || p.Radius is not double r || !double.IsFinite(r) || r <= 0 || p.StartAngleRadians is not double start || !double.IsFinite(start) || p.SweepRadians is not double sweep || !double.IsFinite(sweep) || sweep == 0 || Math.Abs(sweep) >= 2 * Math.PI)
                    Fail("ARC requires a positive radius and a finite signed sweep smaller than a full circle.");
                if (!Same(p.Start, ArcPoint(p, 0)) || !Same(p.End, ArcPoint(p, 1))) Fail("ARC endpoints disagree with its analytic parameters.");
                length = p.Radius!.Value * Math.Abs(p.SweepRadians!.Value);
                arcs.Add(new(offset, offset + length, p.Radius.Value));
            }
            else { Fail("Only LINE and ARC primitives are supported."); return; }
            if (!double.IsFinite(length) || length <= 0 || !double.IsFinite(offset + length)) Fail("Primitive length must be finite and positive.");
            offset += length; ends[index] = offset;
        }
        ArcRanges = arcs.AsReadOnly();
    }

    public static NativeAlignment Create(NativeAlignmentInput input) => new(input, false);
    public static NativeAlignment FromLegacy(LineString geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        return new(new(Guid.Empty, geometry.Coordinates.Zip(geometry.Coordinates.Skip(1))
            .Select(pair => new NativeAlignmentPrimitive("LINE", new(pair.First.X, pair.First.Y), new(pair.Second.X, pair.Second.Y))).ToArray(), geometry.SRID), true);
    }
    public GeometryPoint PointAt(double offset)
    {
        var (p, fraction) = Locate(offset);
        return p.Kind == "ARC" ? ArcPoint(p, fraction) : new(p.Start.X + fraction * (p.End.X - p.Start.X), p.Start.Y + fraction * (p.End.Y - p.Start.Y));
    }
    public GeometryPoint TangentAt(double offset)
    {
        var (p, fraction) = Locate(offset);
        if (p.Kind == "LINE") { var d = Distance(p.Start, p.End); return new((p.End.X - p.Start.X) / d, (p.End.Y - p.Start.Y) / d); }
        var angle = p.StartAngleRadians!.Value + fraction * p.SweepRadians!.Value;
        var direction = Math.Sign(p.SweepRadians.Value);
        return new(-direction * Math.Sin(angle), direction * Math.Cos(angle));
    }
    public LineString Extract(double from, double to, double tessellationTolerance)
    {
        ValidateOffset(from); ValidateOffset(to);
        if (to <= from || !double.IsFinite(tessellationTolerance) || tessellationTolerance <= 0) Fail("Ordered offsets and an explicit positive rendering tolerance are required.");
        var coordinates = new List<Coordinate>();
        for (var index = 0; index < primitives.Length; index++)
        {
            var begin = index == 0 ? 0 : ends[index - 1]; var end = ends[index];
            var lo = Math.Max(from, begin); var hi = Math.Min(to, end);
            if (hi <= lo) continue;
            var p = primitives[index]; int count = 1;
            if (p.Kind == "ARC")
            {
                var radius = p.Radius!.Value;
                var angleStep = 2 * Math.Acos(Math.Clamp(1 - tessellationTolerance / radius, -1, 1));
                if (angleStep <= 0) Fail("Rendering tolerance is below numeric resolution.");
                var required = Math.Ceiling((hi - lo) / radius / Math.Min(angleStep, Math.PI / 2));
                if (required > 100000) Fail("Rendering exceeds the feature vertex bound.");
                count = (int)Math.Max(1, required);
            }
            for (var step = 0; step <= count; step++)
            {
                var point = PointOn(index, lo + (hi - lo) * step / count);
                if (coordinates.Count == 0 || !coordinates[^1].Equals2D(new Coordinate(point.X, point.Y))) coordinates.Add(new(point.X, point.Y));
                if (coordinates.Count > 100001) Fail("Rendering exceeds the feature vertex bound.");
            }
        }
        return new GeometryFactory(new PrecisionModel(), srid).CreateLineString(coordinates.ToArray());
    }
    private GeometryPoint PointOn(int index, double offset)
    {
        var begin = index == 0 ? 0 : ends[index - 1]; var fraction = (offset - begin) / (ends[index] - begin); var p = primitives[index];
        return p.Kind == "ARC" ? ArcPoint(p, fraction) : new(p.Start.X + fraction * (p.End.X - p.Start.X), p.Start.Y + fraction * (p.End.Y - p.Start.Y));
    }
    private (NativeAlignmentPrimitive Primitive, double Fraction) Locate(double offset)
    {
        ValidateOffset(offset);
        var index = Array.FindIndex(ends, end => offset < end);
        if (index < 0) index = ends.Length - 1;
        var start = index == 0 ? 0 : ends[index - 1];
        return (primitives[index], (offset - start) / (ends[index] - start));
    }
    private void ValidateOffset(double offset) { if (!double.IsFinite(offset) || offset < 0 || offset > Length) Fail("Offset is outside the analytic alignment."); }
    private static GeometryPoint ArcPoint(NativeAlignmentPrimitive p, double fraction) => new(p.Center!.X + p.Radius!.Value * Math.Cos(p.StartAngleRadians!.Value + fraction * p.SweepRadians!.Value), p.Center.Y + p.Radius.Value * Math.Sin(p.StartAngleRadians.Value + fraction * p.SweepRadians.Value));
    private static bool Finite(GeometryPoint? p) => p is not null && double.IsFinite(p.X) && double.IsFinite(p.Y);
    private static double Distance(GeometryPoint a, GeometryPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    // Floating-point consistency bound only; this does not represent survey accuracy or a CRS tolerance.
    private static bool Same(GeometryPoint a, GeometryPoint b) => Distance(a, b) <= 1e-10 * Math.Max(1, Math.Max(Math.Abs(a.X), Math.Max(Math.Abs(a.Y), Math.Max(Math.Abs(b.X), Math.Abs(b.Y)))));
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string detail) => throw new GeometryValidationException("native_alignment_invalid", detail);
}
