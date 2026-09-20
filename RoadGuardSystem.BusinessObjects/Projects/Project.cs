using RoadGuardSystem.BusinessObjects.Concurrency;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class Project : IHasRowVersion
{
    public Guid Id { get; set; }

    public string ProjectCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? EngineeringUtmSrid { get; set; }

    public ProjectStatus Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
