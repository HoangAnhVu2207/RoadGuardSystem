using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.Services.Reporting;

public sealed record ReportingResult<T>(string Code, T? Value = default);
public interface IReportingService
{
    Task<ReportingResult<ReportingCaptureDto>> CaptureAsync(Guid actor, Guid project, ReportingFiltersDto filters, CancellationToken token);
    Task<ReportingResult<ProjectSummaryV1>> SummaryAsync(Guid actor, Guid project, ReportingFiltersDto filters, CancellationToken token);
    Task<ReportingResult<ReportingItemsPageDto>> ItemsAsync(Guid actor, Guid project, ReportingFiltersDto filters, string metric, string? cursor, int pageSize, CancellationToken token);
    Task<ReportingResult<ReportingTimelinePageDto>> TimelineAsync(Guid actor, Guid project, string aggregateType, Guid aggregateId,
        ReportingFiltersDto filters, string? cursor, int pageSize, CancellationToken token);
}
