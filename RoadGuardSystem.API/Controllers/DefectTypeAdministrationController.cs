using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Services.Implementations.Defects;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible]
[Route("api/v{version:apiVersion}/catalog/defect-types/admin")]
public sealed class DefectTypeAdministrationController(DefectTypeAdminService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DefectTypeAdminDto[]>(200)]
    public async Task<IActionResult> List(CancellationToken token)
        => Actor(out var actor, out var role) ? Render(await service.ListAsync(actor, role, token)) : Error(401, "unauthorized");

    [HttpPost]
    [ProducesResponseType<DefectTypeAdminDto>(201)]
    [ProducesResponseType<DefectTypeAdminDto>(200)]
    public Task<IActionResult> Create(CreateDefectTypeDto input,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken token)
        => Write(input.Code, new(input.Name, input.Description, input.IsActive, input.Reason), key, null, true, token);

    [HttpPut("{code}")]
    [ProducesResponseType<DefectTypeAdminDto>(200)]
    public Task<IActionResult> Update(string code, UpdateDefectTypeDto input,
        [FromHeader(Name = "Idempotency-Key")] string? key, [FromHeader(Name = "If-Match")] string? version,
        CancellationToken token)
        => Write(code, input, key, version, false, token);

    private async Task<IActionResult> Write(string code, UpdateDefectTypeDto input, string? key, string? etag,
        bool creating, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1)
            return Error(400, "validation_error");

        return Render(await service.WriteAsync(actor, role, code, input.Name, input.Description, input.IsActive,
            input.Reason, key, etag, creating, token));
    }

    private IActionResult Render(DefectTypeAdminResponse result)
    {
        if (result.Code is { } code) return Error(result.Status, code);
        Response.Headers.CacheControl = "private, no-store";
        if (result.Item is { } item)
        {
            Response.Headers.ETag = $"\"{item.Version}\"";
            return StatusCode(result.Status, item);
        }
        return Ok(result.Items);
    }

    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor) &&
            ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role);
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Defect catalogue request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey];
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}