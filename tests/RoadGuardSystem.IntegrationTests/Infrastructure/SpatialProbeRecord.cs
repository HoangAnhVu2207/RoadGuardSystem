using NetTopologySuite.Geometries;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Integration test probe entity used exclusively for verifying SQL Server geography and geometry mapping.
/// Kept strictly within tests infrastructure — no fake entities exist in production.
/// </summary>
public sealed class SpatialProbeRecord
{
    public Guid Id { get; set; }

    /// <summary>
    /// Mapped to SQL Server geography, SRID 4326 (WGS 84 GPS coordinate).
    /// </summary>
    public Point GpsLocation { get; set; } = null!;

    /// <summary>
    /// Mapped to SQL Server geometry, with project UTM SRID (32648 or 32649).
    /// </summary>
    public Geometry EngineeringGeometry { get; set; } = null!;

    public int ProjectUtmSrid { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
