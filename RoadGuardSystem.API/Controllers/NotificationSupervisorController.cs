using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/notification-supervisor")]
public sealed class NotificationSupervisorController(INotificationSupervisorService service) : ControllerBase
{
    [HttpGet, ProducesResponseType(typeof(NotificationSupervisorDto), 200)]
    public Task<IActionResult> Get(Guid projectId, Guid packageId, Guid itemId, CancellationToken ct) => Execute(projectId, packageId, itemId, ct);
    [HttpPut, ProducesResponseType(typeof(NotificationSupervisorDto), 201), ProducesResponseType(typeof(NotificationSupervisorDto), 200)]
    public Task<IActionResult> Put(Guid projectId, Guid packageId, Guid itemId, CancellationToken ct) => Execute(projectId, packageId, itemId, ct);
    [HttpDelete, ProducesResponseType(204)]
    public Task<IActionResult> Delete(Guid projectId, Guid packageId, Guid itemId, CancellationToken ct) => Execute(projectId, packageId, itemId, ct);

    private async Task<IActionResult> Execute(Guid project, Guid package, Guid item, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor)) return Unauthorized();
        UserRoleCode role;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); }
        catch (ArgumentOutOfRangeException) { return Forbid(); }
        // Do not let ApiController body binding precede the exact current authority/resource guard.
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        string? Header(string name) => Request.Headers.TryGetValue(name, out var value) ? value.ToString() : null;
        var result = await service.ExecuteAsync(new(actor, role, project, package, item, Request.Method,
            Header("Idempotency-Key"), Header("If-Match"), Header("If-None-Match"), body), ct);
        if (result.Version is not null) Response.Headers.ETag = '"' + result.Version + '"';
        if (result.Status == 201) Response.Headers.Location = $"/api/v1/projects/{project}/repair-packages/{package}/items/{item}/notification-supervisor";
        if (result.Code is null)
        {
            var value = result.Value;
            return result.Status == 204 ? NoContent() : StatusCode(result.Status, value is null ? null :
                new NotificationSupervisorDto(value.AssignmentId, value.SupervisorId, value.Status, value.AssignedAt, value.RevokedAt, value.Version));
        }
        var error = new ProblemDetails { Status = result.Status, Title = "Notification assignment request failed" };
        error.Extensions["code"] = result.Code; error.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return StatusCode(result.Status, error);
    }
}
