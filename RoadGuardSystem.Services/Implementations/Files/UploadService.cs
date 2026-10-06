using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Implementations.Files;

public sealed class UploadService : IUploadService
{
    private static readonly HashSet<string> Purposes = new(StringComparer.Ordinal)
    {
        "REPORT_PHOTO", "BEFORE", "AFTER", "MEASUREMENT", "SURVEY_VIDEO", "TELEMETRY", "ROUTE_SOURCE", "DOCUMENT"
    };

    private readonly IUploadRepository _repository;
    private readonly IProjectScopeGuard _scopeGuard;
    private readonly TimeProvider _timeProvider;
    private readonly UploadSessionOptions _options;

    public UploadService(
        IUploadRepository repository,
        IProjectScopeGuard scopeGuard,
        TimeProvider timeProvider,
        IOptions<UploadSessionOptions> options)
    {
        _repository = repository;
        _scopeGuard = scopeGuard;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<UploadServiceResult> CreateAsync(
        Guid actorUserId,
        UserRoleCode role,
        UploadCreateRequestDto request,
        string idempotencyKey,
        Guid? correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedRole(role) || !IsValidCreate(actorUserId, request, idempotencyKey) || !ValidOptions())
        {
            return new(UploadServiceStatus.InvalidInput);
        }

        if (!await InProjectScopeAsync(actorUserId, role, request.ProjectId!.Value, cancellationToken))
        {
            return new(UploadServiceStatus.Forbidden);
        }

        if (IsFieldPurpose(request.Purpose) && (request.TargetId is not Guid fieldTask ||
            !await _repository.IsCurrentFieldActorAsync(actorUserId,role,request.ProjectId.Value,fieldTask,request.Purpose.Trim().ToUpperInvariant(),true,cancellationToken)))
            return new(UploadServiceStatus.Forbidden);

        if (IsSurveyPurpose(request.Purpose) && (role != UserRoleCode.DroneOperator || request.TargetId is not { } taskId ||
            !await _repository.IsCurrentSurveyOperatorAsync(actorUserId, request.ProjectId.Value, taskId, true, cancellationToken)))
            return new(UploadServiceStatus.Forbidden);

        var maximum = UploadAdmissionPolicy.MaximumBytes(request.Purpose, request.MediaType);
        if (maximum is null || request.SizeBytes > maximum)
            return new(UploadServiceStatus.InvalidInput, MaxBytes: maximum, ActualBytes: request.SizeBytes);

        var now = _timeProvider.GetUtcNow();
        var normalized = Normalize(request);
        var result = await _repository.CreateAsync(new UploadCreatePersistenceRequest(
            actorUserId,
            normalized.ProjectId!.Value,
            normalized.TargetId,
            normalized.Purpose,
            normalized.FileName,
            normalized.MediaType,
            normalized.SizeBytes,
            normalized.ChecksumSha256,
            _options.PartSizeBytes,
            now.AddHours(_options.SessionLifetimeHours),
            idempotencyKey.Trim(),
            Fingerprint($"{normalized.Purpose}|{normalized.ProjectId}|{normalized.TargetId}|{normalized.FileName}|{normalized.MediaType}|{normalized.SizeBytes}|{normalized.ChecksumSha256}"),
            correlationId), cancellationToken);
        return MapMutation(result);
    }

    public async Task<UploadServiceResult> GetSessionAsync(Guid actorUserId, UserRoleCode role, Guid uploadId, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetSessionAsync(uploadId, cancellationToken);
        if (session is null) return new(UploadServiceStatus.NotFound);
        if (!await CanAccessAssetAsync(actorUserId, role, session.ProjectId, session.Purpose, session.TargetId, false, cancellationToken)) return new(UploadServiceStatus.Forbidden);
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
        if (!await CanAccessAssetAsync(actorUserId, role, session.ProjectId, session.Purpose, session.TargetId, true, cancellationToken)) return new(UploadServiceStatus.Forbidden);
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
        if (!await CanAccessAssetAsync(actorUserId, role, session.ProjectId, session.Purpose, session.TargetId, true, cancellationToken,
            requireActiveTask: session.Status is not ("VERIFYING" or "VERIFIED"))) return new(UploadServiceStatus.Forbidden);
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
        if (!await CanReadFileAsync(actorUserId,role,file,cancellationToken)) return new(UploadServiceStatus.Forbidden);
        return new(UploadServiceStatus.Success, File: ToDto(file));
    }

    public async Task<UploadServiceResult> DownloadAsync(Guid actorUserId, UserRoleCode role, Guid fileId, CancellationToken cancellationToken = default)
    {
        var file = await _repository.GetFileMetadataAsync(fileId, cancellationToken);
        if (file is null || file.Status != "VERIFIED") return new(UploadServiceStatus.NotFound);
        if (!await CanReadFileAsync(actorUserId,role,file,cancellationToken)) return new(UploadServiceStatus.Forbidden);
        try
        {
            var stream=await _repository.OpenFileAsync(file.ObjectKey,cancellationToken);
            if(IsFieldPurpose(file.Purpose))
            {
                try
                {
                    var fresh=await _repository.GetFileMetadataAsync(fileId,cancellationToken);
                    if(fresh is null || fresh.Status!="VERIFIED" || fresh.Version!=file.Version ||
                        !await CanReadFileAsync(actorUserId,role,fresh,cancellationToken))
                    {await stream.DisposeAsync();return new(UploadServiceStatus.Forbidden);}
                }
                catch{await stream.DisposeAsync();throw;}
            }
            return new(UploadServiceStatus.Success, File: ToDto(file), Content: stream);
        }
        catch (FileStorageException)
        {
            return new(UploadServiceStatus.StorageUnavailable);
        }
    }

    public async Task ProcessOneVerificationAsync(CancellationToken cancellationToken = default)
    {
        await _repository.VerifyNextAsync(cancellationToken);
    }

    private async Task<bool> CanReadFileAsync(Guid actor,UserRoleCode role,FileMetadataPersistenceView file,CancellationToken token)
        => await CanAccessAssetAsync(actor,role,file.ProjectId,file.Purpose,file.TargetId,false,token) ||
            (file.ProjectId is Guid project && IsFieldPurpose(file.Purpose) && await _repository.IsCurrentLegacyFieldFileReaderAsync(actor,role,project,file.Id,file.Purpose!,token));

    private async Task<bool> CanAccessAssetAsync(Guid actorUserId, UserRoleCode role, Guid? projectId, string? purpose, Guid? targetId, bool mutation, CancellationToken cancellationToken, bool requireActiveTask = true)
    {
        if (projectId is not { } scopedProjectId || !IsSupportedRole(role) || !await InProjectScopeAsync(actorUserId, role, scopedProjectId, cancellationToken)) return false;
        if (IsFieldPurpose(purpose))return targetId is Guid fieldTask &&
            await _repository.IsCurrentFieldActorAsync(actorUserId,role,scopedProjectId,fieldTask,purpose!.Trim().ToUpperInvariant(),mutation && requireActiveTask,cancellationToken);
        if (!IsSurveyPurpose(purpose)) return true;
        if (!mutation && role is UserRoleCode.Supervisor or UserRoleCode.ProjectManager) return true;
        return role == UserRoleCode.DroneOperator && targetId is { } taskId &&
            await _repository.IsCurrentSurveyOperatorAsync(actorUserId, scopedProjectId, taskId, mutation && requireActiveTask, cancellationToken);
    }

    private static bool IsFieldPurpose(string? purpose)=>purpose?.Trim().ToUpperInvariant() is "BEFORE" or "AFTER" or "MEASUREMENT";

    private static bool IsSurveyPurpose(string? purpose) => purpose?.Trim().ToUpperInvariant() is "SURVEY_VIDEO" or "TELEMETRY";

    private async Task<bool> InProjectScopeAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CancellationToken cancellationToken)
        => await _scopeGuard.AuthorizeAsync(actorUserId, role, projectId, cancellationToken) is not null;

    private static bool IsSupportedRole(UserRoleCode role)
        => role is UserRoleCode.Supervisor or UserRoleCode.ProjectManager or UserRoleCode.DroneOperator or UserRoleCode.RepairCrew;

    private static bool IsValidCreate(Guid actorUserId, UploadCreateRequestDto? request, string idempotencyKey)
        => actorUserId != Guid.Empty && request is not null && request.ProjectId is not null && request.ProjectId != Guid.Empty &&
           request.SizeBytes > 0 && !string.IsNullOrWhiteSpace(idempotencyKey) &&
           Purposes.Contains(request.Purpose?.Trim().ToUpperInvariant() ?? string.Empty) && ValidMimeType(request.MediaType) &&
           request.FileName is { Length: > 0 and <= 255 } && request.ChecksumSha256 is { Length: 64 } &&
           request.ChecksumSha256.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private bool ValidOptions()
        => _options.PartSizeBytes == 8 * 1024 * 1024 && _options.SessionLifetimeHours == 24 && _options.SignedPutUrlLifetimeMinutes == 15;

    private static bool ValidMimeType(string? value)
        => value is { Length: > 2 and <= 120 } && value.Count(character => character == '/') == 1;

    private static UploadCreateRequestDto Normalize(UploadCreateRequestDto request)
        => request with
        {
            Purpose = request.Purpose.Trim().ToUpperInvariant(),
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
