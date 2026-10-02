using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Services.Files;

namespace RoadGuardSystem.Services.Reports;

public sealed record ReporterLifecycleResult(int Status, string? Code = null, OwnReportDto? Report = null,
    OwnReportPageDto? Page = null, IReadOnlyDictionary<string, string[]>? Errors = null);
public interface IReporterLifecycleService
{
    Task<ReporterLifecycleResult> ReadAsync(Guid actorId, UserRoleCode role, Guid reportId, CancellationToken ct);
    Task<ReporterLifecycleResult> ListAsync(Guid actorId, UserRoleCode role, int pageSize, string? cursor, CancellationToken ct);
    Task<ReporterLifecycleResult> SupplementAsync(Guid actorId, UserRoleCode role, Guid reportId,
        CreateReporterReportRequestDto request, string key, string expectedVersion, Guid? correlationId, CancellationToken ct);
    Task<UploadServiceResult> DownloadAsync(Guid actorId, UserRoleCode role, Guid reportId, Guid evidenceId, CancellationToken ct);
}
