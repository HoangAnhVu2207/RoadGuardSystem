using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Staff;
using RoadGuardSystem.Services.Staff;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}")]
public sealed class StaffDirectoryController(IStaffReadService service) : ControllerBase
{
    [HttpGet("users")]
    [WebCookieEligible]
    [ProducesResponseType<StaffPageResponseDto<StaffUserResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Users([FromQuery] string? search, [FromQuery] string? role, [FromQuery] string? status,
        [FromQuery] int limit = 20, [FromQuery] string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var actorRole)) return Unauthorized();
        return Map(await service.UsersAsync(actor, actorRole, search, role, status, limit, cursor, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/members")]
    [WebCookieEligible]
    [ProducesResponseType<StaffPageResponseDto<ProjectMemberResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Members(Guid projectId, [FromQuery] string? search, [FromQuery] string? role,
        [FromQuery] string? status, [FromQuery] bool includeHistory = false, [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var actorRole)) return Unauthorized();
        return Map(await service.MembersAsync(actor, actorRole, projectId, search, role, status, includeHistory, limit, cursor, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/primary-project-manager")]
    [WebCookieEligible]
    [ProducesResponseType<PrimaryProjectManagerResponseDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Primary(Guid projectId, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var actorRole)) return Unauthorized();
        return Map(await service.PrimaryAsync(actor, actorRole, projectId, cancellationToken));
    }

    private bool Actor(out Guid actor, out RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role)
    {
        var validId = Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor);
        return ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role) && validId;
    }

    private IActionResult Map<T>(StaffReadResult<T> result)
    {
        if (result.Status == StaffReadStatus.Success) return Ok(result.Value);
        var (status, code, title) = result.Status switch
        {
            StaffReadStatus.Forbidden => (403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            StaffReadStatus.NotFound => (404, ApiErrorCodes.ProjectNotFound, "Not found"),
            _ => (400, ApiErrorCodes.ValidationError, "Invalid request")
        };
        var problem = new ProblemDetails { Status = status, Title = title, Detail = title, Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
