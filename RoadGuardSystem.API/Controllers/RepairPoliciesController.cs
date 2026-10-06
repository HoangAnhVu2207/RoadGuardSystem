using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-policies")]
public sealed class RepairPoliciesController(IRepairPolicyService service) : ControllerBase
{
    [HttpPost("drafts")]
    public Task<IActionResult> Create(Guid projectId, RepairPolicyDefinitionInput input, CancellationToken token)
        => Execute(projectId, "create", null, input, null, token);
    [HttpGet("drafts/{draftId:guid}")]
    public Task<IActionResult> Draft(Guid projectId, Guid draftId, CancellationToken token)
        => Execute(projectId, "draft-get", draftId, null, null, token);
    [HttpPut("drafts/{draftId:guid}")]
    public Task<IActionResult> Update(Guid projectId, Guid draftId, RepairPolicyDefinitionInput input, CancellationToken token)
        => Execute(projectId, "update", draftId, input, null, token);
    [HttpPost("drafts/{draftId:guid}/publish")]
    public Task<IActionResult> Publish(Guid projectId, Guid draftId, RepairPolicyReasonInput input, CancellationToken token)
        => Execute(projectId, "publish", draftId, null, input.Reason, token);
    [HttpGet("revisions/{revisionId:guid}")]
    public Task<IActionResult> Revision(Guid projectId, Guid revisionId, CancellationToken token)
        => Execute(projectId, "revision-get", revisionId, null, null, token);
    [HttpPost("revisions/{revisionId:guid}/revoke")]
    public Task<IActionResult> Revoke(Guid projectId, Guid revisionId, RepairPolicyReasonInput input, CancellationToken token)
        => Execute(projectId, "revoke", revisionId, null, input.Reason, token);
    private async Task<IActionResult> Execute(Guid project, string action, Guid? resource,
        RepairPolicyDefinitionInput? input, string? reason, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1)
            return Error(400, "validation_error");
        var result = await service.ExecuteAsync(actor, role, project, action, resource, input, reason,
            Request.Headers["Idempotency-Key"].FirstOrDefault(), Request.Headers.IfMatch.FirstOrDefault(), token);
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Value is not null) Response.Headers.ETag = $"\"{result.Value.Version}\"";
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Repair policy request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
