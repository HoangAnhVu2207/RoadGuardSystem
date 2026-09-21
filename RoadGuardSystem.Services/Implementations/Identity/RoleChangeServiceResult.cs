using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Identity;

public sealed record RoleChangeServiceResult(
    RoleChangeServiceStatus Status,
    UserRoleCode RoleCode,
    string? Message = null);
