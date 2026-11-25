using NetTopologySuite.Geometries;

namespace RoadGuardSystem.Repositories.Spatial;

/// <summary>WGS84 to WGS84 UTM north only. This is not a custom/VN2000 datum adapter.</summary>
public static class Wgs84MetricProjection
{
    public static Coordinate? Project(double longitude, double latitude, int srid)
    {
        if (!double.IsFinite(longitude) || !double.IsFinite(latitude) ||
            latitude is < 0 or > 84 || !SpatialConstants.IsAllowedProjectUtmSrid(srid)) return null;
        var central = srid == 32648 ? 105d : 111d;
        if (Math.Abs(longitude - central) > 3) return null;
        // Snyder ellipsoidal TM series, restricted to the UTM zone. WGS84 a/f,
        // false easting 500000m, false northing 0m, k0=.9996. Reference/control
        // vectors are independently generated with PROJ, not inverse round trips.
        const double a = 6378137, f = 1 / 298.257223563, k0 = .9996;
        var e2 = f * (2 - f); var ep2 = e2 / (1 - e2);
        var phi = latitude * Math.PI / 180; var lambda = (longitude - central) * Math.PI / 180;
        var sin = Math.Sin(phi); var cos = Math.Cos(phi); var tan = Math.Tan(phi);
        var n = a / Math.Sqrt(1 - e2 * sin * sin); var t = tan * tan; var c = ep2 * cos * cos;
        var aa = cos * lambda;
        var m = a * ((1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * e2 * e2 * e2 / 256) * phi
            - (3 * e2 / 8 + 3 * e2 * e2 / 32 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(2 * phi)
            + (15 * e2 * e2 / 256 + 45 * e2 * e2 * e2 / 1024) * Math.Sin(4 * phi)
            - 35 * e2 * e2 * e2 / 3072 * Math.Sin(6 * phi));
        var x = 500000 + k0 * n * (aa + (1 - t + c) * Math.Pow(aa, 3) / 6
            + (5 - 18 * t + t * t + 72 * c - 58 * ep2) * Math.Pow(aa, 5) / 120);
        var y = k0 * (m + n * tan * (aa * aa / 2 + (5 - t + 9 * c + 4 * c * c) * Math.Pow(aa, 4) / 24
            + (61 - 58 * t + t * t + 600 * c - 330 * ep2) * Math.Pow(aa, 6) / 720));
        return double.IsFinite(x) && double.IsFinite(y) ? new(x, y) : null;
    }
}
