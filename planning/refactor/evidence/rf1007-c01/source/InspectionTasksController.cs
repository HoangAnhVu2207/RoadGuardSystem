using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Services.Inspections;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/inspection-tasks")]
public sealed class InspectionTasksController : ControllerBase
{
    private readonly IInspectionTaskQueryService _service;

    public InspectionTasksController(IInspectionTaskQueryService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType<InspectionTaskPageResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> List(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var actorUserId, out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.ListAssignedAsync(
            actorUserId,
            actorRole,
            cursor,
            limit,
            cancellationToken);

        return result.Status switch
        {
            InspectionTaskQueryStatus.Success => Ok(result.Page),
            InspectionTaskQueryStatus.Forbidden => ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            InspectionTaskQueryStatus.InvalidCursor or InspectionTaskQueryStatus.InvalidInput => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Invalid request"),
            _ => ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized")
        };
    }

    private bool TryGetActor(out Guid actorUserId, out UserRoleCode actorRole)
    {
        actorUserId = Guid.Empty;
        actorRole = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actorUserId))
        {
            return false;
        }

        try
        {
            actorRole = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty);
            return actorRole != UserRoleCode.Unknown;
        }
        catch (ArgumentOutOfRangeException)
        {
            actorRole = UserRoleCode.Unknown;
            return false;
        }
    }

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status400BadRequest
                ? "The request is invalid."
                : "The current user does not have access to the requested resource.",
            Instance = Request.Path,
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{(status == 400 ? "1" : status == 401 ? "2" : "4")}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] =
            HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
