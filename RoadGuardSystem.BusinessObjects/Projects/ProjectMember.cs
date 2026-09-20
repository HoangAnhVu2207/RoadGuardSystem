using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class ProjectMember : IHasRowVersion
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public UserRoleCode RoleCode { get; set; }

    public bool IsPrimary { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public ProjectMemberStatus Status { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public static ProjectMember CreatePrimaryProjectManager(
        Guid id,
        Guid projectId,
        Guid userId,
        DateOnly validFrom)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentException("Membership, project and user ids must not be empty.");
        }

        if (validFrom == default)
        {
            throw new ArgumentException("Membership valid-from date is required.", nameof(validFrom));
        }

        return new ProjectMember
        {
            Id = id,
            ProjectId = projectId,
            UserId = userId,
            RoleCode = UserRoleCode.ProjectManager,
            IsPrimary = true,
            ValidFrom = validFrom,
            Status = ProjectMemberStatus.Active
        };
    }
}
