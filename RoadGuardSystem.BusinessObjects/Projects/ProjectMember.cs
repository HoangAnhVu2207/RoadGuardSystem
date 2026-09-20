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
}
