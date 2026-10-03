using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing.Anh02;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, Route("api/v{version:apiVersion}/projects/{projectId:guid}")]
public sealed class AiMockRunsController(IAnh02AiService service, IAiCandidateFactsReader candidates) : ControllerBase
{
    [HttpPost("ai-mock-runs")]
    public async Task<IActionResult> Create(Guid projectId, CreateAiMockRunRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (string.IsNullOrWhiteSpace(key)) return Error(428, "validation_error");
        try
        {
            var view = await service.CreateAsync(actor, role, projectId, request, key, ct);
            Response.Headers.ETag = $"\"{view.Version}\"";
            return Accepted($"/api/v1/projects/{projectId:D}/ai-mock-runs/{view.Id:D}", view);
        }
        catch (AiRequestException ex) { return Error(ex.Status, ex.Code); }
    }
    [HttpGet("ai-mock-runs/{runId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, Guid runId, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        try { var view = await service.GetAsync(actor, role, projectId, runId, ct); Response.Headers.ETag = $"\"{view.Version}\""; return Ok(view); }
        catch (AiRequestException ex) { return Error(ex.Status, ex.Code); }
    }
    [HttpGet("ai-mock-runs/{runId:guid}/result")]
    public async Task<IActionResult> Result(Guid projectId, Guid runId, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        try { var view = await service.GetResultAsync(actor, role, projectId, runId, ct); Response.Headers.ETag = $"\"{view.ResultHash}\""; return Ok(view); }
        catch (AiRequestException ex) { return Error(ex.Status, ex.Code); }
    }
    [HttpGet("ai-candidates/{detectionId:guid}")]
    public async Task<IActionResult> Candidate(Guid projectId, Guid detectionId, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        var result = await candidates.ResolveAsync(actor, role, projectId, detectionId, cancellationToken: ct);
        if (result.Facts is { } facts) { Response.Headers.ETag = $"\"{facts.SourceVersion}\""; return Ok(facts); }
        return result.Status switch
        {
            AnhHuyProducerStatus.Forbidden => Error(403, "access_forbidden"),
            AnhHuyProducerStatus.NotFound => Error(404, "not_found"),
            AnhHuyProducerStatus.StaleGeometry => Error(409, "stale_geometry"),
            _ => Error(409, "source_not_ready")
        };
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "AI mock request could not be completed", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey];
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
