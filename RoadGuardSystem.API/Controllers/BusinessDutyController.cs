using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/receiving-requests/{requestId:guid}")]
public sealed class BusinessDutyController(BusinessDutyService service) : ControllerBase
{
    [HttpGet("/api/v{version:apiVersion}/projects/{projectId:guid}/receiving-requests")]
    public async Task<IActionResult> List(Guid projectId, [FromQuery] Guid? scopeId, [FromQuery] Guid? after,
        [FromQuery] int limit = 25, CancellationToken token = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        return Map(await service.ListAsync(actor, role, projectId, scopeId, after, limit, token));
    }
    [HttpGet]
    public async Task<IActionResult> Read(Guid projectId, Guid requestId, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        return Map(await service.ReadAsync(actor, role, projectId, requestId, token));
    }
    [HttpPost("ack")]
    public Task<IActionResult> Ack(Guid projectId, Guid requestId, BusinessRequestAckInput input, CancellationToken token)
        => Execute(projectId, requestId, "ack", null, null, input.ClaimedDeviceAt, token);
    [HttpPost("appointment")]
    public Task<IActionResult> Appoint(Guid projectId, Guid requestId, BusinessDutyAppointmentInput input, CancellationToken token)
        => Execute(projectId, requestId, "appoint", input.AssigneeId, input.Reason, null, token);
    private async Task<IActionResult> Execute(Guid project, Guid request, string action, Guid? assignee, string? reason,
        DateTimeOffset? claimedAt, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count != 1 || Request.Headers.IfMatch.Count != 1) return Error(400, "validation_error");
        return Map(await service.ExecuteAsync(actor, role, project, request, action, Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers.IfMatch.ToString(), assignee, reason, claimedAt, token));
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = default;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor) &&
            ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role);
    }
    private ObjectResult Map(BusinessDutyResult result)
    {
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Version is not null) Response.Headers.ETag = $"\"{result.Version}\"";
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Business duty request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
