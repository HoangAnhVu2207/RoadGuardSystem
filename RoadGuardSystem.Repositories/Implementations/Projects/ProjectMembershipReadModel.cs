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
                orderby member.Status descending, member.ValidFrom descending, member.Id
                select new EffectiveProjectMembership(
                    member.Id,
                    member.ProjectId,
                    member.UserId,
                    member.RoleCode,
                    member.Status,
                    member.ValidFrom,
                    member.ValidTo))
            .ToListAsync(cancellationToken);

        return memberships.FirstOrDefault();
    }

    public async Task<EffectiveProjectMembership?> FindActiveEffectiveAsync(
        Guid userId,
        Guid projectId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        return await (
                from member in _context.ProjectMembers.AsNoTracking()
                where member.UserId == userId && member.ProjectId == projectId &&
                      member.Status == ProjectMemberStatus.Active &&
                      member.ValidFrom <= effectiveOn &&
                      (member.ValidTo == null || member.ValidTo >= effectiveOn)
                orderby member.ValidFrom descending, member.Id
                select new EffectiveProjectMembership(
                    member.Id,
                    member.ProjectId,
                    member.UserId,
                    member.RoleCode,
                    member.Status,
                    member.ValidFrom,
                    member.ValidTo))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
