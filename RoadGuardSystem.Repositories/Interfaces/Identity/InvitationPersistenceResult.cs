using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed record InvitationPersistenceResult(
    IdentityOnboardingPersistenceStatus Status,
    StaffInvitation? Invitation = null,
    UserSecurityState? User = null,
    Guid? SessionId = null,
    string? DeliveryToken = null);
