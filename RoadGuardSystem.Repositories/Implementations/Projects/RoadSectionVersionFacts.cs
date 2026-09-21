namespace RoadGuardSystem.Repositories.Projects;

public sealed record RoadSectionVersionFacts(
    Guid ProjectId,
    int? EngineeringUtmSrid,
    Guid? CurrentVersionId,
    int? CurrentVersionNo);
