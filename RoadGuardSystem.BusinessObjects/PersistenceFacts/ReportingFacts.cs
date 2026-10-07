using System.Text.Json.Serialization;
namespace RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting;

// Materialized persistence commands and snapshots, independent of public transport contracts.
public sealed record CaseDefectSnapshotV1Fact(string SchemaVersion, Guid SnapshotId, Guid ProjectId,
    DateTimeOffset CapturedAt, string Hash, CaseReadFactV1Fact[] Cases, DefectReadFactV1Fact[] Defects,
    CaseEvidenceRefV1Fact[] AuthorizedEvidence, string[] MissingReasons);

public sealed record CaseEvidenceRefV1Fact(Guid CaseId, Guid SourceReportId, Guid EvidenceId,
    Guid FileId, string FileVersion, string Sha256, long SizeBytes, string MediaType);

public sealed record CasePublicationRefV1Fact(Guid PublicationId, string Version, Guid[] RecipientReportIds, Guid[] EvidenceIds);

public sealed record CaseReadFactV1Fact(Guid CaseId, Guid ProjectId, string Version, string Status,
    CaseReportRefV1Fact[] CurrentReports, ReportingSourceRefFact[] Conclusions, CasePublicationRefV1Fact[] Publications);

public sealed record CaseReportRefV1Fact(Guid ReportId, string Version, DateTimeOffset ReceivedAt);

public sealed record DefectReadFactV1Fact(Guid DefectId, Guid ProjectId, string Version, string Status,
    string SourceKind, Guid SourceId, string SourceVersion, Guid? RouteVersionId, Guid? SegmentSetId,
    Guid? SegmentId, string? GeometryVersion, string PositionStatus);

public sealed record ProjectSummaryV1Fact(string SchemaVersion, Guid ProjectId, DateTimeOffset ReadAt, string DefinitionVersion,
    ReportingFiltersFact Filters, ReportingMetricFact[] Metrics, string[] Warnings, string Isolation);

public sealed record ReportingAvailabilityFact(string Availability, string[] ReasonCodes);

public sealed record ReportingCaptureFact(ProjectSummaryV1Fact Summary, ReportingItemFact[] Items, ReportingFileFact[] Files,
    ReportingTimelineItemFact[] Timeline, ReportingAvailabilityFact TimelineAvailability,
    CaseDefectSnapshotV1Fact? CaseDefectFacts = null);

public sealed record ReportingDimensionsFact(string? Status = null, string? Band = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null);

public sealed record ReportingFileFact(Guid FileId, string Version, string Sha256, long SizeBytes, string MediaType);

public sealed record ReportingFiltersFact(DateTimeOffset? From = null, DateTimeOffset? To = null,
    Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid[]? SegmentIds = null);

public sealed record ReportingItemFact(Guid Id, string Metric, string Type, string Version, string? Status = null,
    Guid? ParentTaskId = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid? SegmentId = null, string? Band = null,
    Guid? DatasetId = null, Guid? AssessmentId = null, long? SizeBytes = null, string? Unit = null, decimal? Bias = null,
    decimal? Mae = null, decimal? Rmse = null, int? Used = null, int? Excluded = null, Guid? ModelVersionId = null, string? SplitId = null, string? MeasurementType = null);

public sealed record ReportingMetricFact(string Code, ReportingDimensionsFact Dimensions, decimal? Value, string Unit,
    long? Numerator, long? Denominator, bool PeriodApplicable, string Availability, string[] ReasonCodes, ReportingSourceRefFact[] SourceRefs);

public sealed record ReportingSourceRefFact(string Type, Guid Id, string Version);

public sealed record ReportingTimelineItemFact(Guid EventId, DateTimeOffset OccurredAt, string? ActorDisplay, string Action,
    ReportingSourceRefFact Source, string Summary);
