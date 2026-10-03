using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Labels;
using RoadGuardSystem.Services.Labels;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/labels")]
public sealed class TrainingLabelsController(ITrainingLabelService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid projectId, CreateTrainingLabelDto request, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key)) return Error(428, "precondition_required");
        return Map(await service.CreateAsync(actor, role, projectId, request, key.ToString(), Correlation(), token));
    }

    [HttpGet("{labelId:guid}")]
    public async Task<IActionResult> Read(Guid projectId, Guid labelId, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        return Map(await service.ReadAsync(actor, role, projectId, labelId, token));
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, [FromQuery] int pageSize = 50,
        [FromQuery] string? cursor = null, CancellationToken token = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var result = await service.ListAsync(actor, role, projectId, pageSize, cursor, token);
        return result.Page is null ? Error(result.Status, result.Code!) : Ok(result.Page);
    }

    [HttpPost("{labelId:guid}/revisions")]
    public async Task<IActionResult> Revise(Guid projectId, Guid labelId, ReviseTrainingLabelDto request, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key)) return Error(428, "precondition_required");
        if (!Request.Headers.ContainsKey("If-Match")) return Error(428, "precondition_required");
        return Map(await service.ReviseAsync(actor, role, projectId, labelId, request, key.ToString(),
            Request.Headers.IfMatch.ToString(), Correlation(), token));
    }

    [HttpPost("~/api/v{version:apiVersion}/labels/{labelId:guid}/review")]
    public async Task<IActionResult> Review(Guid labelId, ReviewTrainingLabelDto request, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key)) return Error(428, "precondition_required");
        if (!Request.Headers.ContainsKey("If-Match")) return Error(428, "precondition_required");
        return Map(await service.ReviewAsync(actor, role, labelId, request, key.ToString(),
            Request.Headers.IfMatch.ToString(), Correlation(), token));
    }

    private IActionResult Map(TrainingLabelResult result)
    {
        if (result.Label is null) return Error(result.Status, result.Code!, result.Errors);
        Response.Headers.ETag = $"\"{result.Label.Version}\"";
        var location = $"/api/v1/projects/{result.Label.ProjectId}/labels/{result.Label.Id}";
        return result.Status == 201 ? Created(location, result.Label) : Ok(result.Label);
    }

    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private Guid? Correlation() => Guid.TryParse(
        HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var id) ? id : null;

    private ObjectResult Error(int status, string code, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails { Status = status, Title = "Training label request failed",
            Detail = "The training label request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        if (errors is not null) problem.Extensions["errors"] = errors;
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
