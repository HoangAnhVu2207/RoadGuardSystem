namespace RoadGuardSystem.Repositories.Projects;

public enum RoadSectionVersionWriteStatus
{
    Success,
    Replayed,
    InvalidInput,
    ProjectNotFound,
    ProjectClosed,
    RoadSectionNotFound,
    RoadSectionCodeConflict,
    StaleConcurrency,
    IdempotentConflict
}
