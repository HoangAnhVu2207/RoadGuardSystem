namespace RoadGuardSystem.DTOs.Reporting;

public sealed record ReportingFiltersDto(DateTimeOffset? From = null, DateTimeOffset? To = null,
    Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid[]? SegmentIds = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingFiltersFact?(ReportingFiltersDto? value)
        => value is null ? null! : new(value.From, value.To, value.RouteVersionId, value.SegmentSetId, value.SegmentIds);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingFiltersDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingFiltersFact? value)
        => value is null ? null! : new(value.From, value.To, value.RouteVersionId, value.SegmentSetId, value.SegmentIds);
}
public sealed record ReportingSourceRefDto(string Type, Guid Id, string Version)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingSourceRefFact?(ReportingSourceRefDto? value)
        => value is null ? null! : new(value.Type, value.Id, value.Version);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingSourceRefDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingSourceRefFact? value)
        => value is null ? null! : new(value.Type, value.Id, value.Version);
}
public sealed record ReportingMetricDto(string Code, ReportingDimensionsDto Dimensions, decimal? Value, string Unit,
    long? Numerator, long? Denominator, bool PeriodApplicable, string Availability, string[] ReasonCodes, ReportingSourceRefDto[] SourceRefs)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingMetricFact?(ReportingMetricDto? value)
        => value is null ? null! : new(value.Code, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingDimensionsFact)value.Dimensions, value.Value, value.Unit, value.Numerator, value.Denominator, value.PeriodApplicable, value.Availability, value.ReasonCodes, value.SourceRefs?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingSourceRefFact)item).ToArray()!);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingMetricDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingMetricFact? value)
        => value is null ? null! : new(value.Code, (global::RoadGuardSystem.DTOs.Reporting.ReportingDimensionsDto)value.Dimensions, value.Value, value.Unit, value.Numerator, value.Denominator, value.PeriodApplicable, value.Availability, value.ReasonCodes, value.SourceRefs?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.ReportingSourceRefDto)item).ToArray()!);
}
public sealed record ReportingDimensionsDto(string? Status = null, string? Band = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingDimensionsFact?(ReportingDimensionsDto? value)
        => value is null ? null! : new(value.Status, value.Band, value.RouteVersionId, value.SegmentSetId);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingDimensionsDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingDimensionsFact? value)
        => value is null ? null! : new(value.Status, value.Band, value.RouteVersionId, value.SegmentSetId);
}
public sealed record ReportingCaptureDto(ProjectSummaryV1 Summary, ReportingItemDto[] Items, ReportingFileDto[] Files,
    ReportingTimelineItemDto[] Timeline, ReportingAvailabilityDto TimelineAvailability,
    CaseDefectSnapshotV1? CaseDefectFacts = null,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] global::RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsSnapshot? DefectStatistics = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingCaptureFact?(ReportingCaptureDto? value)
        => value is null ? null! : new((global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ProjectSummaryV1Fact)value.Summary, value.Items?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingItemFact)item).ToArray()!, value.Files?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingFileFact)item).ToArray()!, value.Timeline?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingTimelineItemFact)item).ToArray()!, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingAvailabilityFact)value.TimelineAvailability, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.CaseDefectSnapshotV1Fact?)value.CaseDefectFacts, value.DefectStatistics);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingCaptureDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingCaptureFact? value)
        => value is null ? null! : new((global::RoadGuardSystem.DTOs.Reporting.ProjectSummaryV1)value.Summary, value.Items?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.ReportingItemDto)item).ToArray()!, value.Files?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.ReportingFileDto)item).ToArray()!, value.Timeline?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.ReportingTimelineItemDto)item).ToArray()!, (global::RoadGuardSystem.DTOs.Reporting.ReportingAvailabilityDto)value.TimelineAvailability, (global::RoadGuardSystem.DTOs.Reporting.CaseDefectSnapshotV1?)value.CaseDefectFacts, value.DefectStatistics);
}
public sealed record ProjectSummaryV1(string SchemaVersion, Guid ProjectId, DateTimeOffset ReadAt, string DefinitionVersion,
    ReportingFiltersDto Filters, ReportingMetricDto[] Metrics, string[] Warnings, string Isolation)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ProjectSummaryV1Fact?(ProjectSummaryV1? value)
        => value is null ? null! : new(value.SchemaVersion, value.ProjectId, value.ReadAt, value.DefinitionVersion, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingFiltersFact)value.Filters, value.Metrics?.Select(item => (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingMetricFact)item).ToArray()!, value.Warnings, value.Isolation);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ProjectSummaryV1?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ProjectSummaryV1Fact? value)
        => value is null ? null! : new(value.SchemaVersion, value.ProjectId, value.ReadAt, value.DefinitionVersion, (global::RoadGuardSystem.DTOs.Reporting.ReportingFiltersDto)value.Filters, value.Metrics?.Select(item => (global::RoadGuardSystem.DTOs.Reporting.ReportingMetricDto)item).ToArray()!, value.Warnings, value.Isolation);
}
public sealed record ReportingItemDto(Guid Id, string Metric, string Type, string Version, string? Status = null,
    Guid? ParentTaskId = null, Guid? RouteVersionId = null, Guid? SegmentSetId = null, Guid? SegmentId = null, string? Band = null,
    Guid? DatasetId = null, Guid? AssessmentId = null, long? SizeBytes = null, string? Unit = null, decimal? Bias = null,
    decimal? Mae = null, decimal? Rmse = null, int? Used = null, int? Excluded = null, Guid? ModelVersionId = null, string? SplitId = null, string? MeasurementType = null)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingItemFact?(ReportingItemDto? value)
        => value is null ? null! : new(value.Id, value.Metric, value.Type, value.Version, value.Status, value.ParentTaskId, value.RouteVersionId, value.SegmentSetId, value.SegmentId, value.Band, value.DatasetId, value.AssessmentId, value.SizeBytes, value.Unit, value.Bias, value.Mae, value.Rmse, value.Used, value.Excluded, value.ModelVersionId, value.SplitId, value.MeasurementType);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingItemDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingItemFact? value)
        => value is null ? null! : new(value.Id, value.Metric, value.Type, value.Version, value.Status, value.ParentTaskId, value.RouteVersionId, value.SegmentSetId, value.SegmentId, value.Band, value.DatasetId, value.AssessmentId, value.SizeBytes, value.Unit, value.Bias, value.Mae, value.Rmse, value.Used, value.Excluded, value.ModelVersionId, value.SplitId, value.MeasurementType);
}
public sealed record ReportingFileDto(Guid FileId, string Version, string Sha256, long SizeBytes, string MediaType)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingFileFact?(ReportingFileDto? value)
        => value is null ? null! : new(value.FileId, value.Version, value.Sha256, value.SizeBytes, value.MediaType);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingFileDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingFileFact? value)
        => value is null ? null! : new(value.FileId, value.Version, value.Sha256, value.SizeBytes, value.MediaType);
}
public sealed record ReportingTimelineItemDto(Guid EventId, DateTimeOffset OccurredAt, string? ActorDisplay, string Action,
    ReportingSourceRefDto Source, string Summary)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingTimelineItemFact?(ReportingTimelineItemDto? value)
        => value is null ? null! : new(value.EventId, value.OccurredAt, value.ActorDisplay, value.Action, (global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingSourceRefFact)value.Source, value.Summary);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingTimelineItemDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingTimelineItemFact? value)
        => value is null ? null! : new(value.EventId, value.OccurredAt, value.ActorDisplay, value.Action, (global::RoadGuardSystem.DTOs.Reporting.ReportingSourceRefDto)value.Source, value.Summary);
}
public sealed record ReportingAvailabilityDto(string Availability, string[] ReasonCodes)
{
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingAvailabilityFact?(ReportingAvailabilityDto? value)
        => value is null ? null! : new(value.Availability, value.ReasonCodes);
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(value))]
    public static implicit operator ReportingAvailabilityDto?(global::RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting.ReportingAvailabilityFact? value)
        => value is null ? null! : new(value.Availability, value.ReasonCodes);
}
public sealed record ReportingItemsPageDto(string SchemaVersion, string Metric, DateTimeOffset ReadAt, ReportingItemDto[] Items,
    string? NextCursor, ReportingAvailabilityDto Availability);
public sealed record ReportingTimelinePageDto(string SchemaVersion, DateTimeOffset ReadAt, ReportingTimelineItemDto[] Items,
    string? NextCursor, ReportingAvailabilityDto Availability);
