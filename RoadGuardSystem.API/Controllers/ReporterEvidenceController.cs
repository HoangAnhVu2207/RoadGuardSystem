using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Files;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reporter-evidence")]
public sealed class ReporterEvidenceController : ControllerBase
{
    private readonly IReporterEvidenceService _service;

    public ReporterEvidenceController(IReporterEvidenceService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost("uploads")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        ReporterEvidenceCreateRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreateAsync(actorUserId, role, request, idempotencyKey, CorrelationId(), cancellationToken);
        if (result.Status == UploadServiceStatus.InvalidInput && result.MaxBytes is not null)
        {
            var error = ProblemResponse(422, ApiErrorCodes.UploadValidationFailed, "Upload validation failed");
            var problem = (ProblemDetails)error.Value!;
            problem.Detail = $"REPORT_PHOTO permits at most {result.MaxBytes} bytes; received {result.ActualBytes}.";
            problem.Extensions["maxBytes"] = result.MaxBytes;
            problem.Extensions["actualBytes"] = result.ActualBytes;
            return error;
        }
        return MapSession(result, StatusCodes.Status201Created, $"/api/v1/reporter-evidence/uploads/{result.Session?.Id:D}");
    }

    [Authorize]
    [HttpGet("uploads/{uploadId:guid}")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSession(Guid uploadId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        return MapSession(await _service.GetSessionAsync(actorUserId, role, uploadId, cancellationToken), StatusCodes.Status200OK, null);
    }

    [Authorize]
    [HttpPost("uploads/{uploadId:guid}/cancel")]
    [ProducesResponseType<UploadCancelResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Cancel(Guid uploadId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryGetActor(out var actorUserId, out var role))
            return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.CancelAsync(actorUserId, role, uploadId, cancellationToken);
        return result switch
        {
            UploadServiceStatus.Success or UploadServiceStatus.Replayed =>
                Ok(new UploadCancelResponseDto(uploadId, "FAILED", UploadSession.CancelledFailureCode)),
            UploadServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.UploadSessionNotFound, "Not found"),
            _ => ProblemResponse(409, "upload_not_cancellable", "Upload cannot be cancelled")
        };
    }

    [Authorize]
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
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return MapSession(
            await _service.CompleteAsync(actorUserId, role, uploadId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken),
            StatusCodes.Status202Accepted,
            $"/api/v1/reporter-evidence/uploads/{uploadId:D}");
    }

    [Authorize]
    [HttpGet("files/{fileId:guid}")]
    [ProducesResponseType<FileMetadataResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFileMetadata(Guid fileId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.GetFileMetadataAsync(actorUserId, role, fileId, cancellationToken);
        if (result.File is not null) Response.Headers.ETag = $"\"{result.File.Version}\"";
        return result.Status switch
        {
            UploadServiceStatus.Success when result.File is not null => Ok(result.File),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            _ => ProblemResponse(404, ApiErrorCodes.FileNotFound, "Not found")
        };
    }

    [Authorize]
    [HttpGet("files/{fileId:guid}/content")]
    public async Task<IActionResult> Download(Guid fileId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.DownloadAsync(actorUserId, role, fileId, cancellationToken);
        return result.Status switch
        {
            UploadServiceStatus.Success when result.Content is not null && result.File is not null => File(result.Content, result.File.MediaType),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, ApiErrorCodes.UploadStorageUnavailable, "Storage unavailable"),
            UploadServiceStatus.Conflict => ProblemResponse(409, "source_not_ready", "Evidence is not ready"),
            _ => ProblemResponse(404, ApiErrorCodes.FileNotFound, "Not found")
        };
    }

    [Authorize]
    [HttpGet("publications/{publicationId:guid}/reports/{reportId:guid}/evidence/{evidenceId:guid}/content")]
    public async Task<IActionResult> DownloadPublication(Guid publicationId, Guid reportId, Guid evidenceId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.DownloadPublicationAsync(actorUserId, role, publicationId, reportId, evidenceId, cancellationToken);
        return result.Status switch
        {
            UploadServiceStatus.Success when result.Content is not null && result.File is not null => File(result.Content, result.File.MediaType),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, ApiErrorCodes.UploadStorageUnavailable, "Storage unavailable"),
            UploadServiceStatus.Conflict => ProblemResponse(409, "source_not_ready", "Evidence is not ready"),
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
