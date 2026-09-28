namespace RoadGuardSystem.Services.Authentication;

public enum IdentityOnboardingStatus
{
    Success,
    IdempotentReplay,
    InvalidInput,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,
    IdempotencyConflict,
    DeliveryUnavailable
}
