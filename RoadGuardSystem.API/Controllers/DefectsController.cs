using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Services.Defects;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/defects")]
public sealed class DefectsController(IDefectWorkflowService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, [FromQuery] string? status,
        [FromQuery] string? type, [FromQuery] Guid? segmentId, [FromQuery] int pageSize = 50,
        [FromQuery] string? cursor = null, CancellationToken token = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var result = await service.ListAsync(actor, role, projectId, status, type, segmentId,
            pageSize, cursor, token);
        return result.Page is null ? Error(result.Status, result.Code!, result.Errors) : Ok(result.Page);
    }

    [HttpGet("{defectId:guid}")]
    public async Task<IActionResult> Read(Guid projectId, Guid defectId, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        return Map(await service.ReadAsync(actor, role, projectId, defectId, token));
    }

    [HttpPost("{defectId:guid}/assessments")]
    public async Task<IActionResult> Assess(Guid projectId, Guid defectId, DefectAssessmentRequestDto request,
        CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key) || !Request.Headers.ContainsKey("If-Match"))
            return Error(428, "precondition_required");
        return Map(await service.AssessAsync(actor, role, projectId, defectId, request, key.ToString(),
            Request.Headers.IfMatch.ToString(), Correlation(), token));
    }

    [HttpPost("{defectId:guid}/verification-decisions")]
    public async Task<IActionResult> Verify(Guid projectId, Guid defectId, DefectVerificationRequestDto request,
        CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key) || !Request.Headers.ContainsKey("If-Match"))
            return Error(428, "precondition_required");
        return Map(await service.VerifyAsync(actor, role, projectId, defectId, request, key.ToString(),
            Request.Headers.IfMatch.ToString(), Correlation(), token));
    }

    private IActionResult Map(DefectWorkflowResult result)
    {
        if (result.Defect is null) return Error(result.Status, result.Code!, result.Errors);
        Response.Headers.ETag = $"\"{result.Defect.Version}\"";
        return Ok(result.Defect);
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
        var problem = new ProblemDetails { Status = status, Title = "Defect request failed",
            Detail = "The defect request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        if (errors is not null) problem.Extensions["errors"] = errors;
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
