using RoadGuardSystem.BusinessObjects.Reports;

namespace RoadGuardSystem.Repositories.Reports;

public sealed record ReporterReportWriteResult(Report Report, string Version);
public enum ReporterIntakeFactsStatus { Ready, Forbidden, NotFound, SourceNotReady, StaleFile }
public sealed class ReporterIntakeFactsException(ReporterIntakeFactsStatus status) : Exception { public ReporterIntakeFactsStatus Status { get; } = status; }

public interface IReporterReportRepository
{
    Task<ReporterReportWriteResult> CreateAndSaveAsync(Guid reporterUserId, string description,
        IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken cancellationToken = default);
}
