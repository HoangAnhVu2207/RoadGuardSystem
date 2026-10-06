using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/tasks/{taskId:guid}")]
public sealed class RepairExecutionController(IRepairExecutionService service) : ControllerBase
{
    [HttpPost("assessment")]
    public Task<IActionResult> Assess(Guid projectId, Guid packageId, Guid itemId, Guid taskId,
        RepairMeasurementAssessmentInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.AssessAsync(actor, role, projectId, packageId, itemId, taskId, input, key, version, ct));
    [HttpPost("execution-start")]
    public Task<IActionResult> Start(Guid projectId, Guid packageId, Guid itemId, Guid taskId,
        RepairExecutionStartInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.StartExecutionAsync(actor, role, projectId, packageId, itemId, taskId, input, key, version, ct));
    [HttpPost("execution-finish")]
    public Task<IActionResult> Finish(Guid projectId, Guid packageId, Guid itemId, Guid taskId,
        RepairExecutionFinishInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.FinishExecutionAsync(actor, role, projectId, packageId, itemId, taskId, input, key, version, ct));
    [HttpPost("attempts")]
    public Task<IActionResult> SubmitAttempt(Guid projectId, Guid packageId, Guid itemId, Guid taskId,
        RepairAttemptSubmitInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.SubmitAttemptAsync(actor, role, projectId, packageId, itemId, taskId,
            input, key, version, ct));
    [HttpPost("attempts/supplements")]
    public Task<IActionResult> SupplementAttempt(Guid projectId, Guid packageId, Guid itemId, Guid taskId,
        RepairAttemptSupplementInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.SupplementAttemptAsync(actor, role, projectId, packageId, itemId,
            taskId, input, key, version, ct));
    [HttpPost("attempts/reviews")]
    public Task<IActionResult> ReviewAttempt(Guid projectId, Guid packageId, Guid itemId, Guid taskId,
        RepairAttemptReviewInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.ReviewAttemptAsync(actor, role, projectId, packageId, itemId,
            taskId, input, key, version, ct));
    private async Task<IActionResult> Run(Func<Guid, UserRoleCode, string?, string?, Task<RepairWorkflowResult>> execute)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        string? key = null;
        if (Request.Headers.TryGetValue("Idempotency-Key", out var keys))
        {
            if (keys.Count != 1) return Error(400, "validation_error");
            key = keys.ToString();
        }
        if (Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
        var version = Request.Headers.IfMatch.Count == 0 ? null : Request.Headers.IfMatch.ToString();
        var result = await execute(actor, role, key, version);
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Version is not null) Response.Headers.ETag = $"\"{result.Version}\"";
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Repair request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
