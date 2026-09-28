namespace RoadGuardSystem.Repositories.Identity;

public sealed record V2AccountUpdatePersistenceResult(
    V2AccountUpdatePersistenceStatus Status,
    UserProfileState? Profile = null);
