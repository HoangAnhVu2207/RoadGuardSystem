namespace RoadGuardSystem.Repositories.Identity;

public sealed record PasswordRecoveryPersistenceResult(
    Guid RequestId,
    bool TargetMatched,
    int NotificationsCreated);
