using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed record ReporterRegistrationPersistenceResult(
    IdentityOnboardingPersistenceStatus Status,
    ReporterRegistrationIntent? Intent = null,
    UserSecurityState? User = null,
    Guid? SessionId = null);
