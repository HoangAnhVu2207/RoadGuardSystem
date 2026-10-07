using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Services.Reporting;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/defect-statistics")]
public sealed class DefectStatisticsController(IDefectStatisticsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Read(Guid projectId, [FromQuery] Guid? routeVersionId, [FromQuery] Guid? segmentSetId, [FromQuery] Guid[]? segmentIds, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        return Map(await service.ReadAsync(actor, role, projectId, new(RouteVersionId: routeVersionId, SegmentSetId: segmentSetId, SegmentIds: segmentIds), token));
    }
    [HttpPost]
    public async Task<IActionResult> Confirm(Guid projectId, DefectStatisticsInputDto input, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
        return Map(await service.ConfirmAsync(actor, role, projectId, input, Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers.IfMatch.ToString(), token));
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = default;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor) &&
            ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role);
    }
    private ObjectResult Map(RoadGuardSystem.BusinessObjects.Reporting.DefectStatisticsResult result)
    {
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Value is not null) Response.Headers.ETag = '"' + result.Value.Version + '"';
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Defect statistics request failed", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
