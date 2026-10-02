using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Services.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class ProcessingV2Controller : ControllerBase
{
    private readonly IProcessingV2Service _service;

    public ProcessingV2Controller(IProcessingV2Service service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost("processing-jobs")]
    public async Task<IActionResult> CreateProcessingJob([FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CreateProcessingJobRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var role)) return Problem(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Problem(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        return Map(await _service.CreateAsync(actor, role, request, idempotencyKey, CorrelationId(), cancellationToken), created: true);
    }

    [Authorize]
    [HttpGet("processing-jobs/{jobId:guid}")]
    public async Task<IActionResult> GetProcessingJob(Guid jobId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var role)) return Problem(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        return Map(await _service.GetAsync(actor, role, jobId, cancellationToken));
    }

    [Authorize]
    [HttpPost("processing-jobs/{jobId:guid}/retry")]
    public async Task<IActionResult> RetryProcessingJob(Guid jobId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, RetryProcessingJobRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var role)) return Problem(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return Problem(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return Map(await _service.RetryAsync(actor, role, jobId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken));
    }

    [Authorize(Policy = "AiCallback")]
    [HttpPost("internal/processing-jobs/{jobId:guid}/results")]
    public async Task<IActionResult> ReceiveAiResult(Guid jobId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, ReceiveAiResultRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Problem(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        return Map(await _service.ReceiveResultAsync(jobId, request, idempotencyKey, cancellationToken));
    }

    [Authorize]
    [HttpPost("projects/{projectId:guid}/validation-runs")]
    public async Task<IActionResult> CreateValidationRun(Guid projectId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CreateValidationRunRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var role)) return Problem(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return Problem(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreateValidationAsync(actor, role, projectId, request, idempotencyKey, CorrelationId(), cancellationToken);
        return result.Status switch
        {
            ProcessingV2ServiceStatus.Success or ProcessingV2ServiceStatus.Replayed when result.Job is not null => ValidationAccepted(result.Job),
            ProcessingV2ServiceStatus.Forbidden => Problem(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            ProcessingV2ServiceStatus.IdempotentConflict => Problem(409, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
            ProcessingV2ServiceStatus.Conflict => Problem(409, ApiErrorCodes.ValidationError, "Validation run cannot be created"),
            _ => Problem(422, ApiErrorCodes.ValidationError, "Validation run is invalid")
        };
    }

    [Authorize]
    [HttpGet("validation-runs/{runId:guid}")]
    public async Task<IActionResult> GetValidationResult(Guid runId, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var role)) return Problem(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.GetValidationAsync(actor, role, runId, cancellationToken);
        return result is null ? Problem(404, ApiErrorCodes.NotFound, "Not found") : Ok(result);
    }

    private ObjectResult Map(ProcessingV2ServiceResult result, bool created = false)
    {
        if (result.Job is not null) Response.Headers.ETag = $"\"{result.Job.Version}\"";
        return result.Status switch
        {
            ProcessingV2ServiceStatus.Success or ProcessingV2ServiceStatus.Replayed when result.Job is not null => created ? Accepted($"/api/v1/processing-jobs/{result.Job.Id}", result.Job) : Ok(result.Job),
            ProcessingV2ServiceStatus.Forbidden => Problem(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            ProcessingV2ServiceStatus.NotFound => Problem(404, ApiErrorCodes.NotFound, "Not found"),
            ProcessingV2ServiceStatus.ConcurrencyConflict => Problem(412, ApiErrorCodes.ConcurrencyConflict, "Precondition failed"),
            ProcessingV2ServiceStatus.IdempotentConflict => Problem(409, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
            ProcessingV2ServiceStatus.Conflict => Problem(409, ApiErrorCodes.ValidationError, "Processing transition is not allowed"),
            _ => Problem(422, ApiErrorCodes.ValidationError, "Processing request is invalid")
        };
    }

    private AcceptedResult ValidationAccepted(ProcessingJobResponseDto job)
    {
        Response.Headers.ETag = $"\"{job.Version}\"";
        return Accepted($"/api/v1/validation-runs/{job.Id}", job);
    }

    private bool TryActor(out Guid id, out UserRoleCode role)
    {
        id = Guid.Empty;
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out id)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private Guid? CorrelationId() => Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : null;
    private ObjectResult Problem(int status, string code, string title)
    {
        var details = new ProblemDetails { Status = status, Title = title, Detail = "The processing request could not be completed.", Instance = Request.Path };
        details.Extensions["code"] = code;
        details.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(details) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
