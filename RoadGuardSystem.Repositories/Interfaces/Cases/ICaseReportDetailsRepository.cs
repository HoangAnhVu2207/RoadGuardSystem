using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Cases;

public sealed record CaseEvidenceRow(Guid Id, Guid FileId, string FileVersion, Guid? SupplementId,
    DateTimeOffset? CapturedAt, string LocationSource, decimal? Latitude, decimal? Longitude, decimal? AccuracyMeters);
public sealed record CaseSupplementRow(Guid Id, string Description, DateTimeOffset ReceivedAt,
    IReadOnlyList<CaseEvidenceRow> Evidence);
public sealed record CaseReportRow(Guid Id, Guid ReporterUserId, string ReporterDisplayName, string? ReporterPhoneNumber,
    bool ReporterPhoneNumberConfirmed, string Description, DateTimeOffset ReceivedAt,
    IReadOnlyList<CaseEvidenceRow> OriginalEvidence, IReadOnlyList<CaseSupplementRow> Supplements);
public sealed record CaseReportRows(IReadOnlyList<CaseReportRow> Items, bool HasMore);
public sealed record CaseEvidenceContent(Stream Stream, string MediaType, string FileVersion);

public interface ICaseReportDetailsRepository
{
    Task<CaseReportRows> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, DateTimeOffset? beforeReceivedAt,
        Guid? beforeReportId, int take, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
    Task<CaseEvidenceContent> DownloadAsync(Guid actor, UserRoleCode role, Guid caseId, Guid reportId,
        Guid evidenceId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken token);
}
