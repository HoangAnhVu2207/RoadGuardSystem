using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Repositories.Defects;
using NetTopologySuite.Geometries;
using RoadGuardSystem.Repositories.Spatial;

namespace RoadGuardSystem.Repositories.Implementations.Defects;

public sealed class ReportMatchingLocationRepository(RoadGuardDbContext db) : IReportMatchingLocationRepository
{
    public Task<bool> ReceiptExistsAsync(Guid actor, Guid project, string key, CancellationToken token)
        => db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == actor && row.ProjectId == project &&
            row.Operation == "matching.location.confirm.v1" && row.IdempotencyKey == key, token);
    public async Task SaveAsync(ReportMatchingLocation location, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Matching confirmation requires the source transaction.");
        db.Add(location);
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), location.ActorId, location.ConfirmedAt,
            "report_matching_location_confirmed", "Report", location.ReportId, null,
            System.Text.Json.JsonSerializer.Serialize(new { confirmationId = location.Id, location.Sequence }),
            location.Reason, "matching.location.confirm.v1", null, ["confirmationId", "sequence"]));
        await db.SaveChangesAsync(token);
    }

    public async Task<IReadOnlyDictionary<Guid, CandidateSpatialDistance>> DistancesAsync(Guid project, decimal latitude,
        decimal longitude, CancellationToken token)
    {
        // SQL Server geography measures WGS84 points in metres. UTM display
        // projection is intentionally not reused as an authoritative measurement adapter.
        var targets = await (from defect in db.Defects.AsNoTracking()
                             join route in db.RoadSectionVersions.AsNoTracking() on defect.RoadSectionVersionId equals route.Id into routes
                             from route in routes.DefaultIfEmpty()
                             where defect.ProjectId == project
                             select new { defect.Id, defect.Geometry, RouteId = (Guid?)route.Id, route.CrsProfileRevisionId }).ToArrayAsync(token);
        var result = new Dictionary<Guid, CandidateSpatialDistance>();
        foreach (var target in targets)
        {
            var shape = target.Geometry;
            if (shape is null || shape.IsEmpty) { result[target.Id] = new(null, "TARGET_GEOMETRY_MISSING"); continue; }
            if (!shape.IsValid || shape.Coordinates.Any(c => !double.IsFinite(c.X) || !double.IsFinite(c.Y)))
            { result[target.Id] = new(null, "TARGET_GEOMETRY_INVALID"); continue; }
            if (target.RouteId is null)
            { result[target.Id] = new(null, "TARGET_CRS_CONTEXT_MISSING"); continue; }
            if (target.CrsProfileRevisionId is not null)
            { result[target.Id] = new(null, "NATIVE_CRS_TRANSFORM_UNAVAILABLE"); continue; }
            if (shape.SRID == 4326)
            { result[target.Id] = new(null, "GEOGRAPHIC_GEOMETRY_UNSUPPORTED"); continue; }
            if (!SpatialConstants.IsAllowedProjectUtmSrid(shape.SRID))
            { result[target.Id] = new(null, "TARGET_CRS_UNSUPPORTED"); continue; }
            // The existing independently checked inverse validates the declared
            // zone's area only; distances below still use the original grid shape.
            if (shape.Coordinates.Any(c => c.X is < 100000 or > 900000 || c.Y is < 0 or > 10000000 ||
                Wgs84DisplayProjection.Coordinates(c, shape.SRID) is null))
            { result[target.Id] = new(null, "TARGET_COORDINATES_OUTSIDE_CRS_AREA"); continue; }
            if (Wgs84MetricProjection.Project((double)longitude, (double)latitude, shape.SRID) is not { } xy)
            { result[target.Id] = new(null, "SOURCE_OUTSIDE_SUPPORTED_CRS_AREA"); continue; }
            // Minimum Euclidean distance to the actual projected shape, including
            // its interior. No centroid, snap, matching threshold or ground-scale claim.
            var meters = shape.Distance(new Point(xy) { SRID = shape.SRID });
            result[target.Id] = double.IsFinite(meters) && meters >= 0
                ? new(meters, "MEASURED", "WGS84_UTM_GRID_MINIMUM") : new(null, "DISTANCE_UNAVAILABLE");
        }
        var points = targets.Where(row => row.RouteId != null && row.CrsProfileRevisionId == null && row.Geometry is NetTopologySuite.Geometries.Point
        { SRID: 4326, IsEmpty: false, IsValid: true } point &&
            double.IsFinite(point.X) && double.IsFinite(point.Y) && point.X is >= -180 and <= 180 && point.Y is >= -90 and <= 90)
            .Select(row => new { id = row.Id, latitude = row.Geometry!.Coordinate.Y, longitude = row.Geometry.Coordinate.X }).ToArray();
        if (points.Length == 0) return result;
        // Materialize valid points before calling SQL's geography constructor: SQL
        // may evaluate CLR functions before WHERE predicates on null/nonpoint rows.
        // No missing coordinate is replaced with zero.
        var json = System.Text.Json.JsonSerializer.Serialize(points);
        var rows = await db.Database.SqlQuery<DistanceRow>($"""
            SELECT [Id], geography::Point({latitude},{longitude},4326).STDistance(
                geography::Point([Latitude],[Longitude],4326)) AS [Meters]
            FROM OPENJSON({json}) WITH ([Id] uniqueidentifier '$.id', [Latitude] float '$.latitude', [Longitude] float '$.longitude')
            """).ToArrayAsync(token);
        foreach (var row in rows.Where(row => double.IsFinite(row.Meters) && row.Meters >= 0))
            result[row.Id] = new(row.Meters, "MEASURED", "WGS84_ELLIPSOID_POINT_DISTANCE");
        return result;
    }
    private sealed record DistanceRow(Guid Id, double Meters);
}
