using RoadGuardSystem.BusinessObjects.Candidates;

namespace RoadGuardSystem.Repositories.Integration;

public sealed record ReporterFileFacts(Guid FileId, Guid OwnerId, Guid? ProjectId, string Purpose, string State,
    string FileVersion, string Checksum, long SizeBytes, string MediaType, DateTimeOffset UploadedAt);
public sealed record PublicationFileFacts(Guid FileId, string FileVersion, Guid OwnerUserId);
public sealed record ReportSourceEvidence(Guid EvidenceId, Guid FileId, string FileVersion);
public sealed record ReportCandidateFacts(Guid ReportId, Guid OwnerId, Guid CaseId, Guid? ProjectId,
    Guid? RouteVersionId, Guid? SegmentSetId, string ReportVersion, string CaseVersion,
    ActiveCandidateDisposition? Disposition, string? DispositionHeadVersion, ReportSourceEvidence[] Evidence);
public interface IAnhHuyFactsRepository
{
    Task<bool> IsCurrentActorAsync(Guid actorId, RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role, CancellationToken ct);
    Task<ReporterFileFacts?> GetFileAsync(Guid fileId, CancellationToken ct);
    Task<PublicationFileFacts?> GetPublicationFileAsync(Guid actorId, Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken ct);
    Task<ReportCandidateFacts?> GetReportSourceAsync(Guid reportId, CancellationToken ct);
}
