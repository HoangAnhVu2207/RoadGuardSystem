using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController(IReporterReportService service) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReporterReportRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorId, out var role)) return ProblemResponse(401, "auth_unauthorized", "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, "precondition_required", "Idempotency-Key is required");
        var result = await service.CreateAsync(actorId, role, request, idempotencyKey, CorrelationId(), cancellationToken);
        if (result.Report is not null) Response.Headers.ETag = $"\"{result.Report.Version}\"";
        return result.Status switch
        {
            ReporterReportCommandStatus.Created or ReporterReportCommandStatus.Replayed when result.Report is not null => Created($"/api/v1/reports/{result.Report.Id:D}", result.Report),
            ReporterReportCommandStatus.Forbidden => ProblemResponse(403, "access_forbidden", "Forbidden"),
            ReporterReportCommandStatus.NotFound => ProblemResponse(404, "not_found", "Not found"),
            ReporterReportCommandStatus.StaleFile => ProblemResponse(412, "concurrency_conflict", "Precondition failed"),
            ReporterReportCommandStatus.SourceNotReady => ProblemResponse(409, "source_not_ready", "Evidence is not ready"),
            ReporterReportCommandStatus.IdempotencyConflict => ProblemResponse(409, "idempotency_key_reused", "Conflict"),
            _ => ProblemResponse(400, "validation_error", "Validation failed")
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

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = "The report request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
