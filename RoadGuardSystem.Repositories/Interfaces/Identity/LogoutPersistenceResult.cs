namespace RoadGuardSystem.Repositories.Identity;

public sealed record LogoutPersistenceResult(
    bool IdempotentReplay = false,
    bool IdempotentConflict = false);
