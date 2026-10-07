namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class DatasetAssessment
{
    private DatasetAssessment() { }
    public Guid Id { get; private set; }
    public Guid DatasetId { get; private set; }
    public string MethodVersion { get; private set; } = "";
    public Guid ReviewedBy { get; private set; }
    public DateTimeOffset ReviewedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static DatasetAssessment Create(Guid id, Guid dataset, Guid reviewer, DateTimeOffset now) => new()
    { Id = id, DatasetId = dataset, ReviewedBy = reviewer, ReviewedAt = now, MethodVersion = "pm-evidence-review.v1" };
}

public sealed class DatasetAssessmentItem
{
    private DatasetAssessmentItem() { }
    public Guid Id { get; private set; }
    public Guid AssessmentId { get; private set; }
    public Guid RouteVersionId { get; private set; }
    public Guid SegmentSetId { get; private set; }
    public Guid SegmentId { get; private set; }
    public string TargetBand { get; private set; } = "";
    public string PositionStatus { get; private set; } = "";
    public string QualityStatus { get; private set; } = "";
    public string CoverageStatus { get; private set; } = "";
    public string Reason { get; private set; } = "";
    public string EvidenceJson { get; private set; } = "[]";
    public static DatasetAssessmentItem Create(Guid assessment, Guid route, Guid set, Guid segment, string band,
        string position, string quality, string coverage, string reason, string evidence) => new()
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment,
            RouteVersionId = route,
            SegmentSetId = set,
            SegmentId = segment,
            TargetBand = band,
            PositionStatus = position,
            QualityStatus = quality,
            CoverageStatus = coverage,
            Reason = reason,
            EvidenceJson = evidence
        };
}

public sealed class BaselineSelection
{
    private BaselineSelection() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid SelectedBy { get; private set; }
    public DateTimeOffset SelectedAt { get; private set; }
    public string Reason { get; private set; } = "";
    public byte[] RowVersion { get; private set; } = [];
    public static BaselineSelection Create(Guid project, Guid actor, string reason, DateTimeOffset now) => new()
    { Id = Guid.NewGuid(), ProjectId = project, SelectedBy = actor, SelectedAt = now, Reason = reason };
}

public sealed class BaselineSelectionItem
{
    private BaselineSelectionItem() { }
    public Guid Id { get; private set; }
    public Guid BaselineSelectionId { get; private set; }
    public Guid DatasetId { get; private set; }
    public Guid AssessmentId { get; private set; }
    public Guid RouteVersionId { get; private set; }
    public Guid SegmentSetId { get; private set; }
    public Guid SegmentId { get; private set; }
    public string TargetBand { get; private set; } = "";
    public static BaselineSelectionItem Create(Guid batch, Guid dataset, Guid assessment, Guid route, Guid set, Guid segment, string band) => new()
    {
        Id = Guid.NewGuid(),
        BaselineSelectionId = batch,
        DatasetId = dataset,
        AssessmentId = assessment,
        RouteVersionId = route,
        SegmentSetId = set,
        SegmentId = segment,
        TargetBand = band
    };
}

public sealed class BaselineCurrentPointer
{
    private BaselineCurrentPointer() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid RouteVersionId { get; private set; }
    public Guid SegmentSetId { get; private set; }
    public Guid SegmentId { get; private set; }
    public string TargetBand { get; private set; } = "";
    public Guid SelectionId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public void Select(Guid selection) => SelectionId = selection;
    public static BaselineCurrentPointer Create(Guid project, BaselineSelectionItem item) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = project,
        RouteVersionId = item.RouteVersionId,
        SegmentSetId = item.SegmentSetId,
        SegmentId = item.SegmentId,
        TargetBand = item.TargetBand,
        SelectionId = item.Id
    };
}
