namespace RoadGuardSystem.Repositories.Projects;

public enum PrimaryProjectManagerWriteStatus
{
    Success,
    Replayed,
    IdempotentConflict,
    NotFound,
    StaleConcurrency,
    InvalidInput
}
