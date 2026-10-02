namespace RoadGuardSystem.DTOs.Surveys;

public sealed record AssessmentEvidenceDto(Guid FileId, long? FromMilliseconds = null, long? ToMilliseconds = null);
public sealed record AssessmentItemDto(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string TargetBand,
    string PositionStatus, string QualityStatus, string CoverageStatus, string Reason, IReadOnlyList<AssessmentEvidenceDto> Evidence);
public sealed record CreateAssessmentDto(string MethodVersion, IReadOnlyList<AssessmentItemDto> Items);
public sealed record AssessmentView(Guid Id, Guid DatasetId, string MethodVersion, Guid ReviewedBy, DateTimeOffset ReviewedAt,
    IReadOnlyList<AssessmentItemDto> Items, string Version);
public sealed record BaselineItemDto(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string TargetBand,
    Guid DatasetId, Guid AssessmentId, Guid? ExpectedBaselineSelectionId);
public sealed record CreateBaselineDto(IReadOnlyList<BaselineItemDto> Items, string Reason);
public sealed record BaselineSelectedItemDto(Guid SelectionId, Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId,
    string TargetBand, Guid DatasetId, Guid AssessmentId);
public sealed record BaselineView(Guid Id, IReadOnlyList<BaselineSelectedItemDto> Items, Guid SelectedBy, DateTimeOffset SelectedAt, string Version);
public sealed record DatasetSourceFileDto(Guid FileId, string ChecksumSha256, long SizeBytes, string MediaType, string Purpose);
public sealed record DatasetDetailView(Guid Id, Guid SurveyTaskId, Guid ProjectId, Guid DataVersionId, Guid? SubmittedBy,
    DateTimeOffset? SubmittedAt, DateTimeOffset? RecordedAt, Guid? DeviceId, IReadOnlyList<BandScopeDto> Scope,
    string IntegrityStatus, string TelemetryStatus, IReadOnlyList<DatasetSourceFileDto> SourceFiles, IReadOnlyList<DatasetPairDto> Pairs, string Version);
public sealed record SurveyTaskWorkPackage(string SchemaVersion, SurveyTaskV2ResponseDto Task, DateTimeOffset? DueAt,
    PositionDto? AccessPoint, IReadOnlyList<SurveyGeometryRefDto> GeometryRefs, string ScopeVersion);
public sealed record SurveyGeometryRefDto(Guid RouteVersionId, Guid SegmentSetId, IReadOnlyList<Guid> SegmentIds);
