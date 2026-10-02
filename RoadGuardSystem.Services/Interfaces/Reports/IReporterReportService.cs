using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Reports;

namespace RoadGuardSystem.Services.Reports;

public enum ReporterReportCommandStatus { Created, Replayed, Forbidden, NotFound, InvalidInput, SourceNotReady, IdempotencyConflict }

public sealed record ReporterReportCommandResult(ReporterReportCommandStatus Status, ReporterReportResponseDto? Report = null);

public interface IReporterReportService
{
    Task<ReporterReportCommandResult> CreateAsync(Guid actorUserId, UserRoleCode role,
        CreateReporterReportRequestDto request, string idempotencyKey, CancellationToken cancellationToken = default);
}
