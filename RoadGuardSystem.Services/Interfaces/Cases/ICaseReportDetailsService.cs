using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Repositories.Cases;

namespace RoadGuardSystem.Services.Cases;

public sealed record CaseReportDetailResult(int Status, string? Code = null, CaseReportDetailPageDto? Page = null,
    CaseEvidenceContent? Content = null);

public interface ICaseReportDetailsService
{
    Task<CaseReportDetailResult> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, int limit, string? cursor,
        CancellationToken token);
    Task<CaseReportDetailResult> DownloadAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId,
        Guid evidenceId, CancellationToken token);
}
