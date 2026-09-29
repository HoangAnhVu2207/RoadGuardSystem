using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
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
    [HttpPost("uploads")]
    [ProducesResponseType<UploadSessionResponseDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        UploadCreateRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreateAsync(actorUserId, role, request, idempotencyKey, CorrelationId(), cancellationToken);
        return MapSession(result, StatusCodes.Status201Created, $"/api/v1/uploads/{result.Session?.Id:D}");
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
    [HttpPost("uploads/{uploadId:guid}/part-urls")]
    [ProducesResponseType<UploadPartUrlsResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartUrls(
        Guid uploadId,
        UploadPartUrlsRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        return MapPartUrls(await _service.GetPartUrlsAsync(actorUserId, role, uploadId, request, idempotencyKey, cancellationToken));
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
            $"/api/v1/uploads/{uploadId:D}");
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
            UploadServiceStatus.Success when result.PartUrls is not null => Ok(result.PartUrls),
            UploadServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            UploadServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.UploadSessionNotFound, "Not found"),
            UploadServiceStatus.Conflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Conflict"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, ApiErrorCodes.UploadStorageUnavailable, "Storage unavailable"),
            _ => ProblemResponse(422, ApiErrorCodes.UploadValidationFailed, "Upload validation failed")
        };

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
