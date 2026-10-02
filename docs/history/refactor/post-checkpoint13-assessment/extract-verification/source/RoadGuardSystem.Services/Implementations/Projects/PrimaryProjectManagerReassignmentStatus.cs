namespace RoadGuardSystem.Services.Projects;

public enum PrimaryProjectManagerReassignmentStatus
{
    Success,
    Replayed,
    InvalidInput,
    Forbidden,
    ProjectNotFound,
    ProjectClosed,
    ReplacementProjectManagerNotFound,
    StaleConcurrency,
    IdempotentConflict
}
