using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing.Worker;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, Route("api/v{version:apiVersion}/projects/{projectId:guid}/ai-videos/{workId:guid}")]
public sealed class AiVideoTelemetryController(AiVideoTelemetryService service, IAiVideoTelemetryRepository repository) : ControllerBase
{
    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:guid}/ai-videos")]
    public async Task<IActionResult> Discover(Guid projectId, [FromQuery] Guid datasetId, CancellationToken ct,
        [FromQuery] int offset = 0, [FromQuery] int limit = 50)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        try { return Ok(await repository.DiscoverAsync(actor, role, projectId, datasetId, offset, limit, ct)); }
        catch (AiRequestException ex) { return Error(ex.Status, ex.Code); }
    }

    [HttpGet("telemetry")]
    public async Task<IActionResult> Get(Guid projectId, Guid workId, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        try
        {
            var view = await service.GetAsync(actor, role, projectId, workId, ct);
            Response.Headers.ETag = $"\"{view.HeadRevisionId:D}\"";
            return Ok(view);
        }
        catch (AiRequestException ex) { return Error(ex.Status, ex.Code); }
    }

    [HttpPost("telemetry-revisions"), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Append(Guid projectId, Guid workId, AiTelemetryAppendRequest request,
        [FromHeader(Name = "Idempotency-Key"), MaxLength(200)] string? key,
        [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(ifMatch)) return Error(428, "required_precondition");
        if (ifMatch.Length != 38 || ifMatch[0] != '"' || ifMatch[^1] != '"' || !Guid.TryParseExact(ifMatch[1..^1], "D", out var head))
            return Error(422, "invalid_telemetry_version");
        try
        {
            var id = await service.AppendAsync(actor, role, projectId, workId, request, key, head, ct);
            Response.Headers.ETag = $"\"{id:D}\"";
            return Ok(new { revisionId = id });
        }
        catch (AiRequestException ex) { return Error(ex.Status, ex.Code); }
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
        var problem = new ProblemDetails { Status = status, Title = "Video telemetry request could not be completed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey];
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
