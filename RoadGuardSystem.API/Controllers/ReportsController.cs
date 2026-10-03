using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Services.Reports;
using RoadGuardSystem.Services.Files;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController : ControllerBase
{
    private IReporterReportService? Intake => HttpContext.RequestServices.GetService(typeof(IReporterReportService)) as IReporterReportService;
    private IReporterLifecycleService? Lifecycle => HttpContext.RequestServices.GetService(typeof(IReporterLifecycleService)) as IReporterLifecycleService;

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int pageSize = 20, [FromQuery] string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, "auth_unauthorized", "Unauthorized");
        var lifecycle = Lifecycle;
        if (lifecycle is null) return ProblemResponse(503, "dependency_unavailable", "Report lifecycle is not configured");
        return MapLifecycle(await lifecycle.ListAsync(actor, role, pageSize, cursor, cancellationToken));
    }

    [Authorize]
    [HttpGet("{reportId:guid}")]
    public async Task<IActionResult> Read(Guid reportId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, "auth_unauthorized", "Unauthorized");
        var lifecycle = Lifecycle;
        if (lifecycle is null) return ProblemResponse(503, "dependency_unavailable", "Report lifecycle is not configured");
        return MapLifecycle(await lifecycle.ReadAsync(actor, role, reportId, cancellationToken));
    }

    [Authorize]
    [HttpPost("{reportId:guid}/supplements")]
    public async Task<IActionResult> Supplement(Guid reportId, CreateReporterReportRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? key, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, "auth_unauthorized", "Unauthorized");
        var lifecycle = Lifecycle;
        if (lifecycle is null) return ProblemResponse(503, "dependency_unavailable", "Report lifecycle is not configured");
        if (key is null || ifMatch is null) return ProblemResponse(428, "precondition_required", "Required command header missing");
        if (!TryVersion(ifMatch, out var version)) return ProblemResponse(400, "validation_error", "Invalid If-Match",
            new Dictionary<string, string[]> { ["If-Match"] = ["A strong rowversion ETag is required."] });
        return MapLifecycle(await lifecycle.SupplementAsync(actor, role, reportId, request, key, version!, CorrelationId(), cancellationToken));
    }

    [Authorize]
    [HttpGet("{reportId:guid}/evidence/{evidenceId:guid}/download")]
    public async Task<IActionResult> Download(Guid reportId, Guid evidenceId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, "auth_unauthorized", "Unauthorized");
        var lifecycle = Lifecycle;
        if (lifecycle is null) return ProblemResponse(503, "dependency_unavailable", "Report lifecycle is not configured");
        var result = await lifecycle.DownloadAsync(actor, role, reportId, evidenceId, cancellationToken);
        if (result.Status == UploadServiceStatus.Success && result.Content is not null)
            return File(result.Content, result.File!.MediaType);
        return result.Status switch
        {
            UploadServiceStatus.Forbidden => ProblemResponse(403, "access_forbidden", "Forbidden"),
            UploadServiceStatus.Conflict => ProblemResponse(409, "source_not_ready", "Evidence is not ready"),
            UploadServiceStatus.PreconditionFailed => ProblemResponse(412, "concurrency_conflict", "Evidence version changed"),
            UploadServiceStatus.StorageUnavailable => ProblemResponse(503, "dependency_unavailable", "Storage unavailable"),
            _ => ProblemResponse(404, "not_found", "Not found")
        };
    }

    private IActionResult MapLifecycle(ReporterLifecycleResult result)
    {
        if (result.Status != 200) return ProblemResponse(result.Status, result.Code!, "Report request failed", result.Errors);
        if (result.Report is not null) { Response.Headers.ETag = $"\"{result.Report.Version}\""; return Ok(result.Report); }
        return Ok(result.Page);
    }

    private static bool TryVersion(string header, out string? version)
    {
        version = null;
        if (header.Length < 3 || header[0] != '"' || header[^1] != '"') return false;
        try { var bytes = Convert.FromBase64String(header[1..^1]); if (bytes.Length != 8) return false; version = Convert.ToBase64String(bytes); return true; }
        catch (FormatException) { return false; }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReporterReportRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorId, out var role)) return ProblemResponse(401, "auth_unauthorized", "Unauthorized");
        var service = Intake;
        if (service is null) return ProblemResponse(503, "dependency_unavailable", "Report intake is not configured");
        if (!Request.Headers.ContainsKey("Idempotency-Key")) return ProblemResponse(428, "precondition_required", "Idempotency-Key is required");
        var result = await service.CreateAsync(actorId, role, request, idempotencyKey ?? string.Empty, CorrelationId(), cancellationToken);
        if (result.Report is not null) Response.Headers.ETag = $"\"{result.Report.Version}\"";
        return result.Status switch
        {
            ReporterReportCommandStatus.Created or ReporterReportCommandStatus.Replayed when result.Report is not null => Created($"/api/v1/reports/{result.Report.Id:D}", result.Report),
            ReporterReportCommandStatus.Forbidden => ProblemResponse(403, "access_forbidden", "Forbidden"),
            ReporterReportCommandStatus.NotFound => ProblemResponse(404, "not_found", "Not found"),
            ReporterReportCommandStatus.StaleFile => ProblemResponse(412, "concurrency_conflict", "Precondition failed"),
            ReporterReportCommandStatus.SourceNotReady => ProblemResponse(409, "source_not_ready", "Evidence is not ready"),
            ReporterReportCommandStatus.IdempotencyConflict => ProblemResponse(409, "idempotency_key_reused", "Conflict"),
            _ => ProblemResponse(400, "validation_error", "Validation failed", result.ValidationErrors)
        };
    }

    private bool TryGetActor(out Guid actorId, out UserRoleCode role)
    {
        actorId = Guid.Empty; role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actorId)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private Guid? CorrelationId() => Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : null;

    private ObjectResult ProblemResponse(int status, string code, string title, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = "The report request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        if (errors is not null) problem.Extensions["errors"] = errors;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
