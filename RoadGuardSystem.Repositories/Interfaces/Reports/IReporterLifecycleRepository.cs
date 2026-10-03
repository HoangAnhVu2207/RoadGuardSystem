using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Reports;

namespace RoadGuardSystem.Repositories.Reports;

public interface IReporterLifecycleRepository
{
    Task<OwnReportDto> ReadAsync(Guid actorId, Guid reportId, CancellationToken ct);
    Task<OwnReportPageDto> ListAsync(Guid actorId, int pageSize, string? cursor, CancellationToken ct);
    Task GuardAsync(Guid actorId, Guid reportId, IReadOnlyList<VerifiedEvidenceReference> evidence, CancellationToken ct);
    Task<OwnReportDto> SupplementAsync(Guid actorId, Guid reportId, string expectedVersion, string description,
        IReadOnlyList<VerifiedEvidenceReference> evidence, Guid? correlationId, CancellationToken ct);
    Task<(Guid FileId, Guid? PublicationId, string? FileVersion)> ResolveDownloadAsync(Guid actorId, Guid reportId, Guid evidenceId, CancellationToken ct);
}
