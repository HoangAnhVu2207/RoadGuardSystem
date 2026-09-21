namespace RoadGuardSystem.Services.Identity;

public enum RoleChangeServiceStatus
{
    Success,
    IdempotentReplay,
    IdempotentConflict,
    ActorNotAuthorized,
    UserNotFound,
    RoleNotFound,
    StaleConcurrency,
    InvalidInput
}
