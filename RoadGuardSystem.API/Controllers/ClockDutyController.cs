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
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/clocks/{clockId:guid}")]
public sealed class ClockDutyController(ClockDutyService service) : ControllerBase
{
    [HttpPost("extensions")]
    public Task<IActionResult> Extend(Guid projectId, Guid clockId, ClockExtensionInput input, CancellationToken token)
        => Execute(projectId, clockId, "extend", input.Reason, input.NewDueAt, null, token);
    [HttpPost("appointments")]
    public Task<IActionResult> Appoint(Guid projectId, Guid clockId, BusinessDutyAppointmentInput input, CancellationToken token)
        => Execute(projectId, clockId, "appoint", input.Reason, null, input.AssigneeId, token);
    private async Task<IActionResult> Execute(Guid project, Guid clock, string action, string reason, DateTimeOffset? due,
        Guid? assignee, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count != 1 || Request.Headers.IfMatch.Count != 1) return Error(400, "validation_error");
        return Map(await service.ExecuteAsync(actor, role, project, clock, action, Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers.IfMatch.ToString(), reason, due, assignee, token));
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
        var problem = new ProblemDetails { Status = status, Title = "Clock duty request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
