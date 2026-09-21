using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-00")]
public sealed class SqlServerSpatialRoundTripTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;
    private static readonly GeometryFactory Srid4326Factory = new(new PrecisionModel(), SpatialConstants.GpsGeographySrid);
    private static readonly GeometryFactory Srid32648Factory = new(new PrecisionModel(), SpatialConstants.UtmZone48NSrid);
    private static readonly GeometryFactory Srid32649Factory = new(new PrecisionModel(), SpatialConstants.UtmZone49NSrid);

    public SqlServerSpatialRoundTripTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Positive: Fixture creates isolated, collision-safe database with unique name")]
    public void Fixture_Creates_Isolated_CollisionSafe_Database()
    {
        _fixture.DatabaseName.Should().StartWith("RoadGuard_Test_");
        _fixture.DatabaseName.Length.Should().BeGreaterThan(25);
    }

    [Fact(DisplayName = "Positive: Round-trip Point as geography(4326) on real SQL Server")]
    public async Task RoundTrip_Point_As_Geography_4326_Succeeds()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        // Hanoi coordinates: Longitude ~105.854444, Latitude ~21.028511
        // In GIS: X = Longitude, Y = Latitude
        var gpsPoint = Srid4326Factory.CreatePoint(new Coordinate(105.854444, 21.028511));
        var dummyLine = Srid32648Factory.CreateLineString(new[]
        {
            new Coordinate(588500.0, 2325000.0),
            new Coordinate(588600.0, 2325100.0)
        });

        var record = new SpatialProbeRecord
        {
            Id = id,
            GpsLocation = gpsPoint,
            EngineeringGeometry = dummyLine,
            ProjectUtmSrid = SpatialConstants.UtmZone48NSrid,
            Description = "GPS Survey Point at Km 10+500",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // ACT: Save to SQL Server
        await using (var context = _fixture.CreateDbContext())
        {
            context.SpatialProbes.Add(record);
            await context.SaveChangesAsync();
        }

        // ASSERT: Read back in a fresh DbContext
        await using (var verifyContext = _fixture.CreateDbContext())
        {
            var loaded = await verifyContext.SpatialProbes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            loaded.Should().NotBeNull();
            loaded!.GpsLocation.Should().NotBeNull();
            loaded.GpsLocation.SRID.Should().Be(4326, "geography column must preserve SRID 4326");
            loaded.GpsLocation.X.Should().BeApproximately(105.854444, 0.000001);
            loaded.GpsLocation.Y.Should().BeApproximately(21.028511, 0.000001);
        }
    }

    [Theory(DisplayName = "Positive: Round-trip engineering geometry with configured UTM SRID")]
    [InlineData(SpatialConstants.UtmZone48NSrid)]
    [InlineData(SpatialConstants.UtmZone49NSrid)]
    public async Task RoundTrip_EngineeringGeometry_With_Configured_Utm_Srid_Succeeds(int utmSrid)
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var factory = utmSrid == SpatialConstants.UtmZone48NSrid ? Srid32648Factory : Srid32649Factory;
        var lineString = factory.CreateLineString(new[]
        {
            new Coordinate(588500.0, 2325000.0),
            new Coordinate(588750.0, 2325250.0),
            new Coordinate(589000.0, 2325500.0)
        });

        var record = new SpatialProbeRecord
        {
            Id = id,
            GpsLocation = Srid4326Factory.CreatePoint(new Coordinate(106.629664, 10.823099)),
            EngineeringGeometry = lineString,
            ProjectUtmSrid = utmSrid,
            Description = $"UTM Line Segment for Zone {utmSrid}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // ACT: Save to SQL Server
        await using (var context = _fixture.CreateDbContext())
        {
            context.SpatialProbes.Add(record);
            await context.SaveChangesAsync();
        }

        // ASSERT: Read back in a fresh DbContext
        await using (var verifyContext = _fixture.CreateDbContext())
        {
            var loaded = await verifyContext.SpatialProbes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            loaded.Should().NotBeNull();
            loaded!.EngineeringGeometry.Should().NotBeNull();
            loaded.EngineeringGeometry.SRID.Should().Be(utmSrid, $"engineering geometry must preserve configured UTM SRID {utmSrid}");
            loaded.EngineeringGeometry.Coordinates.Should().HaveCount(3);
            loaded.EngineeringGeometry.Coordinates[0].X.Should().BeApproximately(588500.0, 0.001);
            loaded.EngineeringGeometry.Coordinates[0].Y.Should().BeApproximately(2325000.0, 0.001);
        }
    }

    [Fact(DisplayName = "Positive: Confirm SQL Server column types as sys.geography and sys.geometry via database catalog")]
    public async Task Confirm_Sql_Column_Types_Via_Database_Catalog()
    {
        // ARRANGE: Query SQL Server catalog metadata (sys.columns joined with sys.types)
        const string query = @"
SELECT c.name AS ColumnName, t.name AS TypeName
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('SpatialProbeRecords')
  AND c.name IN ('GpsLocation', 'EngineeringGeometry');";

        var columnTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ACT
        await using (var conn = new SqlConnection(_fixture.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var typeName = reader.GetString(1);
                columnTypes[colName] = typeName;
            }
        }

        // ASSERT: Verify exact SQL Server spatial column types
        columnTypes.Should().ContainKey("GpsLocation");
        columnTypes["GpsLocation"].Should().Be("geography", "GpsLocation must be mapped to SQL Server geography type");

        columnTypes.Should().ContainKey("EngineeringGeometry");
        columnTypes["EngineeringGeometry"].Should().Be("geometry", "EngineeringGeometry must be mapped to SQL Server geometry type");
    }

    [Fact(DisplayName = "Positive: Database is reliably dropped on fixture dispose")]
    public async Task Database_Is_Dropped_On_Fixture_Dispose()
    {
        // ARRANGE: Create an isolated database via sub-fixture on the live server
        var tempFixture = new SqlServerTestFixture(masterConnectionString: _fixture.MasterConnectionString);
        await tempFixture.InitializeAsync();
        var dbName = tempFixture.DatabaseName;

        var existsBefore = await _fixture.DatabaseExistsAsync(dbName);
        existsBefore.Should().BeTrue("database should exist after fixture initialization");

        // ACT: Dispose the sub-fixture
        await tempFixture.DisposeAsync();

        // ASSERT: Query master sys.databases on the still-living server to confirm database was dropped
        var existsAfter = await _fixture.DatabaseExistsAsync(dbName);
        existsAfter.Should().BeFalse("the test database must be dropped when fixture is disposed");
    }

    [Fact(DisplayName = "Positive: Empty database fixture does not create spatial probe schema")]
    public async Task EmptyDatabaseFixture_DoesNotCreateSpatialProbeSchema()
    {
        var tempFixture = new SqlServerTestFixture(
            masterConnectionString: _fixture.MasterConnectionString,
            createSpatialProbeSchema: false);
        await tempFixture.InitializeAsync();

        try
        {
            await using var context = tempFixture.CreateDbContext();
            var tableCount = await context.Database.SqlQueryRaw<int>(
                    "SELECT CAST(COUNT(*) AS int) AS [Value] FROM sys.tables WHERE [name] = 'SpatialProbeRecords'")
                .SingleAsync();

            tableCount.Should().Be(0);
        }
        finally
        {
            await tempFixture.DisposeAsync();
        }
    }
}
