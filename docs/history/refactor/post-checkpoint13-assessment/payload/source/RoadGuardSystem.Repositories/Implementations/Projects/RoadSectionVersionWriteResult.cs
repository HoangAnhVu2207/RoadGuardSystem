namespace RoadGuardSystem.Repositories.Projects;

public sealed record RoadSectionVersionWriteResult(
    RoadSectionVersionWriteStatus Status,
    RoadSectionVersionWriteView? Version = null);
