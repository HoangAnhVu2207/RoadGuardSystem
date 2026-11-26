using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using RoadGuardSystem.Services.Cases;

namespace RoadGuardSystem.API.Controllers;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CaseRoutingDecisionDto(string Outcome, string Reason, Guid ReportId, Guid? EvidenceId = null,
    string? RoutingEvaluationVersion = null, RoutingWholeCaseAttestation? Attestation = null);

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}")]
public sealed class CaseRoutingController(IRoutingMapService service) : ControllerBase
{
    [HttpGet("company/road-map"), WebCookieEligible]
    [ProducesResponseType<RoutingMapView>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Map([FromQuery] string? bbox = null, [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null, [FromQuery] bool includeHistorical = false, CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var result = await service.MapAsync(actor, role, bbox, limit, cursor, includeHistorical, cancellationToken);
        if (result.Value is RoutingMapView map)
        {
            var etag = $"\"{map.SnapshotVersion}\"";
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, no-cache";
            if (Request.Headers.IfNoneMatch.Any(x => x == etag)) return StatusCode(304);
        }
        return Result(result);
    }

    [HttpGet("cases/{caseId:guid}/routing-candidates"), WebCookieEligible]
    [ProducesResponseType<RoutingCandidatesView>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(412)]
    public async Task<IActionResult> Candidates(Guid caseId, [FromQuery] Guid reportId, [FromQuery] Guid? evidenceId = null,
        [FromQuery] int limit = 20, [FromQuery] string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        return Result(await service.CandidatesAsync(actor, role, caseId, reportId, evidenceId, limit, cursor, cancellationToken));
    }

    [HttpGet("cases/{caseId:guid}/routing-decisions"), WebCookieEligible]
    [ProducesResponseType<RoutingDecisionPage>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> History(Guid caseId, [FromQuery] int limit = 20, [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        return Result(await service.HistoryAsync(actor, role, caseId, limit, cursor, cancellationToken));
    }

    [HttpPost("cases/{caseId:guid}/routing-decisions"), WebCookieEligible]
    [ProducesResponseType<RoutingWriteView>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(412)]
    [ProducesResponseType<ProblemDetails>(428)]
    public async Task<IActionResult> Decide(Guid caseId, CaseRoutingDecisionDto request, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        if (Request.Headers.IfMatch.Count > 1 || Request.Headers["Idempotency-Key"].Count > 1) return Error(400, "validation_error");
        var match = Request.Headers.IfMatch.ToString();
        var key = Request.Headers["Idempotency-Key"].ToString();
        if (match.Length == 0 || key.Length == 0) return Error(428, "precondition_required");
        if (match.Length < 3 || match[0] != '"' || match[^1] != '"') return Error(400, "validation_error");
        try { if (Convert.FromBase64String(match[1..^1]).Length != 8) return Error(400, "validation_error"); }
        catch (FormatException) { return Error(400, "validation_error"); }
        var correlation = Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : (Guid?)null;
        var result = await service.DecideAsync(actor, role, caseId, match[1..^1], key,
            new(request.Outcome, request.Reason, request.ReportId, request.EvidenceId, request.RoutingEvaluationVersion, request.Attestation), correlation, cancellationToken);
        if (result.Value is RoutingWriteView write) Response.Headers.ETag = $"\"{write.CaseVersion}\"";
        return Result(result);
    }

    private IActionResult Result(RoutingResult result) => result.Status == 200 ? Ok(result.Value) : Error(result.Status, result.Code!);
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Routing request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        return StatusCode(status, problem);
    }
}
