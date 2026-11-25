using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Repositories.Defects;

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

    public async Task<IReadOnlyDictionary<Guid, double>> DistancesAsync(Guid project, decimal latitude,
        decimal longitude, CancellationToken token)
    {
        // SQL Server geography measures WGS84 points in metres. UTM display
        // projection is intentionally not reused as an authoritative measurement adapter.
        var targets = await db.Defects.AsNoTracking().Where(row => row.ProjectId == project && row.Geometry != null)
            .Select(row => new { row.Id, row.Geometry }).ToArrayAsync(token);
        var points = targets.Where(row => row.Geometry is NetTopologySuite.Geometries.Point
        { SRID: 4326, IsEmpty: false, IsValid: true } point &&
            double.IsFinite(point.X) && double.IsFinite(point.Y) && point.X is >= -180 and <= 180 && point.Y is >= -90 and <= 90)
            .Select(row => new { id = row.Id, latitude = row.Geometry!.Coordinate.Y, longitude = row.Geometry.Coordinate.X }).ToArray();
        if (points.Length == 0) return new Dictionary<Guid, double>();
        // Materialize valid points before calling SQL's geography constructor: SQL
        // may evaluate CLR functions before WHERE predicates on null/nonpoint rows.
        // No missing coordinate is replaced with zero.
        var json = System.Text.Json.JsonSerializer.Serialize(points);
        var rows = await db.Database.SqlQuery<DistanceRow>($"""
            SELECT [Id], geography::Point({latitude},{longitude},4326).STDistance(
                geography::Point([Latitude],[Longitude],4326)) AS [Meters]
            FROM OPENJSON({json}) WITH ([Id] uniqueidentifier '$.id', [Latitude] float '$.latitude', [Longitude] float '$.longitude')
            """).ToArrayAsync(token);
        return rows.Where(row => double.IsFinite(row.Meters) && row.Meters >= 0).ToDictionary(row => row.Id, row => row.Meters);
    }
    private sealed record DistanceRow(Guid Id, double Meters);
}
