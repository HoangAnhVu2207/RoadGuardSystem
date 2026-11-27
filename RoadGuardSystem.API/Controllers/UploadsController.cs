using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class UploadsController : ControllerBase
{
    private readonly IUploadService _service;

    public UploadsController(IUploadService service)
    {
        _service = service;
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpPost("uploads")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        UploadCreateRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (UsesWebSession && (role != UserRoleCode.Supervisor || request.ProjectId is not Guid project ||
            request.TargetId != project || !string.Equals(request.Purpose?.Trim(), "DOCUMENT", StringComparison.OrdinalIgnoreCase)))
            return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreateAsync(actorUserId, role, request, idempotencyKey, CorrelationId(), cancellationToken);
        if (result.Status == UploadServiceStatus.InvalidInput && result.MaxBytes is not null)
        {
            var error = ProblemResponse(422, ApiErrorCodes.UploadValidationFailed, "Upload validation failed");
            var problem = (ProblemDetails)error.Value!;
            problem.Detail = $"{request.Purpose} permits at most {result.MaxBytes} bytes; received {result.ActualBytes}.";
            problem.Extensions["maxBytes"] = result.MaxBytes;
            problem.Extensions["actualBytes"] = result.ActualBytes;
            return error;
        }
        return MapSession(result, StatusCodes.Status201Created, $"/api/v1/uploads/{result.Session?.Id:D}");
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpPost("projects/handover-upload-intents")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateHandoverIntent(UploadCreateRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken token)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (Request.Headers["Idempotency-Key"].Count != 1 || string.IsNullOrWhiteSpace(key))
            return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreateHandoverIntentAsync(actor, role, request, key, CorrelationId(), token);
        return MapSession(result, StatusCodes.Status201Created, $"/api/v1/uploads/{result.Session?.Id:D}");
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpGet("uploads/{uploadId:guid}")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSession(Guid uploadId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (UsesWebSession && !await _service.IsOwnProjectDocumentAsync(actorUserId, role, uploadId, cancellationToken))
            return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
        return MapSession(await _service.GetSessionAsync(actorUserId, role, uploadId, cancellationToken), StatusCodes.Status200OK, null);
    }

    [Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpPost("offline/uploads")]
    public async Task<IActionResult> CreateOffline(OfflineUploadCreateRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken token)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (Request.Headers["Idempotency-Key"].Count != 1 || string.IsNullOrWhiteSpace(idempotencyKey))
            return ProblemResponse(428, ApiErrorCodes.ValidationError, "One Idempotency-Key is required");
        return MapSession(await _service.CreateOfflineAsync(actor, role, request, idempotencyKey, CorrelationId(), token),
            StatusCodes.Status201Created, null);
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpPost("uploads/{uploadId:guid}/part-urls")]
    [ProducesResponseType<UploadPartUrlsResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartUrls(
        Guid uploadId,
        UploadPartUrlsRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (UsesWebSession && !await _service.IsOwnProjectDocumentAsync(actorUserId, role, uploadId, cancellationToken))
            return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.GetPartUrlsAsync(actorUserId, role, uploadId, request, idempotencyKey, cancellationToken);
        if (result.PartUrls is not null || result.PartUrlsFailure is not null)
        {
            var current = await RoadGuardSystem.API.Authentication.PartUrlSessionRecheck.ValidateAsync(HttpContext, actorUserId, role, cancellationToken);
            if (current != RoadGuardSystem.Services.Authentication.AuthoritativeSessionValidation.Success)
                return ProblemResponse(current == RoadGuardSystem.Services.Authentication.AuthoritativeSessionValidation.MustChangePassword ? 403 : 401,
                    current == RoadGuardSystem.Services.Authentication.AuthoritativeSessionValidation.MustChangePassword ? ApiErrorCodes.PasswordChangeRequired :
                    current == RoadGuardSystem.Services.Authentication.AuthoritativeSessionValidation.SessionRevoked ? ApiErrorCodes.SessionRevoked : ApiErrorCodes.Unauthorized,
                    "Current session required");
        }
        return MapPartUrls(result);
    }

    [Authorize]
    [HttpPost("uploads/{uploadId:guid}/cancel")]
    [ProducesResponseType<UploadCancelResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Cancel(Guid uploadId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetActor(out var actorUserId, out var role))
            return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        return await _service.CancelProjectUploadAsync(actorUserId, role, uploadId, cancellationToken) switch
        {
            UploadServiceStatus.Success or UploadServiceStatus.Replayed =>
                Ok(new UploadCancelResponseDto(uploadId, "FAILED", UploadSession.CancelledFailureCode)),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.UploadSessionNotFound, "Not found"),
            _ => ProblemResponse(409, "upload_not_cancellable", "Upload cannot be cancelled")
        };
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpPost("uploads/{uploadId:guid}/complete")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Complete(
        Guid uploadId,
        UploadCompleteRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (UsesWebSession && !await _service.IsOwnProjectDocumentAsync(actorUserId, role, uploadId, cancellationToken))
            return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return MapSession(
            await _service.CompleteAsync(actorUserId, role, uploadId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken),
            StatusCodes.Status202Accepted,
            $"/api/v1/uploads/{uploadId:D}");
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpGet("files/{fileId:guid}")]
    [ProducesResponseType<FileMetadataResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFileMetadata(Guid fileId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.GetFileMetadataAsync(actorUserId, role, fileId, UsesWebSession, cancellationToken);
        if (result.File is not null) Response.Headers.ETag = $"\"{result.File.Version}\"";
        return result.Status switch
        {
            UploadServiceStatus.Success when result.File is not null => Ok(result.File),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            _ => ProblemResponse(404, ApiErrorCodes.FileNotFound, "Not found")
        };
    }

    [Authorize]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [HttpGet("files/{fileId:guid}/content")]
    public async Task<IActionResult> Download(Guid fileId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var metadata = await _service.GetFileMetadataAsync(actorUserId, role, fileId, UsesWebSession, cancellationToken);
        if (metadata.Status == UploadServiceStatus.Forbidden) return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
        if (metadata.File is not { Status: "VERIFIED" } file) return ProblemResponse(404, ApiErrorCodes.FileNotFound, "Not found");
        Response.Headers.ETag = $"\"{file.Version}\"";
        if (file.MediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers.AcceptRanges = "bytes";
            // Only a strong current ETag is an If-Range validator. Dates/weak/mismatched
            // validators fall back to the complete representation, without using the range.
            if (Request.Headers.Range.Count > 0 && (Request.Headers.IfRange.Count == 0 ||
                Request.Headers.IfRange.Count == 1 && Request.Headers.IfRange.ToString() == Response.Headers.ETag.ToString()))
            {
                if (Request.Headers.Range.Count != 1 || !RoadGuardSystem.API.Files.VideoByteRange.TryParse(
                    Request.Headers.Range.ToString(), file.SizeBytes, out var first, out var last))
                {
                    Response.Headers.ContentRange = $"bytes */{file.SizeBytes}";
                    return StatusCode(416);
                }
                var partial = await _service.DownloadRangeAsync(actorUserId, role, fileId, first, last, file.Version, UsesWebSession, cancellationToken);
                if (partial.Status == UploadServiceStatus.Forbidden) return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
                if (partial.Content is null) return ProblemResponse(partial.Status == UploadServiceStatus.StorageUnavailable ? 503 : 404,
                    partial.Status == UploadServiceStatus.StorageUnavailable ? ApiErrorCodes.UploadStorageUnavailable : ApiErrorCodes.FileNotFound, "Video unavailable");
                await using var stream = partial.Content;
                Response.StatusCode = 206;
                Response.ContentType = file.MediaType;
                Response.ContentLength = last - first + 1;
                Response.Headers.ContentRange = $"bytes {first}-{last}/{file.SizeBytes}";
                await stream.CopyToAsync(Response.Body, cancellationToken);
                return new EmptyResult();
            }
        }
        var result = await _service.DownloadAsync(actorUserId, role, fileId, UsesWebSession, cancellationToken);
        if (result.Status == UploadServiceStatus.Success && result.Content is not null && result.File is not null)
        {
            Response.ContentLength = result.File.SizeBytes;
            return File(result.Content, result.File.MediaType);
        }
        return result.Status switch
        {
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, ApiErrorCodes.UploadStorageUnavailable, "Storage unavailable"),
            _ => ProblemResponse(404, ApiErrorCodes.FileNotFound, "Not found")
        };
    }

    private ObjectResult MapSession(UploadServiceResult result, int successStatus, string? location)
    {
        if (result.Session is not null) Response.Headers.ETag = $"\"{result.Session.Version}\"";
        return result.Status switch
        {
            UploadServiceStatus.Success or UploadServiceStatus.Replayed when result.Session is not null && successStatus == StatusCodes.Status201Created => Created(location!, result.Session),
            UploadServiceStatus.Success or UploadServiceStatus.Replayed when result.Session is not null && successStatus == StatusCodes.Status202Accepted => Accepted(location!, result.Session),
            UploadServiceStatus.Success or UploadServiceStatus.Replayed when result.Session is not null => Ok(result.Session),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.UploadSessionNotFound, "Not found"),
            UploadServiceStatus.PreconditionFailed => ProblemResponse(412, ApiErrorCodes.ConcurrencyConflict, "Precondition failed"),
            UploadServiceStatus.Conflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Conflict"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, ApiErrorCodes.UploadStorageUnavailable, "Storage unavailable"),
            _ => ProblemResponse(422, ApiErrorCodes.UploadValidationFailed, "Upload validation failed")
        };
    }

    private ObjectResult MapPartUrls(UploadServiceResult result)
        => result.Status switch
        {
            UploadServiceStatus.Success or UploadServiceStatus.Replayed when result.PartUrls is not null => Ok(result.PartUrls),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.UploadSessionNotFound, "Not found"),
            UploadServiceStatus.PartUrlsExpired => PartUrlsProblem(ApiErrorCodes.UploadPartUrlsExpired, result),
            UploadServiceStatus.PartUrlsIdempotencyConflict => ProblemResponse(409, ApiErrorCodes.IdempotencyKeyReused, "Idempotency key reused"),
            UploadServiceStatus.PartUrlsSessionUnavailable => PartUrlsProblem(ApiErrorCodes.UploadSessionNotUploadable, result),
            UploadServiceStatus.PartUrlsMultipartConflict => PartUrlsProblem(ApiErrorCodes.UploadPartUrlsMultipartConflict, result),
            UploadServiceStatus.Conflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Conflict"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, ApiErrorCodes.UploadStorageUnavailable, "Storage unavailable"),
            _ => ProblemResponse(422, ApiErrorCodes.UploadValidationFailed, "Upload validation failed")
        };

    private ObjectResult PartUrlsProblem(string code, UploadServiceResult result)
    {
        var response = ProblemResponse(409, code, "Part URLs unavailable");
        if (response.Value is not ProblemDetails problem) return response;
        problem.Extensions["renewalAllowed"] = false;
        problem.Extensions["reason"] = "multipart_unavailable";
        if (result.PartUrlsFailure is { } failure)
        {
            problem.Extensions["uploadId"] = failure.UploadId;
            if (failure.PartNumbers is not null) problem.Extensions["partNumbers"] = failure.PartNumbers;
            if (failure.ExpiresAt is not null) problem.Extensions["expiresAt"] = failure.ExpiresAt;
            problem.Extensions["serverTime"] = failure.ServerTime;
            problem.Extensions["renewalAllowed"] = failure.RenewalAllowed;
            problem.Extensions["sessionStatus"] = failure.SessionStatus;
            problem.Extensions["reason"] = failure.Reason;
        }
        return response;
    }

    private bool UsesWebSession => User.Identity?.AuthenticationType == RoadGuardSystem.API.Authentication.WebCookieConfiguration.Scheme;

    private bool TryGetActor(out Guid actorUserId, out UserRoleCode role)
    {
        actorUserId = Guid.Empty;
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actorUserId)) return false;
        try
        {
            role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty);
            return role != UserRoleCode.Unknown;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private Guid? CorrelationId()
        => Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : null;

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = "The upload request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
