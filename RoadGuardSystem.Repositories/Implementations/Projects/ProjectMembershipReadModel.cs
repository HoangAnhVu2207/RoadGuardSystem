using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record EffectiveProjectMembership(
    Guid MembershipId,
    Guid ProjectId,
    Guid UserId,
    UserRoleCode RoleCode,
    ProjectMemberStatus Status,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

/// <summary>
/// Reads the authoritative active and effective project membership state for server-side authorization.
/// API policy and Supervisor bypass decisions remain in the application layer.
/// </summary>
public sealed class ProjectMembershipReadModel : IProjectMembershipRepository
{
    private readonly RoadGuardDbContext _context;

    public ProjectMembershipReadModel(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<EffectiveProjectMembership?> FindByUserAndProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || projectId == Guid.Empty)
        {
            return null;
        }

        var memberships = await (
                from member in _context.ProjectMembers.AsNoTracking()
                where member.UserId == userId && member.ProjectId == projectId
                orderby member.Id
                select new EffectiveProjectMembership(
                    member.Id,
                    member.ProjectId,
                    member.UserId,
                    member.RoleCode,
                    member.Status,
                    member.ValidFrom,
                    member.ValidTo))
            .ToListAsync(cancellationToken);

        return memberships.Count == 1 ? memberships[0] : null;
    }

    public async Task<EffectiveProjectMembership?> FindActiveEffectiveAsync(
        Guid userId,
        Guid projectId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        var membership = await FindByUserAndProjectAsync(userId, projectId, cancellationToken);
        return membership is not null && membership.Status == ProjectMemberStatus.Active &&
               membership.ValidFrom <= effectiveOn &&
               (membership.ValidTo is null || membership.ValidTo >= effectiveOn)
            ? membership
            : null;
    }
}
