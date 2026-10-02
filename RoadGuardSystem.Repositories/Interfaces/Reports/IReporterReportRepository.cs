using RoadGuardSystem.BusinessObjects.Reports;

namespace RoadGuardSystem.Repositories.Reports;

public sealed record ReporterReportWriteResult(Report Report, string Version);

public interface IReporterReportRepository
{
    Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
        IReadOnlyList<VerifiedEvidenceReference> evidence, CancellationToken cancellationToken = default);
}
