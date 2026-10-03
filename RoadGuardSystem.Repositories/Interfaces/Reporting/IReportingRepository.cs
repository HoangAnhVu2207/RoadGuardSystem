using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.Repositories.Reporting;

public sealed record ReportingTaskFact(Guid Id, string Status, Guid? ParentTaskId, string Version, bool Legacy);
public sealed record ReportingSegmentFact(Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId);
public sealed record ReportingPublishedSetFact(Guid RouteVersionId, Guid SegmentSetId);
public sealed record ReportingBaselineFact(Guid Id, Guid RouteVersionId, Guid SegmentSetId, Guid SegmentId, string Band, Guid DatasetId, Guid AssessmentId);
public sealed record ReportingReportFact(Guid Id, string Version, Guid CaseId, string CaseVersion);
public sealed record ReportingCaseFact(Guid Id, string Version, string Status);
// Materialized current SQL intake facts, not a Huy lifecycle/defect reader.
public sealed record ReportingIntakeFacts(ReportingReportFact[] Reports, ReportingCaseFact[] Cases);
public sealed record ReportingFacts(ReportingTaskFact[] Tasks, ReportingSegmentFact[] Segments, ReportingBaselineFact[] Baselines,
    ReportingFileDto[] Files, ReportingItemDto[] Validations, ReportingTimelineItemDto[] Timeline, ReportingSourceRefDto[] Sources,
    string[] Warnings, ReportingPublishedSetFact[] PublishedSets, string Isolation = "SERIALIZABLE", ReportingIntakeFacts? Intake = null);
public sealed record ReportingReadResult(string Code, ReportingFacts? Facts = null);

public interface IReportingRepository
{
    // Reuses an existing SERIALIZABLE/SNAPSHOT admission transaction on this scoped DbContext.
    // Otherwise owns a SERIALIZABLE read transaction, including authorization performed by the callback.
    Task<T> ReadConsistentlyAsync<T>(Func<CancellationToken, Task<T>> read, CancellationToken token);
    Task<ReportingReadResult> CaptureAsync(Guid project, ReportingFiltersDto filters, CancellationToken token);
    Task<bool> AggregateBelongsAsync(Guid project, string type, Guid id, CancellationToken token);
    Task<ReportingTimelineItemDto[]> TimelineAsync(string type, Guid id, ReportingFiltersDto filters,
        DateTimeOffset? afterTime, Guid? afterId, int take, CancellationToken token);
}
