using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Services.Exports;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/exports")]
[WebCookieEligible]
public sealed class ExportsController(IExportService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid projectId, [FromHeader(Name = "Idempotency-Key")] string? key, CreateExportRequestDto request, CancellationToken ct)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        if (string.IsNullOrWhiteSpace(key)) return Error("precondition_required");
        var result = await service.CreateAsync(actor, projectId, request, key, CorrelationId(), ct);
        if (result.Value is null) return Error(result.Code);
        Response.Headers.ETag = Quote(result.Value.Version);
        return Accepted($"/api/v1/projects/{projectId:D}/exports/{result.Value.Id:D}", result.Value);
    }
    [HttpGet("{exportId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, Guid exportId, CancellationToken ct)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        var result = await service.GetAsync(actor, projectId, exportId, ct); if (result.Value is null) return Error(result.Code);
        Response.Headers.ETag = Quote(result.Value.Version); return Ok(result.Value);
    }
    [HttpGet("{exportId:guid}/manifest")]
    public async Task<IActionResult> Manifest(Guid projectId, Guid exportId, CancellationToken ct)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        var result = await service.ManifestAsync(actor, projectId, exportId, ct); if (result.Value is null) return Error(result.Code);
        Response.Headers.ETag = Quote(result.Value.SnapshotHash); return Ok(result.Value);
    }
    [HttpGet("{exportId:guid}/content")]
    public async Task<IActionResult> Content(Guid projectId, Guid exportId, CancellationToken ct)
    {
        if (!Actor(out var actor)) return Error("unauthorized");
        var result = await service.ContentAsync(actor, projectId, exportId, ct); if (result.Value is null) return Error(result.Code);
        Response.Headers.ETag = Quote(result.Value.Sha256); Response.Headers.CacheControl = "private, no-store";
        return File(result.Value.Content, result.Value.MediaType, result.Value.FileName, enableRangeProcessing: false);
    }
    private bool Actor(out Guid actor) => Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor);
    private Guid? CorrelationId() => Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : null;
    private static string Quote(string value) => "\"" + value + "\"";
    private ObjectResult Error(string code)
    {
        var status = code switch { "unauthorized" => 401, "forbidden" => 403, "not_found" => 404, "precondition_required" => 428, "idempotency_conflict" or "export_not_ready" => 409, "export_expired" => 410, "producer_unavailable" or "export_storage_unavailable" => 503, _ => 422 };
        var details = new ProblemDetails { Status = status, Title = code, Instance = Request.Path };
        details.Extensions["code"] = code; return new ObjectResult(details) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
