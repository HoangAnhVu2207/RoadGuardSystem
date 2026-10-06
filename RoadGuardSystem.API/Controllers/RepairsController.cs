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
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items")]
public sealed class RepairsController(IRepairWorkflowService service) : ControllerBase
{
    [HttpGet("{itemId:guid}")]
    public Task<IActionResult> Item(Guid projectId, Guid packageId, Guid itemId, CancellationToken ct)
        => Run((actor, role, _, _) => service.ReadItemAsync(actor, role, projectId, packageId, itemId, false, ct), false);

    [HttpGet("{itemId:guid}/history")]
    public Task<IActionResult> History(Guid projectId, Guid packageId, Guid itemId, CancellationToken ct)
        => Run((actor, role, _, _) => service.ReadItemAsync(actor, role, projectId, packageId, itemId, true, ct), false);

    [HttpPost("{itemId:guid}/corrections")]
    public Task<IActionResult> Correct(Guid projectId, Guid packageId, Guid itemId, RepairCorrectionInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.CorrectAsync(actor, role, projectId, packageId, itemId, input, key, version, ct),
            true, $"/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}");

    [HttpPost("{itemId:guid}/review-requests")]
    public Task<IActionResult> RequestReview(Guid projectId, Guid packageId, Guid itemId, RepairReviewRequestInput input, CancellationToken ct)
        => Run((actor, role, key, version) => service.RequestReviewAsync(actor, role, projectId, packageId, itemId, input, key, version, ct), true);

    private async Task<IActionResult> Run(Func<Guid, UserRoleCode, string?, string?, Task<RepairWorkflowResult>> execute,
        bool mutation, string? createdLocation = null)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        string? key = null, version = null;
        if (mutation)
        {
            if (Request.Headers.TryGetValue("Idempotency-Key", out var keys))
            {
                if (keys.Count != 1) return Error(400, "validation_error");
                key = keys.ToString();
            }
            if (Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
            version = Request.Headers.IfMatch.Count == 0 ? null : Request.Headers.IfMatch.ToString();
        }
        var result = await execute(actor, role, key, version);
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Version is not null) Response.Headers.ETag = $"\"{result.Version}\"";
        if (result.Status == 201 && createdLocation is not null) Response.Headers.Location = createdLocation;
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
