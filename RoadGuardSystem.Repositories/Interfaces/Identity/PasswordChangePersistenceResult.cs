namespace RoadGuardSystem.Repositories.Identity;

public sealed record PasswordChangePersistenceResult(
    bool Succeeded = false,
    bool IdempotentReplay = false,
    bool IdempotentConflict = false,
    bool UserNotFound = false,
    bool StaleConcurrency = false);
