using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Services.Cases;
namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0")]
public sealed class PublicPublicationsController(IPublicPublicationService service) : ControllerBase
{
    [Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible, HttpPost("api/v{version:apiVersion}/projects/{projectId:guid}/defects/{defectId:guid}/publications")]
    [ProducesResponseType(typeof(PublicPublicationDto), 201)]
    public Task<IActionResult> Create(Guid projectId, Guid defectId, CancellationToken ct) => Write(projectId, defectId, false, ct);
    [Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible, HttpPost("api/v{version:apiVersion}/projects/{projectId:guid}/publications/{publicId:guid}/withdraw")]
    [ProducesResponseType(typeof(PublicPublicationDto), 200)]
    public Task<IActionResult> Withdraw(Guid projectId, Guid publicId, CancellationToken ct) => Write(projectId, publicId, true, ct);
    [AllowAnonymous, HttpGet("api/v{version:apiVersion}/public/publications/{publicId:guid}")]
    [ProducesResponseType(typeof(PublicPublicationDto), 200)]
    public async Task<IActionResult> Read(Guid publicId, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var value = await service.ReadAsync(publicId, ct); return value is null ? NotFound() : Ok(value);
    }
    private async Task<IActionResult> Write(Guid project, Guid target, bool withdraw, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor)) return Unauthorized();
        UserRoleCode role;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); } catch (ArgumentOutOfRangeException) { return Forbid(); }
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key)) return StatusCode(428);
        var result = withdraw ? await service.WithdrawAsync(actor, role, project, target, key.ToString(), ct) : await service.CreateAsync(actor, role, project, target, key.ToString(), ct);
        if (result.Publication is null) { var error = new ProblemDetails { Status = result.Status, Title = "Publication request failed" }; error.Extensions["code"] = result.Code; return StatusCode(result.Status, error); }
        return result.Status == 201 ? Created($"/api/v1/public/publications/{result.Publication.PublicId}", result.Publication) : Ok(result.Publication);
    }
}
