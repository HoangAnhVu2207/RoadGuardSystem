using NetTopologySuite.Geometries;

namespace RoadGuardSystem.Repositories.Spatial;

/// <summary>
/// Validates geometry values before they cross a persistence boundary.
/// </summary>
public static class SpatialValidation
{
    /// <summary>
    /// Validates that a GPS geography object has a valid non-zero SRID equal to 4326.
    /// Rejects null, SRID 0, and non-4326 coordinates.
    /// </summary>
    public static void EnsureGpsGeography(Geometry? geometry)
    {
        if (geometry is null)
        {
            throw new ArgumentNullException(nameof(geometry), "GPS geography geometry cannot be null.");
        }

        if (geometry.SRID == 0)
        {
            throw new ArgumentException(
                "Spatial SRID 0 is forbidden. GPS coordinates require SRID 4326 (WGS 84).",
                nameof(geometry));
        }

        if (geometry.SRID != SpatialConstants.GpsGeographySrid)
        {
            throw new ArgumentException(
                $"GPS geography requires SRID {SpatialConstants.GpsGeographySrid} (WGS 84), but found {geometry.SRID}.",
                nameof(geometry));
        }
    }

    /// <summary>
    /// Validates that an engineering geometry object uses a valid project UTM SRID and matches the configured project SRID.
    /// Rejects null, SRID 0, unsupported project SRIDs, and geometry SRID mismatches.
    /// </summary>
    public static void EnsureProjectEngineeringGeometry(Geometry? geometry, int projectUtmSrid)
    {
        if (geometry is null)
        {
            throw new ArgumentNullException(nameof(geometry), "Engineering geometry cannot be null.");
        }

        if (!SpatialConstants.AllowedProjectUtmSrids.Contains(projectUtmSrid))
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectUtmSrid),
                projectUtmSrid,
                $"Project UTM SRID {projectUtmSrid} is not supported. Allowed SRIDs are: {string.Join(", ", SpatialConstants.AllowedProjectUtmSrids)}.");
        }

        if (geometry.SRID == 0)
        {
            throw new ArgumentException(
                "Spatial SRID 0 is forbidden. Engineering geometry requires a configured project UTM SRID.",
                nameof(geometry));
        }

        if (geometry.SRID != projectUtmSrid)
        {
            throw new ArgumentException(
                $"Geometry SRID {geometry.SRID} does not match configured project UTM SRID {projectUtmSrid}.",
                nameof(geometry));
        }
    }
}
