using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Services.Messaging;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible]
[Route("api/v{version:apiVersion}")]
public sealed class H6NotificationOperationsController(H6NotificationReadService service) : ControllerBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [HttpGet("notifications/{notificationId:guid}/scope")]
    public async Task<IActionResult> Scope(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        var scope = await service.ScopeAsync(actor, role, notificationId, cancellationToken);
        return scope is null ? Error(404, "notification_not_found") : Ok(scope);
    }

    [HttpGet("projects/{projectId:guid}/clocks")]
    public async Task<IActionResult> Clocks(Guid projectId, [FromQuery] string? cursor,
        [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (limit is < 1 or > 100 || cursor?.Length > 1024) return Error(400, "validation_error");
        H6ClockCursor? parsed = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            try { parsed = JsonSerializer.Deserialize<H6ClockCursor>(cursor, Json); }
            catch (JsonException) { return Error(400, "validation_error"); }
            if (parsed is null) return Error(400, "validation_error");
        }
        var page = await service.ClocksPageAsync(actor, role, projectId, parsed, limit ?? 25, cancellationToken);
        return page.Status switch
        {
            "READY" => Ok(page),
            "DENIED" => Error(403, "project_access_denied"),
            "CURSOR_STALE" => Error(409, "cursor_stale"),
            _ => Error(400, "validation_error")
        };
    }

    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = default;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor) &&
            ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role);
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Notification request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
