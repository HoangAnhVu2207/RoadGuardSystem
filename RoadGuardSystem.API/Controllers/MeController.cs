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
[Route("api/v{version:apiVersion}/me")]
public sealed class MeController : ControllerBase
{
    private readonly IIdentityV2Service _identityService;

    public MeController(IIdentityV2Service identityService)
    {
        _identityService = identityService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await _identityService.GetMeAsync(userId, cancellationToken));
    }

    [HttpPatch]
    public async Task<IActionResult> Update(
        UpdateMeRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var userId))
        {
            return Unauthorized();
        }

        return Map(await _identityService.UpdateMeAsync(
            userId,
            request.DisplayName!,
            ifMatch,
            idempotencyKey!,
            cancellationToken: cancellationToken));
    }

    private IActionResult Map(IdentityV2Result result) => result.Status switch
    {
        IdentityV2Status.Success or IdentityV2Status.IdempotentReplay when result.Actor is not null =>
            Ok(ToResponse(result.Actor)),
        IdentityV2Status.PreconditionRequired => StatusCode(StatusCodes.Status428PreconditionRequired),
        IdentityV2Status.PreconditionFailed => StatusCode(StatusCodes.Status412PreconditionFailed),
        IdentityV2Status.IdempotencyConflict => Conflict(),
        IdentityV2Status.NotFound => NotFound(),
        _ => BadRequest()
    };

    private bool TryGetActorId(out Guid userId) =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);

    private static ActorResponseDto ToResponse(IdentityV2Actor actor) => new(
        actor.Id,
        actor.DisplayName,
        ToV2Role(actor.RoleCode),
        Convert.ToBase64String(actor.RowVersion));

    private static string ToV2Role(RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role) => role switch
    {
        RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Supervisor => "SUPERVISOR",
        RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.ProjectManager => "PM",
        RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.DroneOperator => "OPERATOR",
        RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.RepairCrew => "CREW",
        RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Reporter => "REPORTER",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}
