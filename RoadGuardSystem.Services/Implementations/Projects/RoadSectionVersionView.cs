namespace RoadGuardSystem.Services.Projects;

public sealed record RoadSectionVersionView(
    Guid ProjectId,
    Guid RoadSectionId,
    Guid RoadSectionVersionId,
    string Code,
    int VersionNo,
    bool IsCurrent,
    DateTimeOffset EffectiveFrom,
    string ChangeReason);
