namespace RoadGuardSystem.Repositories.Identity;

public enum V2AccountUpdatePersistenceStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    NotFound,
    RoleNotFound,
    StaleConcurrency,
    InvalidInput
}
