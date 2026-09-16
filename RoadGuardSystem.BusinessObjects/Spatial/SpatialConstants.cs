namespace RoadGuardSystem.BusinessObjects.Spatial;

/// <summary>
/// Spatial reference system identifiers (SRIDs) for RoadGuard per Data Dictionary v1 sections 2.4 & 2.5.
/// Domain invariant definitions owned by BusinessObjects.
/// </summary>
public static class SpatialConstants
{
    /// <summary>
    /// GPS / raw location standard: WGS 84 geographic coordinate system.
    /// SQL Server type: geography, SRID: 4326.
    /// </summary>
    public const int GpsGeographySrid = 4326;

    /// <summary>
    /// Projected engineering coordinate system: UTM Zone 48N (Vietnam Western / Central).
    /// SQL Server type: geometry, SRID: 32648.
    /// </summary>
    public const int UtmZone48NSrid = 32648;

    /// <summary>
    /// Projected engineering coordinate system: UTM Zone 49N (Vietnam Eastern / Islands).
    /// SQL Server type: geometry, SRID: 32649.
    /// </summary>
    public const int UtmZone49NSrid = 32649;

    /// <summary>
    /// The set of permitted project UTM SRIDs per specification.
    /// Systems must configure one of these per project; no global hardcoded UTM zone is allowed.
    /// </summary>
    public static readonly IReadOnlySet<int> AllowedProjectUtmSrids = new HashSet<int>
    {
        UtmZone48NSrid,
        UtmZone49NSrid
    };

    /// <summary>
    /// Checks whether the given SRID is a valid project UTM SRID.
    /// </summary>
    public static bool IsAllowedProjectUtmSrid(int srid) => AllowedProjectUtmSrids.Contains(srid);
}
