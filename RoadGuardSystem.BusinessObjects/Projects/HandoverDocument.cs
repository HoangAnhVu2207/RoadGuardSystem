using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class HandoverDocument : IHasRowVersion
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;

    public DateOnly HandoverDate { get; set; }

    public Guid? AcceptedByUserId { get; set; }

    public Guid? FileId { get; set; }

    public string? Notes { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
