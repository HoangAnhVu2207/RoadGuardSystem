using FluentAssertions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.Repositories.Spatial;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Spatial;

[Trait("TaskId", "P2-00")]
public sealed class SpatialInvariantTests
{
    private static readonly GeometryFactory Srid0Factory = new(new PrecisionModel(), 0);
    private static readonly GeometryFactory Srid4326Factory = new(new PrecisionModel(), SpatialConstants.GpsGeographySrid);
    private static readonly GeometryFactory Srid32648Factory = new(new PrecisionModel(), SpatialConstants.UtmZone48NSrid);
    private static readonly GeometryFactory Srid32649Factory = new(new PrecisionModel(), SpatialConstants.UtmZone49NSrid);
    private static readonly GeometryFactory Srid3857Factory = new(new PrecisionModel(), 3857);

    [Fact(DisplayName = "Negative: Geography with null geometry throws ArgumentNullException")]
    public void Geography_With_Null_Throws_ArgumentNullException()
    {
        var act = () => SpatialValidation.EnsureGpsGeography(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Negative: Geography with SRID 0 is forbidden and throws ArgumentException")]
    public void Geography_With_Srid_Zero_Throws_ArgumentException()
    {
        var point = Srid0Factory.CreatePoint(new Coordinate(105.854444, 21.028511));

        var act = () => SpatialValidation.EnsureGpsGeography(point);

        act.Should().Throw<ArgumentException>("SRID 0 is explicitly forbidden")
            .WithMessage("*SRID 0*");
    }

    [Fact(DisplayName = "Negative: Geography with non-4326 SRID throws ArgumentException")]
    public void Geography_With_Non4326_Srid_Throws_ArgumentException()
    {
        var point = Srid3857Factory.CreatePoint(new Coordinate(11783656.0, 2394625.0));

        var act = () => SpatialValidation.EnsureGpsGeography(point);

        act.Should().Throw<ArgumentException>("GPS geography strictly requires SRID 4326")
            .WithMessage("*4326*");
    }

    [Fact(DisplayName = "Negative: Engineering geometry with null throws ArgumentNullException")]
    public void EngineeringGeometry_With_Null_Throws_ArgumentNullException()
    {
        var act = () => SpatialValidation.EnsureProjectEngineeringGeometry(null, SpatialConstants.UtmZone48NSrid);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Negative: Engineering geometry with SRID 0 is forbidden and throws ArgumentException")]
    public void EngineeringGeometry_With_Srid_Zero_Throws_ArgumentException()
    {
        var point = Srid0Factory.CreatePoint(new Coordinate(588500.0, 2325000.0));

        var act = () => SpatialValidation.EnsureProjectEngineeringGeometry(point, SpatialConstants.UtmZone48NSrid);

        act.Should().Throw<ArgumentException>("SRID 0 is explicitly forbidden")
            .WithMessage("*SRID 0*");
    }

    [Fact(DisplayName = "Negative: Engineering geometry with unsupported project SRID throws ArgumentOutOfRangeException")]
    public void EngineeringGeometry_With_Unsupported_Project_Srid_Throws()
    {
        var point = Srid3857Factory.CreatePoint(new Coordinate(588500.0, 2325000.0));

        // Attempting to configure project with SRID 3857 (Web Mercator), which is not an allowed UTM zone (32648 or 32649)
        var act = () => SpatialValidation.EnsureProjectEngineeringGeometry(point, 3857);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*3857*");
    }

    [Fact(DisplayName = "Negative: Engineering geometry with mismatched project SRID throws ArgumentException")]
    public void EngineeringGeometry_With_Mismatched_Srid_Throws_ArgumentException()
    {
        // Geometry has SRID 32648 but project is configured for 32649
        var point = Srid32648Factory.CreatePoint(new Coordinate(588500.0, 2325000.0));

        var act = () => SpatialValidation.EnsureProjectEngineeringGeometry(point, SpatialConstants.UtmZone49NSrid);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*32648*")
            .WithMessage("*32649*");
    }

    [Fact(DisplayName = "Positive: Valid geography Point with SRID 4326 passes validation")]
    public void Geography_With_Srid_4326_Succeeds()
    {
        var point = Srid4326Factory.CreatePoint(new Coordinate(105.854444, 21.028511));

        var act = () => SpatialValidation.EnsureGpsGeography(point);

        act.Should().NotThrow();
    }

    [Theory(DisplayName = "Positive: Valid engineering geometry with allowed UTM SRIDs passes validation")]
    [InlineData(SpatialConstants.UtmZone48NSrid)]
    [InlineData(SpatialConstants.UtmZone49NSrid)]
    public void EngineeringGeometry_With_Valid_Utm_Srids_Succeeds(int utmSrid)
    {
        var factory = new GeometryFactory(new PrecisionModel(), utmSrid);
        var line = factory.CreateLineString(new[]
        {
            new Coordinate(588500.0, 2325000.0),
            new Coordinate(588600.0, 2325100.0)
        });

        var act = () => SpatialValidation.EnsureProjectEngineeringGeometry(line, utmSrid);

        act.Should().NotThrow();
    }

    [Fact(DisplayName = "Negative: AllowedProjectUtmSrids is truly immutable and cannot be mutated by caller")]
    public void AllowedProjectUtmSrids_Is_Truly_Immutable()
    {
        var allowed = SpatialConstants.AllowedProjectUtmSrids;

        // If cast to mutable collection interfaces, mutation operations throw NotSupportedException
        if (allowed is ICollection<int> collection)
        {
            var addAct = () => collection.Add(99999);
            addAct.Should().Throw<NotSupportedException>("modifying frozen/immutable set is strictly unsupported");

            var clearAct = () => collection.Clear();
            clearAct.Should().Throw<NotSupportedException>("modifying frozen/immutable set is strictly unsupported");

            var removeAct = () => collection.Remove(SpatialConstants.UtmZone48NSrid);
            removeAct.Should().Throw<NotSupportedException>("modifying frozen/immutable set is strictly unsupported");
        }

        // Verify it contains exactly the two allowed UTM zones and cannot be altered
        allowed.Should().BeEquivalentTo(new[] { SpatialConstants.UtmZone48NSrid, SpatialConstants.UtmZone49NSrid });
        SpatialConstants.IsAllowedProjectUtmSrid(SpatialConstants.UtmZone48NSrid).Should().BeTrue();
        SpatialConstants.IsAllowedProjectUtmSrid(SpatialConstants.UtmZone49NSrid).Should().BeTrue();
        SpatialConstants.IsAllowedProjectUtmSrid(3857).Should().BeFalse();
    }
}
