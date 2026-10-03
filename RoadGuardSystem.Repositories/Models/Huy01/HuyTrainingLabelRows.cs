namespace RoadGuardSystem.Repositories.Models.Huy01;

public sealed class HuyTrainingLabelHead
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string SourceKind { get; set; } = "";
    public Guid SourceId { get; set; }
    public Guid? ReportSourceId { get; set; }
    public Guid? AIDetectionSourceId { get; set; }
    public int CurrentRevision { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HuyTrainingLabelRevision
{
    public Guid Id { get; set; }
    public Guid LabelId { get; set; }
    public int Revision { get; set; }
    public string SourceVersion { get; set; } = "";
    public Guid FileId { get; set; }
    public string FileVersion { get; set; } = "";
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string DefectTypeCode { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class HuyTrainingLabelReview
{
    public Guid Id { get; set; }
    public Guid LabelId { get; set; }
    public Guid RevisionId { get; set; }
    public string Decision { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid ActorUserId { get; set; }
    public DateTimeOffset ReviewedAt { get; set; }
}
