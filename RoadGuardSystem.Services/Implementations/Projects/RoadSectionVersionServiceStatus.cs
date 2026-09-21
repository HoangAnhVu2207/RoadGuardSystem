namespace RoadGuardSystem.Services.Projects;

public enum RoadSectionVersionServiceStatus
{
    Success,
    Replayed,
    InvalidInput,
    Forbidden,
    ProjectNotFound,
    ProjectClosed,
    RoadSectionNotFound,
    RoadSectionCodeConflict,
    StaleConcurrency,
    IdempotentConflict
}
