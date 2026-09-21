namespace RoadGuardSystem.DTOs.Projects;

public sealed record RoadSectionWorkPackageDto(
    Guid RoadSectionId,
    string Code,
    string? Name,
    Guid CurrentVersionId,
    int VersionNo,
    string GeometryWkt,
    int Srid,
    DateTimeOffset EffectiveFrom,
    string ChangeReason);
