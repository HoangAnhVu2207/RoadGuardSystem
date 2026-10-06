using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/safety-measures")]
public sealed class RepairSafetyController(IRepairSafetyService service) : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> Create(Guid projectId, Guid packageId, Guid itemId,
        RepairSafetyCreateInput input, CancellationToken token)
        => Run((actor, role, key, version) => service.CreateAsync(actor, role, projectId, packageId,
            itemId, input, key, version, token), true);

    [HttpGet("{measureId:guid}")]
    public Task<IActionResult> Read(Guid projectId, Guid packageId, Guid itemId, Guid measureId, CancellationToken token)
        => Run((actor, role, _, _) => service.ReadAsync(actor, role, projectId, packageId, itemId,
            measureId, token), false);

    [HttpPost("{measureId:guid}/installation")]
    public Task<IActionResult> Install(Guid projectId, Guid packageId, Guid itemId, Guid measureId,
        RepairSafetyInstallInput input, CancellationToken token)
        => Run((actor, role, key, version) => service.InstallAsync(actor, role, projectId, packageId,
            itemId, measureId, input, key, version, token), true);

    [HttpPost("{measureId:guid}/checks")]
    public Task<IActionResult> Check(Guid projectId, Guid packageId, Guid itemId, Guid measureId,
        RepairSafetyCheckInput input, CancellationToken token)
        => Run((actor, role, key, version) => service.CheckAsync(actor, role, projectId, packageId,
            itemId, measureId, input, key, version, token), true);

    [HttpPost("{measureId:guid}/warnings/{warningId:guid}/acknowledgement")]
    public Task<IActionResult> Acknowledge(Guid projectId, Guid packageId, Guid itemId, Guid measureId,
        Guid warningId, RepairSafetyAcknowledgementInput input, CancellationToken token)
        => Run((actor, role, key, version) => service.AcknowledgeAsync(actor, role, projectId, packageId,
            itemId, measureId, warningId, input, key, version, token), true);

    private async Task<IActionResult> Run(Func<Guid, UserRoleCode, string?, string?, Task<RepairSafetyServiceResult>> action,
        bool write)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1)
            return Error(400, "validation_error");
        var result = await action(actor, role, write ? Request.Headers["Idempotency-Key"].FirstOrDefault() : null,
            write ? Request.Headers.IfMatch.FirstOrDefault() : null);
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Value is not null) Response.Headers.ETag = $"\"{result.Value.Version}\"";
        return StatusCode(result.Status, result.Value);
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Repair safety request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
