namespace RoadGuardSystem.Repositories.Identity;

public enum IdentityOnboardingPersistenceStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    InvalidInput,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests
}
