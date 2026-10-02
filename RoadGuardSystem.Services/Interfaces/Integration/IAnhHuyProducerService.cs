using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Projects;

namespace RoadGuardSystem.Services.Integration;

public enum AnhHuyProducerStatus { Ready, NotFound, Forbidden, SourceNotReady, StaleFile, StaleGeometry, StaleSource, StaleDisposition }
public sealed record AnhHuyProducerResult<T>(AnhHuyProducerStatus Status, T? Facts = default);
public sealed record ResolvedEvidenceFacts(VerifiedEvidenceReference Reference, Guid? ProjectId,
    string Purpose, string ChecksumSha256, long SizeBytes, string MediaType, DateTimeOffset UploadedAt);
public sealed record GeometrySegmentContext(Guid Id, int Sequence, Guid? PreviousId, Guid? NextId);
public sealed record ProjectGeometryContext(string SchemaVersion, Guid ProjectId, Guid RouteVersionId,
    Guid SegmentSetId, string Version, GeometryPackageView Package, GeometrySegmentContext[] Segments);
public sealed record ResolvedCandidateSourceFacts(CandidateSourceFacts DomainFacts, Guid CaseId,
    Guid[] EvidenceIds, ResolvedEvidenceFacts[] Evidence, ProjectGeometryContext Geometry);

// Anh owns these real read producers. Huy must re-resolve them inside its command
// transaction/concurrency boundary; a successful preflight read is not a commit lock.
public interface IAnhHuyProducerService
{
    Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePrivateEvidenceAsync(Guid actorId, UserRoleCode role,
        Guid fileId, Guid evidenceId, string? expectedFileVersion = null, CancellationToken cancellationToken = default);
    Task<AnhHuyProducerResult<ResolvedEvidenceFacts>> ResolvePublicationEvidenceAsync(Guid actorId, UserRoleCode role,
        Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken = default);
    Task<AnhHuyProducerResult<ProjectGeometryContext>> ResolveGeometryAsync(Guid actorId, UserRoleCode role,
        Guid projectId, Guid routeVersionId, Guid segmentSetId, string? expectedVersion = null,
        bool requireCurrent = true, CancellationToken cancellationToken = default);
    Task<AnhHuyProducerResult<ResolvedCandidateSourceFacts>> ResolveCandidateSourceAsync(Guid actorId, UserRoleCode role,
        Guid projectId, CandidateSourceKind kind, Guid sourceId, string? expectedSourceVersion = null,
        string? expectedGeometryVersion = null, string? expectedDispositionVersion = null,
        CancellationToken cancellationToken = default);
}
