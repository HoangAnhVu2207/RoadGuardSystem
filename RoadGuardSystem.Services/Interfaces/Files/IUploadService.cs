using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Files;

public interface IUploadService
{
    Task<UploadServiceResult> CreateAsync(Guid actorUserId, UserRoleCode role, UploadCreateRequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<UploadServiceResult> GetSessionAsync(Guid actorUserId, UserRoleCode role, Guid uploadId, CancellationToken cancellationToken = default);
    Task<UploadServiceResult> GetPartUrlsAsync(Guid actorUserId, UserRoleCode role, Guid uploadId, UploadPartUrlsRequestDto request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<UploadServiceResult> CompleteAsync(Guid actorUserId, UserRoleCode role, Guid uploadId, UploadCompleteRequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<UploadServiceResult> GetFileMetadataAsync(Guid actorUserId, UserRoleCode role, Guid fileId, CancellationToken cancellationToken = default);
    Task<UploadServiceResult> DownloadAsync(Guid actorUserId, UserRoleCode role, Guid fileId, CancellationToken cancellationToken = default);
    Task ProcessOneVerificationAsync(CancellationToken cancellationToken = default);
}
