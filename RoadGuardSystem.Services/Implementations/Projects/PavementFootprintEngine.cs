using RoadGuardSystem.DTOs.Projects;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;

namespace RoadGuardSystem.Services.Projects;

public static class PavementFootprintEngine
{
    public static PavementGeometryPreview Planned(NativeAlignment alignment, PavementPlanCreateInput input)
    {
        ArgumentNullException.ThrowIfNull(alignment); ArgumentNullException.ThrowIfNull(input);
        if (!double.IsFinite(input.DisplayToleranceMeters) || input.DisplayToleranceMeters <= 0)
            throw new ArgumentException("An explicit positive display tolerance is required.", nameof(input));
        if (alignment.Length >= (double)decimal.MaxValue)
            throw new ArgumentException("The alignment exceeds supported metric dimensions.", nameof(input));
        ArgumentNullException.ThrowIfNull(input.Layout);
        var canonicalLength = (decimal)alignment.Length;
        var layoutInput = input.Layout;
        var normalized = false;
        if (layoutInput.WidthProfile is { Length: > 0 } widths && widths[^1] is { ToOffsetMeters: > 0 } last &&
            last.ToOffsetMeters != canonicalLength && Math.Abs(last.ToOffsetMeters - canonicalLength) <= (decimal)(1e-12 * Math.Max(1, alignment.Length)))
        {
            // Numeric conversion consistency only: JSON round-trip double versus decimal.
            // This neither changes strip widths nor represents survey accuracy/tolerance.
            var adjusted = widths.ToArray(); adjusted[^1] = last with { ToOffsetMeters = canonicalLength };
            layoutInput = layoutInput with { WidthProfile = adjusted }; normalized = true;
        }
        var plan = PavementLayoutEngine.Plan(canonicalLength, layoutInput);
        if (normalized) plan = plan with { Warnings = plan.Warnings.Append("NATIVE_OFFSET_NUMERIC_CONVERSION_NORMALIZED").ToArray() };
        if (plan.Cells.Length > 10000) throw new ArgumentException("Footprint preview exceeds the feature bound.", nameof(input));
        long vertexCount = 0;
        var slabs = plan.Cells.Select(cell =>
        {
            var from = Math.Min(alignment.Length, (double)cell.FromOffsetMeters);
            var to = Math.Min(alignment.Length, (double)cell.ToOffsetMeters);
            var left = (double)cell.FromLateralMeters; var right = (double)cell.ToLateralMeters;
            var offsets = new SortedSet<double> { from, to };
            foreach (var boundary in alignment.PrimitiveBoundaries.Where(x => x > from && x < to)) offsets.Add(boundary);
            foreach (var arc in alignment.ArcRanges.Where(x => x.FromOffsetMeters < to && x.ToOffsetMeters > from))
            {
                var lateral = Math.Max(Math.Abs(left), Math.Abs(right));
                if (arc.Radius <= lateral) throw new ArgumentException("A slab offset reaches or crosses the inner curve centre.", nameof(input));
                var outerRadius = arc.Radius + lateral;
                var angle = Math.Min(Math.PI / 2, 2 * Math.Acos(Math.Clamp(1 - input.DisplayToleranceMeters / outerRadius, -1, 1)));
                if (angle <= 0) throw new ArgumentException("Display tolerance is below numeric resolution.", nameof(input));
                var low = Math.Max(from, arc.FromOffsetMeters); var high = Math.Min(to, arc.ToOffsetMeters);
                var required = Math.Ceiling((high - low) / arc.Radius / angle);
                if (required > 4096) throw new ArgumentException("Footprint vertex bound exceeded.", nameof(input));
                var steps = (int)Math.Max(1, required);
                for (var index = 0; index <= steps; index++) offsets.Add(low + (high - low) * index / steps);
            }
            if (offsets.Count > 4097) throw new ArgumentException("Footprint vertex bound exceeded.", nameof(input));
            vertexCount += 2L * offsets.Count + 1;
            if (vertexCount > 1000000) throw new ArgumentException("The footprint batch exceeds the vertex bound.", nameof(input));
            GeometryPoint Offset(double at, double lateral)
            {
                var point = alignment.PointAt(at); var tangent = alignment.TangentAt(at);
                return new(point.X - tangent.Y * lateral, point.Y + tangent.X * lateral);
            }
            var ring = offsets.Select(x => Offset(x, left)).Concat(offsets.Reverse().Select(x => Offset(x, right))).ToList();
            ring.Add(ring[0]);
            ValidatePolygon(ring.ToArray());
            return new SlabGeometrySnapshot(cell.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), cell.Sequence,
                ring.ToArray(), (double)(cell.ToOffsetMeters - cell.FromOffsetMeters), (double)(cell.ToLateralMeters - cell.FromLateralMeters), null, "ANALYTIC_PLAN", ProvenanceStatus: "SERVER_ANALYTIC_MODEL");
        }).ToArray();
        if (slabs.Sum(s => (long)s.Footprint.Length) > 1000000)
            throw new ArgumentException("The footprint batch exceeds the vertex bound.", nameof(input));
        return new(plan, slabs, plan.Warnings, input.DisplayToleranceMeters);
    }
    public static PavementGeometryPreview AsBuilt(PavementGeometryPreview planned, AsBuiltLayoutInput input)
    {
        ArgumentNullException.ThrowIfNull(planned); ArgumentNullException.ThrowIfNull(input);
        if (input.SourcePlanId == Guid.Empty || input.Slabs is null || input.Slabs.Length is < 1 or > 10000 ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 2000)
            throw new ArgumentException("A source plan, bounded slab evidence and a reason are required.", nameof(input));
        if (input.Slabs.Any(s => s is null || s.Footprint is null) || input.Slabs.Sum(s => (long)s.Footprint.Length) > 1000000)
            throw new ArgumentException("A bounded set of non-null slab footprints is required.", nameof(input));
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var polygons = new List<Geometry>();
        var slabs = input.Slabs.Select(slab =>
        {
            if (string.IsNullOrWhiteSpace(slab.Key) || slab.Key.Length > 80 || !keys.Add(slab.Key) ||
                slab.Key.Any(c => c is < '!' or > '~') ||
                string.IsNullOrWhiteSpace(slab.Source) || slab.Source.Length > 80 ||
                slab.PlannedSequence is { } sequence && !planned.Slabs.Any(s => s.PlannedSequence == sequence) ||
                slab.LengthMeters is { } length && (!double.IsFinite(length) || length <= 0) ||
                slab.WidthMeters is { } width && (!double.IsFinite(width) || width <= 0) ||
                (slab.LengthMeters is null || slab.WidthMeters is null) && (string.IsNullOrWhiteSpace(slab.DimensionUnknownReason) || slab.DimensionUnknownReason.Length > 500))
                throw new ArgumentException("Slab identity, provenance and explicit known/unknown dimensions are required.", nameof(input));
            polygons.Add(ValidatePolygon(slab.Footprint));
            return new SlabGeometrySnapshot(slab.Key, slab.PlannedSequence, slab.Footprint.ToArray(), slab.LengthMeters,
                slab.WidthMeters, slab.DimensionUnknownReason, slab.Source, slab.SourceFileId);
        }).ToArray();
        if (slabs.Sum(s => (long)s.Footprint.Length) > 1000000)
            throw new ArgumentException("The footprint batch exceeds the vertex bound.", nameof(input));
        var plannedArea = UnaryUnionOp.Union(planned.Slabs.Select(s => (Geometry)ValidatePolygon(s.Footprint)).ToList());
        var builtArea = UnaryUnionOp.Union(polygons);
        var overlap = Math.Max(0, polygons.Sum(p => p.Area) - builtArea.Area);
        var gap = plannedArea.Difference(builtArea).Area;
        var outside = builtArea.Difference(plannedArea).Area;
        var warnings = new List<string> { "RENDERED_COVERAGE_IS_NOT_SURVEY_ACCURACY" };
        if (gap > 0) warnings.Add("AS_BUILT_GAPS");
        if (overlap > 0) warnings.Add("AS_BUILT_OVERLAPS");
        if (outside > 0) warnings.Add("AS_BUILT_OUTSIDE_PLAN");
        return new(null, slabs, warnings.ToArray(), planned.DisplayToleranceMeters, gap, overlap);
    }
    private static Polygon ValidatePolygon(GeometryPoint[] points)
    {
        if (points is null || points.Length is < 4 or > 8195 || points.Any(p => p is null || !double.IsFinite(p.X) || !double.IsFinite(p.Y)) || points[0] != points[^1])
            throw new ArgumentException("A bounded closed native footprint is required.", nameof(points));
        var polygon = new GeometryFactory().CreatePolygon(points.Select(p => new Coordinate(p.X, p.Y)).ToArray());
        if (!polygon.IsValid || polygon.IsEmpty || !double.IsFinite(polygon.Area) || polygon.Area <= 0)
            throw new ArgumentException("Footprint must be a valid positive-area polygon.", nameof(points));
        return polygon;
    }
}
