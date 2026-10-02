using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Services.Reporting;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, Route("api/v{version:apiVersion}/projects/{projectId:guid}/reports")]
public sealed class ReportingController(IReportingService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(Guid projectId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? routeVersionId, [FromQuery] Guid? segmentSetId, [FromQuery] Guid[]? segmentIds, CancellationToken token)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        var result = await service.SummaryAsync(actor, projectId, new(from, to, routeVersionId, segmentSetId, segmentIds), token);
        if (result.Value is not { } value) return Error(result.Code);
        Response.Headers.ETag = $"\"{Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant()}\"";
        return Ok(value);
    }
    [HttpGet("items")]
    public async Task<IActionResult> Items(Guid projectId, [FromQuery] string metric, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] Guid? routeVersionId, [FromQuery] Guid? segmentSetId, [FromQuery] Guid[]? segmentIds, [FromQuery] string? cursor, CancellationToken token, [FromQuery] int pageSize = 50)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        var result = await service.ItemsAsync(actor, projectId, new(from, to, routeVersionId, segmentSetId, segmentIds), metric, cursor, pageSize, token);
        return result.Value is { } value ? Ok(value) : Error(result.Code);
    }
    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline(Guid projectId, [FromQuery] string aggregateType, [FromQuery] Guid aggregateId,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] string? cursor, CancellationToken token, [FromQuery] int pageSize = 50)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        var result = await service.TimelineAsync(actor, projectId, aggregateType, aggregateId, new(from, to), cursor, pageSize, token);
        return result.Value is { } value ? Ok(value) : Error(result.Code);
    }
    private bool Actor(out Guid actor) => Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out actor);
    private ObjectResult Error(string code)
    {
        var status = code switch { "unauthorized" => 401, "access_forbidden" => 403, "not_found" => 404, "cursor_invalid" => 400, _ => 422 };
        var problem = new ProblemDetails { Status = status, Title = code, Detail = "The reporting request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
