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

public sealed class ProjectScopeGuard : IProjectScopeGuard
{
    private readonly ProjectMembershipReadModel _membershipReadModel;
    private readonly TimeProvider _timeProvider;

    public ProjectScopeGuard(
        ProjectMembershipReadModel membershipReadModel,
        TimeProvider timeProvider)
    {
        _membershipReadModel = membershipReadModel;
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

        var membership = await _membershipReadModel.FindActiveEffectiveAsync(
            userId,
            projectId,
            DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);
        return membership is not null && membership.RoleCode == authoritativeRole
            ? new ProjectAccessScope(projectId, authoritativeRole, membership.MembershipId)
            : null;
    }
}
