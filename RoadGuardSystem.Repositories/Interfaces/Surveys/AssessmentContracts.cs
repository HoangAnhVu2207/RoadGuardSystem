namespace RoadGuardSystem.Repositories.Surveys;

public sealed record AssessmentEvidence(Guid FileId, long? FromMilliseconds, long? ToMilliseconds);
public sealed record AssessmentItem(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string TargetBand,
    string PositionStatus, string QualityStatus, string CoverageStatus, string Reason, IReadOnlyList<AssessmentEvidence> Evidence);
public sealed record AssessmentRecord(Guid Id, Guid DatasetId, string MethodVersion, Guid ReviewedBy, DateTimeOffset ReviewedAt,
    IReadOnlyList<AssessmentItem> Items, string Version);
public sealed record BaselineItem(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string TargetBand,
    Guid DatasetId, Guid AssessmentId, Guid? ExpectedBaselineSelectionId);
public sealed record SelectedBaselineItem(Guid SelectionId, Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId,
    string TargetBand, Guid DatasetId, Guid AssessmentId);
public sealed record BaselineRecord(Guid Id, IReadOnlyList<SelectedBaselineItem> Items, Guid SelectedBy, DateTimeOffset SelectedAt, string Version);
public sealed record AssessmentOutcome(string Code, AssessmentRecord? Assessment = null, BaselineRecord? Baseline = null);
