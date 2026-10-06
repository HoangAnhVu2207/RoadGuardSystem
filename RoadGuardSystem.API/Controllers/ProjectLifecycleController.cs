using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/lifecycle")]
public sealed class ProjectLifecycleController(IProjectLifecycleService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Read(Guid projectId, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        return Map(await service.ReadAsync(actor, role, projectId, cancellationToken));
    }
    [HttpPost("renewed-handling-scope")]
    public async Task<IActionResult> Renew(Guid projectId, RenewedHandlingScopeInput input, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
        return Map(await service.RenewAsync(actor, role, projectId, input, Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers.IfMatch.ToString(), cancellationToken));
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = default;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor) &&
            ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role);
    }
    private ObjectResult Map(ProjectLifecycleMutationResult result)
    {
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Value is not null) Response.Headers.ETag = $"\"{result.Value.Version}\"";
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Project lifecycle request failed", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
