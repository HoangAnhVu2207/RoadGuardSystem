using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;

namespace RoadGuardSystem.Repositories.Spatial;

/// <summary>Display projection for explicitly identified WGS84 geographic and WGS84 UTM north coordinates.</summary>
internal static class Wgs84DisplayProjection
{
    internal static double[]? Coordinates(Coordinate point, int srid)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y)) return null;
        if (srid == SpatialConstants.GpsGeographySrid) return Geographic(point.X, point.Y);
        if (!SpatialConstants.IsAllowedProjectUtmSrid(srid) ||
            point.X is < 100000 or > 900000 || point.Y is < 0 or > 10000000) return null;

        // Inverse Transverse Mercator series on the WGS84 ellipsoid. UTM north
        // uses a 500 km false easting, zero false northing, and scale 0.9996.
        const double a = 6378137;
        const double f = 1 / 298.257223563;
        const double k = 0.9996;
        const double e2 = f * (2 - f);
        const double ep2 = e2 / (1 - e2);
        var n = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
        var mu = point.Y / (k * a * (1 - e2 / 4 - 3 * Math.Pow(e2, 2) / 64 - 5 * Math.Pow(e2, 3) / 256));
        var foot = mu
            + (3 * n / 2 - 27 * Math.Pow(n, 3) / 32) * Math.Sin(2 * mu)
            + (21 * n * n / 16 - 55 * Math.Pow(n, 4) / 32) * Math.Sin(4 * mu)
            + 151 * Math.Pow(n, 3) / 96 * Math.Sin(6 * mu)
            + 1097 * Math.Pow(n, 4) / 512 * Math.Sin(8 * mu);
        var sin = Math.Sin(foot);
        var cos = Math.Cos(foot);
        var t = Math.Pow(Math.Tan(foot), 2);
        var c = ep2 * cos * cos;
        var v = a / Math.Sqrt(1 - e2 * sin * sin);
        var r = v * (1 - e2) / (1 - e2 * sin * sin);
        var d = (point.X - 500000) / (v * k);
        var latitude = foot - v * Math.Tan(foot) / r *
            (d * d / 2 - (5 + 3 * t + 10 * c - 4 * c * c - 9 * ep2) * Math.Pow(d, 4) / 24
                + (61 + 90 * t + 298 * c + 45 * t * t - 252 * ep2 - 3 * c * c) * Math.Pow(d, 6) / 720);
        var longitude = (d - (1 + 2 * t + c) * Math.Pow(d, 3) / 6
            + (5 - 2 * c + 28 * t - 3 * c * c + 8 * ep2 + 24 * t * t) * Math.Pow(d, 5) / 120) / cos;
        var degrees = latitude * (180 / Math.PI);
        var offset = longitude * (180 / Math.PI);
        return degrees is >= 0 and <= 84 && Math.Abs(offset) <= 3.000001
            ? Geographic((srid == SpatialConstants.UtmZone48NSrid ? 105 : 111) + offset, degrees) : null;
    }

    internal static GeometryShapeFact? LineString(LineString? line, int? sourceSrid)
    {
        if (line is null || line.IsEmpty || line.SRID != sourceSrid) return null;
        var coordinates = new List<double[]>(line.NumPoints);
        foreach (var point in line.Coordinates)
        {
            if (Coordinates(point, line.SRID) is not { } xy) return null;
            coordinates.Add(xy);
        }
        return new("LineString", coordinates.ToArray());
    }

    private static double[]? Geographic(double longitude, double latitude)
        => longitude is >= -180 and <= 180 && latitude is >= -90 and <= 90 ? [longitude, latitude] : null;
}
