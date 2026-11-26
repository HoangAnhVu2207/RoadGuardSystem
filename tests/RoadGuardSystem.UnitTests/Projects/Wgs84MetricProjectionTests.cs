using RoadGuardSystem.Repositories.Spatial;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class Wgs84MetricProjectionTests
{
    // Independently evaluated with PROJ (pyproj Transformer, EPSG:4326 -> EPSG,
    // always_xy=True). No inverse implementation from this repo produces expectations.
    [Theory]
    [InlineData(32648, 105, 10, 500000, 1105412.4913010783)]
    [InlineData(32649, 111, 10, 500000, 1105412.4913010783)]
    [InlineData(32648, 106.7, 10.8, 685864.1320529593, 1194382.272863328)]
    [InlineData(32649, 109, 21, 292110.7839273595, 2323448.290229021)]
    [InlineData(32648, 102.1, 23.2, 203155.22258247575, 2568621.1303078043)]
    [InlineData(32649, 113.9, 23.2, 796844.7774175243, 2568621.1303078043)]
    public void ForwardMatchesIndependentProjectionWithinOneCentimetre(int epsg, double longitude,
        double latitude, double easting, double northing)
    {
        var result = Wgs84MetricProjection.Project(longitude, latitude, epsg);
        Assert.NotNull(result);
        Assert.InRange(result.X, easting - .01, easting + .01);
        Assert.InRange(result.Y, northing - .01, northing + .01);
    }

    [Theory]
    [InlineData(0, 105, 10)]
    [InlineData(3405, 105, 10)]
    [InlineData(32648, 111, 10)]
    [InlineData(32649, 105, 10)]
    [InlineData(32648, 105, -1)]
    [InlineData(32648, 105, 85)]
    [InlineData(32648, double.NaN, 10)]
    public void UnsupportedOrOutOfAreaIsUnavailableRatherThanSnapped(int epsg, double longitude, double latitude)
        => Assert.Null(Wgs84MetricProjection.Project(longitude, latitude, epsg));
}
