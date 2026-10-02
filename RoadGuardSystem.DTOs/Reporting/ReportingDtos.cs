namespace RoadGuardSystem.DTOs.Reporting;

public sealed record ReportingFiltersDto(DateTimeOffset? From = null, DateTimeOffset? To = null,
    Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid[]? SegmentIds = null);
public sealed record ReportingDimensionsDto(string? Status = null, string? Band = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null);
public sealed record ReportingSourceRefDto(string Type, Guid Id, string Version);
public sealed record ReportingMetricDto(string Code, ReportingDimensionsDto Dimensions, decimal? Value, string Unit,
    long? Numerator, long? Denominator, bool PeriodApplicable, string Availability, string[] ReasonCodes, ReportingSourceRefDto[] SourceRefs);
public sealed record ProjectSummaryV1(string SchemaVersion, Guid ProjectId, DateTimeOffset ReadAt, string DefinitionVersion,
    ReportingFiltersDto Filters, ReportingMetricDto[] Metrics, string[] Warnings, string Isolation);
public sealed record ReportingItemDto(Guid Id, string Metric, string Type, string Version, string? Status = null,
    Guid? ParentTaskId = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid? SegmentId = null, string? Band = null,
    Guid? DatasetId = null, Guid? AssessmentId = null, long? SizeBytes = null, string? Unit = null, decimal? Bias = null,
    decimal? Mae = null, decimal? Rmse = null, int? Used = null, int? Excluded = null, Guid? ModelVersionId = null, string? SplitId = null, string? MeasurementType = null);
public sealed record ReportingFileDto(Guid FileId, string Version, string Sha256, long SizeBytes, string MediaType);
public sealed record ReportingTimelineItemDto(Guid EventId, DateTimeOffset OccurredAt, string? ActorDisplay, string Action,
    ReportingSourceRefDto Source, string Summary);
public sealed record ReportingAvailabilityDto(string Availability, string[] ReasonCodes);
public sealed record ReportingItemsPageDto(string SchemaVersion, string Metric, DateTimeOffset ReadAt, ReportingItemDto[] Items,
    string? NextCursor, ReportingAvailabilityDto Availability);
public sealed record ReportingTimelinePageDto(string SchemaVersion, DateTimeOffset ReadAt, ReportingTimelineItemDto[] Items,
    string? NextCursor, ReportingAvailabilityDto Availability);
// Materialized admission facts, also consumed by the export snapshot. No transport result or query is retained.
public sealed record ReportingCaptureDto(ProjectSummaryV1 Summary, ReportingItemDto[] Items, ReportingFileDto[] Files,
    ReportingTimelineItemDto[] Timeline, ReportingAvailabilityDto TimelineAvailability);
