using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Identity;

public sealed record IdentityV2Actor(
    Guid Id,
    string DisplayName,
    UserRoleCode RoleCode,
    byte[] RowVersion);
