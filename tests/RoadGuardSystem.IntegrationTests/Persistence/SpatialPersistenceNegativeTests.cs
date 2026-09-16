using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Spatial;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

/// <summary>
/// Persistence-level negative tests verifying that invalid spatial data is rejected
/// when calling SaveChangesAsync or executing direct database operations.
/// Uses IClassFixture so the database is initialized once for the test class.
/// </summary>
[Trait("TaskId", "P2-00")]
public sealed class SpatialPersistenceNegativeTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;
    private static readonly GeometryFactory Srid0Factory = new(new PrecisionModel(), 0);
    private static readonly GeometryFactory Srid4326Factory = new(new PrecisionModel(), SpatialConstants.GpsGeographySrid);
    private static readonly GeometryFactory Srid32648Factory = new(new PrecisionModel(), SpatialConstants.UtmZone48NSrid);
    private static readonly GeometryFactory Srid32649Factory = new(new PrecisionModel(), SpatialConstants.UtmZone49NSrid);
    private static readonly GeometryFactory Srid3857Factory = new(new PrecisionModel(), 3857);

    public SpatialPersistenceNegativeTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Negative Persistence: SaveChangesAsync rejects GPS geography with SRID 0")]
    public async Task SaveChangesAsync_Rejects_GpsGeography_With_Srid_Zero()
    {
        await using var context = _fixture.CreateDbContext();
        var record = new SpatialProbeRecord
        {
            Id = Guid.NewGuid(),
            GpsLocation = Srid0Factory.CreatePoint(new Coordinate(105.85, 21.02)),
            EngineeringGeometry = Srid32648Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            ProjectUtmSrid = SpatialConstants.UtmZone48NSrid,
            Description = "Invalid GPS SRID 0",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.SpatialProbes.Add(record);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>("GPS geography with SRID 0 must be rejected on SaveChangesAsync");
    }

    [Fact(DisplayName = "Negative Persistence: SaveChangesAsync rejects GPS geography with non-4326 SRID")]
    public async Task SaveChangesAsync_Rejects_GpsGeography_With_Non4326_Srid()
    {
        await using var context = _fixture.CreateDbContext();
        var record = new SpatialProbeRecord
        {
            Id = Guid.NewGuid(),
            GpsLocation = Srid3857Factory.CreatePoint(new Coordinate(11783656.0, 2394625.0)),
            EngineeringGeometry = Srid32648Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            ProjectUtmSrid = SpatialConstants.UtmZone48NSrid,
            Description = "Invalid GPS SRID 3857",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.SpatialProbes.Add(record);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>("GPS geography with non-4326 SRID must be rejected on SaveChangesAsync");
    }

    [Fact(DisplayName = "Negative Persistence: SaveChangesAsync rejects Engineering Geometry with SRID 0")]
    public async Task SaveChangesAsync_Rejects_EngineeringGeometry_With_Srid_Zero()
    {
        await using var context = _fixture.CreateDbContext();
        var record = new SpatialProbeRecord
        {
            Id = Guid.NewGuid(),
            GpsLocation = Srid4326Factory.CreatePoint(new Coordinate(105.85, 21.02)),
            EngineeringGeometry = Srid0Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            ProjectUtmSrid = SpatialConstants.UtmZone48NSrid,
            Description = "Invalid Engineering SRID 0",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.SpatialProbes.Add(record);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>("Engineering geometry with SRID 0 must be rejected on SaveChangesAsync");
    }

    [Fact(DisplayName = "Negative Persistence: SaveChangesAsync rejects unsupported ProjectUtmSrid")]
    public async Task SaveChangesAsync_Rejects_Unsupported_ProjectUtmSrid()
    {
        await using var context = _fixture.CreateDbContext();
        var record = new SpatialProbeRecord
        {
            Id = Guid.NewGuid(),
            GpsLocation = Srid4326Factory.CreatePoint(new Coordinate(105.85, 21.02)),
            EngineeringGeometry = Srid3857Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            ProjectUtmSrid = 3857, // Unsupported UTM zone
            Description = "Unsupported Project UTM SRID 3857",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.SpatialProbes.Add(record);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>("Project UTM SRID outside 32648/32649 must be rejected on SaveChangesAsync");
    }

    [Fact(DisplayName = "Negative Persistence: SaveChangesAsync rejects EngineeringGeometry SRID mismatching ProjectUtmSrid")]
    public async Task SaveChangesAsync_Rejects_Mismatched_EngineeringGeometry_Srid()
    {
        await using var context = _fixture.CreateDbContext();
        var record = new SpatialProbeRecord
        {
            Id = Guid.NewGuid(),
            GpsLocation = Srid4326Factory.CreatePoint(new Coordinate(105.85, 21.02)),
            EngineeringGeometry = Srid32648Factory.CreateLineString(new[] { new Coordinate(588500, 2325000), new Coordinate(588600, 2325100) }),
            ProjectUtmSrid = SpatialConstants.UtmZone49NSrid, // Mismatch: geometry is 32648, project is 32649
            Description = "Mismatched Engineering SRID",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        context.SpatialProbes.Add(record);
        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>("Mismatched engineering geometry SRID must be rejected on SaveChangesAsync");
    }

    [Fact(DisplayName = "Negative Persistence: SQL Server check constraint directly rejects invalid ProjectUtmSrid")]
    public async Task Database_CheckConstraint_Rejects_Invalid_ProjectUtmSrid()
    {
        // Direct SQL insert bypassing application logic to prove database CHECK constraint enforcement
        var insertSql = $@"
INSERT INTO [SpatialProbeRecords] ([Id], [GpsLocation], [EngineeringGeometry], [ProjectUtmSrid], [Description], [CreatedAtUtc])
VALUES (NEWID(), geography::Point(21.02, 105.85, 4326), geometry::Point(588500, 2325000, 32648), 99999, 'Invalid Zone', SYSDATETIMEOFFSET());";

        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(insertSql, conn);

        var act = () => cmd.ExecuteNonQueryAsync();

        await act.Should().ThrowAsync<SqlException>("database check constraint CK_SpatialProbeRecords_ProjectUtmSrid must reject unconfigured SRID");
    }

    [Fact(DisplayName = "Negative Persistence: SQL Server check constraint directly rejects invalid GpsLocation SRID")]
    public async Task Database_CheckConstraint_Rejects_Invalid_GpsLocation_Srid()
    {
        // Direct SQL insert with SRID 4327 (not 4326)
        var insertSql = $@"
INSERT INTO [SpatialProbeRecords] ([Id], [GpsLocation], [EngineeringGeometry], [ProjectUtmSrid], [Description], [CreatedAtUtc])
VALUES (NEWID(), geography::STGeomFromText('POINT(105.85 21.02)', 4327), geometry::Point(588500, 2325000, 32648), 32648, 'Invalid GPS SRID', SYSDATETIMEOFFSET());";

        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(insertSql, conn);

        var act = () => cmd.ExecuteNonQueryAsync();

        await act.Should().ThrowAsync<SqlException>("database check constraint CK_SpatialProbeRecords_GpsLocation_Srid must reject non-4326 geography");
    }
}
