using FluentAssertions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.Repositories.Spatial;
using Xunit;
namespace RoadGuardSystem.UnitTests.Projects;

public sealed class H2NativeSpatialTests
{
    [Fact]
    public void Zero_srid_is_native_only_with_an_explicit_pinned_profile()
    {
        var geometry = new GeometryFactory(new PrecisionModel(), 0).CreateLineString([new(0, 0), new(1, 0)]);
        SpatialValidation.EnsureProjectEngineeringGeometry(geometry, Guid.NewGuid(), 0);
        var unpinned = () => SpatialValidation.EnsureProjectEngineeringGeometry(geometry, Guid.Empty, 0);
        unpinned.Should().Throw<ArgumentException>();
        var legacy = () => SpatialValidation.EnsureProjectEngineeringGeometry(geometry, 0); legacy.Should().Throw<ArgumentException>();
        var gps = () => SpatialValidation.EnsureGpsGeography(geometry); gps.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void Profile_geometry_srid_mismatch_is_rejected()
    {
        var geometry = new GeometryFactory(new PrecisionModel(), 0).CreateLineString([new(0, 0), new(1, 0)]);
        var mismatch = () => SpatialValidation.EnsureProjectEngineeringGeometry(geometry, Guid.NewGuid(), 1); mismatch.Should().Throw<ArgumentException>();
    }
}
