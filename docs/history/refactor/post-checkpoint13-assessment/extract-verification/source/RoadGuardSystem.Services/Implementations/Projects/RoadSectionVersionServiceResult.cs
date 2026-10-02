namespace RoadGuardSystem.Services.Projects;

public sealed record RoadSectionVersionServiceResult(
    RoadSectionVersionServiceStatus Status,
    RoadSectionVersionView? Version = null);
