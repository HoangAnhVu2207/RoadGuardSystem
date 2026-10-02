namespace RoadGuardSystem.DTOs.Projects;

public sealed record RoadSectionVersionResponseDto(
    Guid ProjectId,
    Guid RoadSectionId,
    Guid RoadSectionVersionId,
    string Code,
    int VersionNo,
    bool IsCurrent,
    DateTimeOffset EffectiveFrom,
    string ChangeReason);
