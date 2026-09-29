using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Identity;
using RoadGuardSystem.Services.Identity;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IIdentityV2Service _identityService;

    public UsersController(IIdentityV2Service identityService)
    {
        _identityService = identityService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await _identityService.GetAccountAsync(actorUserId, userId, cancellationToken);
        if (result.Status == IdentityV2Status.Success && result.Actor is not null)
        {
            Response.Headers.ETag = $"\"{Convert.ToBase64String(result.Actor.RowVersion)}\"";
            return Ok(ToResponse(result.Actor));
        }

        return Map(result);
    }

    [HttpPatch("{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        AccountUpdateRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await _identityService.UpdateAccountAsync(
            actorUserId,
            userId,
            request.Status!,
            request.Role!,
            request.Reason!,
            ifMatch,
            idempotencyKey!,
            cancellationToken: cancellationToken);
        if (result.Actor is not null && (result.Status is IdentityV2Status.Success or IdentityV2Status.IdempotentReplay))
        {
            Response.Headers.ETag = $"\"{Convert.ToBase64String(result.Actor.RowVersion)}\"";
            return Ok(ToResponse(result.Actor));
        }

        return result.Status switch
        {
            IdentityV2Status.PreconditionRequired => ProblemResponse(StatusCodes.Status428PreconditionRequired, ApiErrorCodes.ValidationError, "Required precondition headers are missing"),
            IdentityV2Status.PreconditionFailed => ProblemResponse(StatusCodes.Status412PreconditionFailed, ApiErrorCodes.ConcurrencyConflict, "Precondition failed"),
            _ => Map(result)
        };
    }

    [HttpPost("{userId:guid}/password-reset")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        AdminResetPasswordV2RequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await _identityService.ResetPasswordAsync(
            actorUserId,
            userId,
            request.TemporaryPassword!,
            request.Reason!,
            idempotencyKey!,
            cancellationToken: cancellationToken);
        return result.Status switch
        {
            IdentityV2Status.Success or IdentityV2Status.IdempotentReplay => NoContent(),
            _ => Map(result)
        };
    }

    private ObjectResult Map(IdentityV2Result result) => result.Status switch
    {
        IdentityV2Status.Forbidden => ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
        IdentityV2Status.NotFound => ProblemResponse(StatusCodes.Status404NotFound, ApiErrorCodes.IdentityUserNotFound, "Not found"),
        IdentityV2Status.IdempotencyConflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
        IdentityV2Status.Conflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.ConcurrencyConflict, "Conflict"),
        _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad request")
    };

    private ObjectResult ProblemResponse(int status, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status403Forbidden ? "Forbidden" : status == StatusCodes.Status409Conflict ? "Conflict" : "Request failed",
            Detail = detail,
            Instance = Request.Path
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

    private bool TryGetActorId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);

    private static ActorResponseDto ToResponse(IdentityV2Actor actor) => new(
        actor.Id,
        actor.DisplayName,
        actor.RoleCode switch
        {
            RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Supervisor => "SUPERVISOR",
            RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.ProjectManager => "PM",
            RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.DroneOperator => "OPERATOR",
            RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.RepairCrew => "CREW",
            RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Reporter => "REPORTER",
            _ => throw new ArgumentOutOfRangeException(nameof(actor), actor.RoleCode, null)
        },
        Convert.ToBase64String(actor.RowVersion));
}
