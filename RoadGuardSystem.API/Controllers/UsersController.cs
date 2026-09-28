using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        if (result.Status is IdentityV2Status.Success or IdentityV2Status.IdempotentReplay && result.Actor is not null)
        {
            return Ok(ToResponse(result.Actor));
        }

        return result.Status switch
        {
            IdentityV2Status.PreconditionRequired => StatusCode(StatusCodes.Status428PreconditionRequired),
            IdentityV2Status.PreconditionFailed => StatusCode(StatusCodes.Status412PreconditionFailed),
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

    private IActionResult Map(IdentityV2Result result) => result.Status switch
    {
        IdentityV2Status.Forbidden => Forbid(),
        IdentityV2Status.NotFound => NotFound(),
        IdentityV2Status.IdempotencyConflict => Conflict(),
        IdentityV2Status.Conflict => Conflict(),
        _ => BadRequest()
    };

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
