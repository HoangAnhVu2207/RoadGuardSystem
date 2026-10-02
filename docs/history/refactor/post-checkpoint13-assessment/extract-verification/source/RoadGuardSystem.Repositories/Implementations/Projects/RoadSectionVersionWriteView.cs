namespace RoadGuardSystem.Repositories.Projects;

public sealed record RoadSectionVersionWriteView(
    Guid ProjectId,
    Guid RoadSectionId,
    Guid RoadSectionVersionId,
    string Code,
    int VersionNo,
    bool IsCurrent,
    DateTimeOffset EffectiveFrom,
    string ChangeReason);
