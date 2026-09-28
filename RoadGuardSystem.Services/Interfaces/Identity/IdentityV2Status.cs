namespace RoadGuardSystem.Services.Identity;

public enum IdentityV2Status
{
    Success,
    IdempotentReplay,
    Forbidden,
    NotFound,
    InvalidInput,
    PreconditionRequired,
    PreconditionFailed,
    IdempotencyConflict,
    Conflict
}
