using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Services.Defects;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/candidate-decisions")]
public sealed class CandidatesController : ControllerBase
{
    private ICandidateDecisionService? Service => HttpContext.RequestServices.GetService(typeof(ICandidateDecisionService)) as ICandidateDecisionService;
    private RoadGuardSystem.Services.Candidates.ICandidateMatchingService? Matching =>
        HttpContext.RequestServices.GetService(typeof(RoadGuardSystem.Services.Candidates.ICandidateMatchingService))
        as RoadGuardSystem.Services.Candidates.ICandidateMatchingService;

    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:guid}/candidates")]
    public async Task<IActionResult> Match(Guid projectId, [FromQuery] string? sourceKind,
        [FromQuery] Guid sourceId, [FromQuery] bool expand = false,
        [FromQuery] int pageSize = 50, [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var matching = Matching;
        if (matching is null) return Error(503, "dependency_unavailable");
        var result = await matching.MatchAsync(actor, role, projectId, sourceKind, sourceId,
            expand, pageSize, cursor, cancellationToken);
        return result.Page is null ? Error(result.Status, result.Code!) : Ok(result.Page);
    }

    [HttpPost]
    public async Task<IActionResult> Decide(Guid projectId, CandidateDecisionRequestDto request, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var service = Service;
        if (service is null) return Error(503, "dependency_unavailable");
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key)) return Error(428, "precondition_required");
        var correlation = Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : (Guid?)null;
        return Map(projectId, await service.DecideAsync(actor, role, projectId, request, key.ToString(), correlation, cancellationToken));
    }
    [HttpGet("{decisionId:guid}")]
    public async Task<IActionResult> Read(Guid projectId, Guid decisionId, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var service = Service;
        if (service is null) return Error(503, "dependency_unavailable");
        return Map(projectId, await service.ReadAsync(actor, role, projectId, decisionId, cancellationToken));
    }
    private IActionResult Map(Guid project, CandidateDecisionResult result)
    {
        if (result.Decision is null) return Error(result.Status, result.Code!, result.Errors);
        Response.Headers.ETag = $"\"{result.Decision.Version}\"";
        return result.Status == 201 ? Created($"/api/v1/projects/{project}/candidate-decisions/{result.Decision.Id}", result.Decision) : Ok(result.Decision);
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
    private ObjectResult Error(int status, string code, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails { Status = status, Title = "Candidate request failed", Detail = "The candidate request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        if (errors is not null) problem.Extensions["errors"] = errors;
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
