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
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages")]
public sealed class RepairPackagesController(IRepairProducerService service, IRepairExecutionService execution) : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> Create(Guid projectId, RepairPackageCreateInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.CreatePackageAsync(actor, role, projectId, input, key, version, ct));

    [HttpPost("{packageId:guid}/items")]
    public Task<IActionResult> Propose(Guid projectId, Guid packageId, RepairItemProposeInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.ProposeItemAsync(actor, role, projectId, packageId, input, key, version, ct));

    [HttpPost("{packageId:guid}/items/{itemId:guid}/approve")]
    public Task<IActionResult> Approve(Guid projectId, Guid packageId, Guid itemId, RepairDecisionInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.ApproveItemAsync(actor, role, projectId, packageId, itemId, input, key, version, ct));

    [HttpPost("{packageId:guid}/items/{itemId:guid}/assign")]
    public Task<IActionResult> Assign(Guid projectId, Guid packageId, Guid itemId, RepairItemAssignInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.AssignItemAsync(actor, role, projectId, packageId, itemId, input, key, version, ct));

    [HttpPost("{packageId:guid}/items/{itemId:guid}/final-confirmation")]
    public Task<IActionResult> ConfirmFinal(Guid projectId, Guid packageId, Guid itemId, RepairDecisionInput input,
        CancellationToken ct)
        => Run((actor, role, key, version) => execution.ConfirmFinalAsync(actor, role, projectId, packageId, itemId,
            input, key, version, ct));

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
