using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Authorization;

public sealed record ProjectAccessScope(Guid ProjectId, UserRoleCode AccessRole, Guid? MembershipId);

public interface IProjectScopeGuard
{
    Task<ProjectAccessScope?> AuthorizeAsync(
        Guid userId,
        UserRoleCode authoritativeRole,
        Guid projectId,
        CancellationToken cancellationToken = default);
}
