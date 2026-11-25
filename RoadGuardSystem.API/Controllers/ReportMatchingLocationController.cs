using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Services.Candidates;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/reports/{reportId:guid}/matching-location")]
public sealed class ReportMatchingLocationController(IReportMatchingLocationService service) : ControllerBase
{
    [HttpGet, ProducesResponseType<ReportMatchingLocationView>(200)]
    public Task<IActionResult> Read(Guid projectId, Guid reportId, CancellationToken token)
        => Run(projectId, reportId, null, token);
    [HttpPost, ProducesResponseType<ReportMatchingLocationView>(200)]
    public Task<IActionResult> Confirm(Guid projectId, Guid reportId, ReportMatchingLocationInput input, CancellationToken token)
        => Run(projectId, reportId, input, token);
    private async Task<IActionResult> Run(Guid project, Guid report, ReportMatchingLocationInput? input, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "auth_unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1) return Error(400, "validation_error");
        var result = input is null ? await service.ReadAsync(actor, role, project, report, token)
            : await service.ConfirmAsync(actor, role, project, report, input, Request.Headers["Idempotency-Key"].FirstOrDefault(), token);
        Response.Headers.CacheControl = "no-store";
        return result.Value is null ? Error(result.Status, result.Code!) : Ok(result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Matching location request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
