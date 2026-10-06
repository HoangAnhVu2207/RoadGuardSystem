using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}")]
public sealed class PavementWorkflowController(IPavementWorkflowService service) : ControllerBase
{
    private static readonly System.Text.Json.JsonSerializerOptions PublicJson = new(System.Text.Json.JsonSerializerDefaults.Web);
    [HttpPost("route-versions/{routeVersionId:guid}/segment-sets/{segmentSetId:guid}/pavement-plans")]
    public Task<IActionResult> CreatePlan(Guid projectId, Guid routeVersionId, Guid segmentSetId, PavementPlanCreateInput input, CancellationToken ct)
        => Run(projectId, "plan-create", ct, routeVersionId, segmentSetId, input: input);
    [HttpGet("pavement-layouts/{layoutId:guid}")]
    public Task<IActionResult> GetLayout(Guid projectId, Guid layoutId, CancellationToken ct)
        => Run(projectId, "layout-get", ct, resource: layoutId);
    [HttpPost("pavement-layouts/as-built")]
    public Task<IActionResult> CreateAsBuilt(Guid projectId, AsBuiltLayoutInput input, CancellationToken ct)
        => Run(projectId, "asbuilt-create", ct, input: input);
    [HttpPost("geometry-map-publications")]
    public Task<IActionResult> Publish(Guid projectId, GeometryMapPublishInput input, CancellationToken ct)
        => Run(projectId, "publish", ct, input: input);
    [HttpGet("geometry-map-publications/{publicationId:guid}")]
    public Task<IActionResult> Manifest(Guid projectId, Guid publicationId, CancellationToken ct)
        => Run(projectId, "manifest", ct, resource: publicationId);
    [HttpGet("geometry-map-publications/{publicationId:guid}/layers/{layer}")]
    public Task<IActionResult> Page(Guid projectId, Guid publicationId, string layer,
        [FromQuery] string? expectedContentHash, [FromQuery] int limit = 100, [FromQuery] double[]? bbox = null,
        [FromQuery] string? cursor = null, [FromQuery] Guid? routeVersionId = null, [FromQuery] Guid? segmentSetId = null,
        CancellationToken ct = default)
        => Run(projectId, "page", ct, routeVersionId, segmentSetId, publicationId,
            new PavementLayerQuery(layer, limit, bbox, cursor), expectedContentHash);
    [HttpPost("geometry-location-impacts")]
    public Task<IActionResult> CreateImpact(Guid projectId, GeometryImpactInput input, CancellationToken ct)
        => Run(projectId, "impact-create", ct, input: input);
    [HttpGet("geometry-location-impacts")]
    public Task<IActionResult> ListImpacts(Guid projectId, [FromQuery] Guid? previousRouteVersionId = null,
        [FromQuery] Guid? newRouteVersionId = null, CancellationToken ct = default)
        => Run(projectId, "impact-list", ct, previousRouteVersionId, newRouteVersionId);
    [HttpGet("geometry-location-impacts/{impactId:guid}")]
    public Task<IActionResult> GetImpact(Guid projectId, Guid impactId, CancellationToken ct)
        => Run(projectId, "impact-get", ct, resource: impactId);
    [HttpPost("geometry-location-impacts/{impactId:guid}/decisions")]
    public Task<IActionResult> DecideImpact(Guid projectId, Guid impactId, GeometryImpactDecisionInput input, CancellationToken ct)
        => Run(projectId, "impact-decide", ct, resource: impactId, input: input);

    private async Task<IActionResult> Run(Guid project, string action, CancellationToken ct,
        Guid? route = null, Guid? set = null, Guid? resource = null, object? input = null, string? hash = null)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        if (Request.Headers.TryGetValue("Idempotency-Key", out var keys) && keys.Count != 1)
            return Error(400, "validation_error");
        var etags = Request.Headers.IfMatch;
        if (etags.Count > 1) return Error(400, "validation_error");
        var etag = etags.ToString();
        if (hash is not null && (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c)))) return Error(400, "validation_error");
        if (etag.Length > 0 && (etag.Length != 66 || etag[0] != '"' || etag[^1] != '"' || etag[1..^1].Any(c => !Uri.IsHexDigit(c))))
            return Error(412, "content_hash_mismatch");
        if (hash is not null && etag.Length > 0 && !string.Equals(hash, etag[1..^1], StringComparison.OrdinalIgnoreCase))
            return Error(409, "geometry_version_mismatch");
        var result = await service.ExecuteAsync(role, new(actor, project, action, route, set, resource, input,
            keys.ToString(), (hash ?? (etag.Length == 0 ? null : etag[1..^1]))?.ToLowerInvariant()), ct);
        if (result.Code is not null) return Error(result.Status, result.Code);
        var json = System.Text.Json.JsonSerializer.SerializeToElement(result.Value, PublicJson);
        if (json.ValueKind == System.Text.Json.JsonValueKind.Object && json.TryGetProperty("contentHash", out var contentHash))
            Response.Headers.ETag = $"\"{contentHash.GetString()}\"";
        if (result.Status == 201 && json.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (json.TryGetProperty("publicationId", out var publicationId)) Response.Headers.Location = $"/api/v1/projects/{project}/geometry-map-publications/{publicationId.GetGuid()}";
            else if (json.TryGetProperty("kind", out _) && json.TryGetProperty("id", out var layoutId)) Response.Headers.Location = $"/api/v1/projects/{project}/pavement-layouts/{layoutId.GetGuid()}";
            else if (action == "impact-create" && json.TryGetProperty("id", out var impactId)) Response.Headers.Location = $"/api/v1/projects/{project}/geometry-location-impacts/{impactId.GetGuid()}";
        }
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Pavement request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
