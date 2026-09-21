using NetTopologySuite.Geometries;

namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class RoadSectionVersion
{
    private RoadSectionVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid RoadSectionId { get; private set; }

    public int VersionNo { get; private set; }

    public bool IsCurrent { get; private set; }

    public LineString Geometry { get; private set; } = null!;

    public DateTimeOffset EffectiveFrom { get; private set; }

    public string ChangeReason { get; private set; } = string.Empty;

    public static RoadSectionVersion Create(
        Guid id,
        Guid roadSectionId,
        int versionNo,
        bool isCurrent,
        LineString geometry,
        DateTimeOffset effectiveFrom,
        string changeReason)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Road section version id must not be empty.", nameof(id));
        }

        if (roadSectionId == Guid.Empty)
        {
            throw new ArgumentException("Road section id must not be empty.", nameof(roadSectionId));
        }

        if (versionNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNo), "Version number must be positive.");
        }

        ArgumentNullException.ThrowIfNull(geometry);
        return new RoadSectionVersion
        {
            Id = id,
            RoadSectionId = roadSectionId,
            VersionNo = versionNo,
            IsCurrent = isCurrent,
            Geometry = geometry,
            EffectiveFrom = effectiveFrom.ToUniversalTime(),
            ChangeReason = ValidateRequired(changeReason, nameof(changeReason))
        };
    }

    public void MarkCurrent() => IsCurrent = true;

    public void ClearCurrent() => IsCurrent = false;

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
