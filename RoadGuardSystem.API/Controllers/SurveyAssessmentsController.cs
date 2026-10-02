using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Services.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, Route("api/v{version:apiVersion}")]
public sealed class SurveyAssessmentsController(ISurveyAssessmentService service) : ControllerBase
{
    [HttpPost("datasets/{id:guid}/assessments")]
    public async Task<IActionResult> Create(Guid id, CreateAssessmentDto body, [FromHeader(Name = "Idempotency-Key")] string? key,
        [FromHeader(Name = "If-Match")] string? version, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error("unauthorized");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return Error("precondition_required");
        var result = await service.CreateAsync(actor, role, id, body, key, version, Correlation(), token);
        if (result.Assessment is { } view) { Response.Headers.ETag = $"\"{view.Version}\""; return Created($"/api/v1/datasets/{id}/assessments/{view.Id}", view); }
        return Error(result.Code);
    }
    [HttpGet("datasets/{id:guid}/assessments/{assessmentId:guid}")]
    public async Task<IActionResult> Read(Guid id, Guid assessmentId, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error("unauthorized");
        var result = await service.ReadAsync(actor, role, id, assessmentId, token);
        if (result.Assessment is { } view) { Response.Headers.ETag = $"\"{view.Version}\""; return Ok(view); }
        return Error(result.Code);
    }
    [HttpPost("projects/{projectId:guid}/baseline-selections")]
    public async Task<IActionResult> Select(Guid projectId, CreateBaselineDto body, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error("unauthorized");
        if (string.IsNullOrWhiteSpace(key)) return Error("precondition_required");
        var result = await service.SelectAsync(actor, role, projectId, body, key, Correlation(), token);
        if (result.Baseline is { } view) { Response.Headers.ETag = $"\"{view.Version}\""; return Created($"/api/v1/projects/{projectId}/baseline-selections/{view.Id}", view); }
        return Error(result.Code);
    }
    [HttpGet("projects/{projectId:guid}/baseline-selections")]
    public Task<IActionResult> Current(Guid projectId, [FromQuery] Guid? segmentSetId, CancellationToken token) => ReadBaseline(projectId, null, segmentSetId, token);
    [HttpGet("projects/{projectId:guid}/baseline-selections/{id:guid}")]
    public Task<IActionResult> History(Guid projectId, Guid id, CancellationToken token) => ReadBaseline(projectId, id, null, token);
    private async Task<IActionResult> ReadBaseline(Guid project, Guid? id, Guid? set, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error("unauthorized");
        var result = await service.BaselineReadAsync(actor, role, project, id, set, token);
        return result.Code == "success" ? Ok(result.Value) : Error(result.Code);
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
    private Guid? Correlation() => Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var id) ? id : null;
    private ObjectResult Error(string code)
    {
        var status = code switch { "unauthorized" => 401, "access_forbidden" => 403, "not_found" => 404, "precondition_required" => 428,
            "concurrency_conflict" => 412, "duplicate_request" or "baseline_selection_conflict" => 409, _ => 422 };
        var problem = new ProblemDetails { Status = status, Title = code, Detail = "The survey review request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code == "precondition_required" ? "validation_error" : code;
        problem.Extensions["correlationId"] = Correlation()?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
