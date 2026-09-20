using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public sealed record EffectiveProjectMembership(
    Guid MembershipId,
    Guid ProjectId,
    Guid UserId,
    UserRoleCode RoleCode,
    DateOnly ValidFrom,
    DateOnly? ValidTo);

/// <summary>
/// Reads the authoritative active and effective project membership state for server-side authorization.
/// API policy and Supervisor bypass decisions remain in the application layer.
/// </summary>
public sealed class ProjectMembershipReadModel
{
    private readonly RoadGuardDbContext _context;

    public ProjectMembershipReadModel(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<EffectiveProjectMembership?> FindActiveEffectiveAsync(
        Guid userId,
        Guid projectId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || projectId == Guid.Empty)
        {
            return null;
        }

        var memberships = await (
                from member in _context.ProjectMembers.AsNoTracking()
                join user in _context.Users.AsNoTracking() on member.UserId equals user.Id
                where member.UserId == userId &&
                      member.ProjectId == projectId &&
                      member.Status == ProjectMemberStatus.Active &&
                      member.ValidFrom <= effectiveOn &&
                      (member.ValidTo == null || member.ValidTo >= effectiveOn) &&
                      (user.RoleCode == UserRoleCode.Supervisor || member.RoleCode == user.RoleCode)
                orderby member.Id
                select new EffectiveProjectMembership(
                    member.Id,
                    member.ProjectId,
                    member.UserId,
                    member.RoleCode,
                    member.ValidFrom,
                    member.ValidTo))
            .Take(2)
            .ToListAsync(cancellationToken);

        return memberships.Count == 1 ? memberships[0] : null;
    }
}
