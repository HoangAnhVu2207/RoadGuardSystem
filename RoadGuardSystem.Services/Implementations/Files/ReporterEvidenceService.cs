using System.Security.Cryptography;
using RoadGuardSystem.Services.Integration;
using System.Text;
using Microsoft.Extensions.Options;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Implementations.Files;

public sealed class ReporterEvidenceService : IReporterEvidenceService
{
    private readonly IUploadRepository _repository;
    private readonly IAnhHuyProducerService _producer;
    private readonly IReporterEvidenceRepository _reporter;
    private readonly TimeProvider _timeProvider;
    private readonly UploadSessionOptions _options;

    public ReporterEvidenceService(
        IUploadRepository repository,
        IAnhHuyProducerService producer,
        IReporterEvidenceRepository reporter,
        TimeProvider timeProvider,
        IOptions<UploadSessionOptions> options)
    {
        _repository = repository;
        _producer = producer;
        _reporter = reporter;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<UploadServiceResult> CreateAsync(
        Guid actorUserId,
        UserRoleCode role,
        ReporterEvidenceCreateRequestDto request,
        string idempotencyKey,
        Guid? correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedRole(role)) return new(UploadServiceStatus.Forbidden);
        if (!IsValidCreate(actorUserId, request, idempotencyKey) || !ValidOptions())
        {
            return new(UploadServiceStatus.InvalidInput);
        }

        if (!await _reporter.IsActiveReporterAsync(actorUserId, cancellationToken)) return new(UploadServiceStatus.Forbidden);

        var maximum = UploadAdmissionPolicy.MaximumBytes("REPORT_PHOTO", request.MediaType);
        if (maximum is null || request.SizeBytes > maximum)
            return new(UploadServiceStatus.InvalidInput, MaxBytes: maximum, ActualBytes: request.SizeBytes);

        var now = _timeProvider.GetUtcNow();
        var normalized = Normalize(request);
        var result = await _repository.CreateAsync(new UploadCreatePersistenceRequest(
            actorUserId,
            null,
            null,
            "REPORT_PHOTO",
            normalized.FileName,
            normalized.MediaType,
            normalized.SizeBytes,
            normalized.ChecksumSha256,
            _options.PartSizeBytes,
            now.AddHours(_options.SessionLifetimeHours),
            idempotencyKey.Trim(),
            Fingerprint($"reporter-private.v1|REPORT_PHOTO|{normalized.FileName}|{normalized.MediaType}|{normalized.SizeBytes}|{normalized.ChecksumSha256}"),
            correlationId), cancellationToken);
        return MapMutation(result);
    }

    public async Task<UploadServiceResult> GetSessionAsync(Guid actorUserId, UserRoleCode role, Guid uploadId, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetSessionAsync(uploadId, cancellationToken);
        if (session is null) return new(UploadServiceStatus.NotFound);
        if (!await CanAccessAsync(actorUserId, role, session.OwnerUserId, session.ProjectId, session.Purpose, session.TargetId, cancellationToken)) return new(UploadServiceStatus.NotFound);
        return new(UploadServiceStatus.Success, ToDto(session));
    }

    public async Task<UploadServiceResult> GetPartUrlsAsync(
        Guid actorUserId,
        UserRoleCode role,
        Guid uploadId,
        UploadPartUrlsRequestDto request,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || request?.PartNumbers is null) return new(UploadServiceStatus.InvalidInput);
        var session = await _repository.GetSessionAsync(uploadId, cancellationToken);
        if (session is null) return new(UploadServiceStatus.NotFound);
        if (!await CanAccessAsync(actorUserId, role, session.OwnerUserId, session.ProjectId, session.Purpose, session.TargetId, cancellationToken)) return new(UploadServiceStatus.NotFound);
        var now = _timeProvider.GetUtcNow();
        var urlExpiresAt = now.AddMinutes(_options.SignedPutUrlLifetimeMinutes);
        if (urlExpiresAt > session.ExpiresAt)
        {
            urlExpiresAt = session.ExpiresAt;
        }

        var result = await _repository.GetPartUrlsAsync(
            actorUserId,
            session.ProjectId,
            uploadId,
            request.PartNumbers,
            idempotencyKey.Trim(),
            Fingerprint($"{uploadId:N}|{string.Join(',', request.PartNumbers.OrderBy(number => number))}"),
            now,
            urlExpiresAt,
            cancellationToken);
        return result.Status switch
        {
            UploadPersistenceStatus.Success or UploadPersistenceStatus.Replayed => new(
                result.Status == UploadPersistenceStatus.Replayed ? UploadServiceStatus.Replayed : UploadServiceStatus.Success,
                PartUrls: new(result.Parts.Select(part => new UploadPartUrlResponseDto(part.PartNumber, part.Url, part.ExpiresAt)).ToArray())),
            UploadPersistenceStatus.NotFound => new(UploadServiceStatus.NotFound),
            UploadPersistenceStatus.Conflict => new(UploadServiceStatus.Conflict),
            UploadPersistenceStatus.StorageUnavailable => new(UploadServiceStatus.StorageUnavailable),
            _ => new(UploadServiceStatus.InvalidInput)
        };
    }

    public async Task<UploadServiceResult> CompleteAsync(
        Guid actorUserId,
        UserRoleCode role,
        Guid uploadId,
        UploadCompleteRequestDto request,
        string idempotencyKey,
        string expectedVersion,
        Guid? correlationId,
        CancellationToken cancellationToken = default)
    {
        if (request?.Parts is null || string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(expectedVersion)) return new(UploadServiceStatus.InvalidInput);
        var session = await _repository.GetSessionAsync(uploadId, cancellationToken);
        if (session is null) return new(UploadServiceStatus.NotFound);
        if (!await CanAccessAsync(actorUserId, role, session.OwnerUserId, session.ProjectId, session.Purpose, session.TargetId, cancellationToken)) return new(UploadServiceStatus.NotFound);
        var result = await _repository.CompleteAsync(new UploadCompletePersistenceRequest(
            actorUserId,
            session.ProjectId,
            uploadId,
            expectedVersion.Trim().Trim('"'),
            request.Parts.Select(part => new CompletedStoragePart(part.PartNumber, part.ETag)).ToArray(),
            request.ChecksumSha256,
            idempotencyKey.Trim(),
            Fingerprint($"{uploadId:N}|{expectedVersion}|{request.ChecksumSha256}|{string.Join(',', request.Parts.OrderBy(part => part.PartNumber).Select(part => $"{part.PartNumber}:{part.ETag}"))}"),
            correlationId), cancellationToken);
        return MapMutation(result);
    }

    public async Task<UploadServiceResult> GetFileMetadataAsync(Guid actorUserId, UserRoleCode role, Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _repository.GetFileMetadataAsync(fileId, cancellationToken);
        if (file is null) return new(UploadServiceStatus.NotFound);
        if (!await CanAccessAsync(actorUserId, role, file.OwnerUserId, file.ProjectId, file.Purpose, file.TargetId, cancellationToken)) return new(UploadServiceStatus.NotFound);
        return new(UploadServiceStatus.Success, File: ToDto(file));
    }

    public async Task<UploadServiceResult> DownloadPublicationAsync(Guid actorUserId, UserRoleCode role,
        Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken = default)
    {
        var resolved = await _producer.ResolvePublicationEvidenceAsync(actorUserId, role, publicationId, reportId, evidenceId, cancellationToken);
        if (resolved.Status != AnhHuyProducerStatus.Ready)
            return new(resolved.Status == AnhHuyProducerStatus.Forbidden ? UploadServiceStatus.Forbidden
                : resolved.Status == AnhHuyProducerStatus.SourceNotReady ? UploadServiceStatus.Conflict : UploadServiceStatus.NotFound);
        var file = await _repository.GetFileMetadataAsync(resolved.Facts!.Reference.FileId, cancellationToken);
        if (file is null) return new(UploadServiceStatus.NotFound);
        if (file.Status != "VERIFIED" || file.Version != resolved.Facts.Reference.FileVersion) return new(UploadServiceStatus.Conflict);
        try { return new(UploadServiceStatus.Success, File: ToDto(file), Content: await _repository.OpenFileAsync(file.ObjectKey, cancellationToken)); }
        catch (FileStorageException) { return new(UploadServiceStatus.StorageUnavailable); }
    }

    public async Task<UploadServiceResult> DownloadAsync(Guid actorUserId, UserRoleCode role, Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _repository.GetFileMetadataAsync(fileId, cancellationToken);
        if (file is null) return new(UploadServiceStatus.NotFound);
        if (!await CanAccessAsync(actorUserId, role, file.OwnerUserId, file.ProjectId, file.Purpose, file.TargetId, cancellationToken)) return new(UploadServiceStatus.NotFound);
        if (file.Status != "VERIFIED") return new(UploadServiceStatus.Conflict);
        try
        {
            return new(UploadServiceStatus.Success, File: ToDto(file), Content: await _repository.OpenFileAsync(file.ObjectKey, cancellationToken));
        }
        catch (FileStorageException)
        {
            return new(UploadServiceStatus.StorageUnavailable);
        }
    }

    private async Task<bool> CanAccessAsync(Guid actorUserId, UserRoleCode role, Guid ownerId, Guid? projectId, string? purpose, Guid? targetId, CancellationToken cancellationToken)
        => IsSupportedRole(role) && ownerId == actorUserId && projectId is null && targetId is null && purpose == "REPORT_PHOTO"
           && await _reporter.IsActiveReporterAsync(actorUserId, cancellationToken);
    private static bool IsSupportedRole(UserRoleCode role) => role == UserRoleCode.Reporter;
    private static bool IsValidCreate(Guid actorUserId, ReporterEvidenceCreateRequestDto? request, string idempotencyKey)
        => actorUserId != Guid.Empty && request is not null && request.SizeBytes > 0 && !string.IsNullOrWhiteSpace(idempotencyKey)
           && request.FileName is { Length: > 0 and <= 255 } && !string.IsNullOrWhiteSpace(request.FileName)
           && request.MediaType?.Trim().ToLowerInvariant() is "image/jpeg" or "image/png"
           && request.ChecksumSha256 is { Length: 64 } && request.ChecksumSha256.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private bool ValidOptions()
        => _options.PartSizeBytes == 8 * 1024 * 1024 && _options.SessionLifetimeHours == 24 && _options.SignedPutUrlLifetimeMinutes == 15;

    private static ReporterEvidenceCreateRequestDto Normalize(ReporterEvidenceCreateRequestDto request)
        => request with
        {
            FileName = request.FileName.Trim(),
            MediaType = request.MediaType.Trim().ToLowerInvariant(),
            ChecksumSha256 = request.ChecksumSha256.Trim()
        };

    private static UploadServiceResult MapMutation(UploadMutationPersistenceResult result)
        => result.Status switch
        {
            UploadPersistenceStatus.Success => new(UploadServiceStatus.Success, ToDto(result.Session!)),
            UploadPersistenceStatus.Replayed => new(UploadServiceStatus.Replayed, ToDto(result.Session!)),
            UploadPersistenceStatus.NotFound => new(UploadServiceStatus.NotFound),
            UploadPersistenceStatus.ConcurrencyConflict => new(UploadServiceStatus.PreconditionFailed),
            UploadPersistenceStatus.Conflict => new(UploadServiceStatus.Conflict),
            UploadPersistenceStatus.StorageUnavailable => new(UploadServiceStatus.StorageUnavailable),
            _ => new(UploadServiceStatus.InvalidInput)
        };

    private static UploadSessionResponseDto ToDto(UploadSessionPersistenceView view)
        => new(view.Id, view.FileId, view.Status, view.PartSizeBytes, view.ExpiresAt, view.Version);

    private static FileMetadataResponseDto ToDto(FileMetadataPersistenceView view)
        => new(view.Id, view.Status == "VERIFIED" ? "VERIFIED" : view.Status == "FAILED" ? "FAILED" : "PENDING", view.ChecksumSha256, view.MediaType, view.SizeBytes, view.Version);

    private static string Fingerprint(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
