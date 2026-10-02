using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Authorization;

public sealed class ProjectScopeGuard : IProjectScopeGuard
{
    private readonly IProjectMembershipRepository _membershipRepository;
    private readonly TimeProvider _timeProvider;

    public ProjectScopeGuard(
        IProjectMembershipRepository membershipRepository,
        TimeProvider timeProvider)
    {
        _membershipRepository = membershipRepository;
        _timeProvider = timeProvider;
    }

    public async Task<ProjectAccessScope?> AuthorizeAsync(
        Guid userId,
        UserRoleCode authoritativeRole,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || projectId == Guid.Empty || authoritativeRole == UserRoleCode.Unknown)
        {
            return null;
        }

        if (authoritativeRole == UserRoleCode.Supervisor)
        {
            return new ProjectAccessScope(projectId, authoritativeRole, null);
        }

        var membership = await _membershipRepository.FindByUserAndProjectAsync(
            userId,
            projectId,
            cancellationToken);
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return membership is not null && membership.Status == ProjectMemberStatus.Active &&
               membership.RoleCode == authoritativeRole &&
               membership.ValidFrom <= today && (membership.ValidTo is null || membership.ValidTo >= today)
            ? new ProjectAccessScope(projectId, authoritativeRole, membership.MembershipId)
            : null;
    }
}
