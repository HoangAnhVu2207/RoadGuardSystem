using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.DTOs.Authentication;
using RoadGuardSystem.DTOs.Invitations;
using RoadGuardSystem.Services.Authentication;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IIdentityOnboardingService _service;

    public InvitationsController(IIdentityOnboardingService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateInvitationRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await _service.CreateInvitationAsync(
            actorUserId, request.Email!, request.Role!, request.ProjectIds!, idempotencyKey!, cancellationToken);
        if (result.Status is IdentityOnboardingStatus.Success or IdentityOnboardingStatus.IdempotentReplay &&
            result.Invitation is not null)
        {
            var response = new InvitationResponseDto(
                result.Invitation.Id,
                result.Invitation.Status,
                result.Invitation.ExpiresAt,
                Convert.ToBase64String(result.Invitation.RowVersion));
            Response.Headers.ETag = $"\"{response.Version}\"";
            return Created($"/api/v1/invitations/{response.Id:D}", response);
        }

        return Map(result);
    }

    [AllowAnonymous]
    [HttpPost("accept")]
    public async Task<IActionResult> Accept(
        AcceptInvitationRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _service.AcceptInvitationAsync(
            request.InvitationToken!, request.DisplayName!, request.Password!, idempotencyKey!, cancellationToken);
        return result.Tokens is null ? Map(result) : Ok(ToTokenResponse(result.Tokens));
    }

    private IActionResult Map(IdentityOnboardingResult result) => result.Status switch
    {
        IdentityOnboardingStatus.Forbidden => Forbid(),
        IdentityOnboardingStatus.NotFound => NotFound(),
        IdentityOnboardingStatus.Conflict or IdentityOnboardingStatus.IdempotencyConflict => Conflict(),
        IdentityOnboardingStatus.DeliveryUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable),
        _ => BadRequest()
    };

    private static AuthTokenResponseDto ToTokenResponse(AuthTokens tokens) => new(
        tokens.AccessToken,
        tokens.RefreshToken,
        "Bearer",
        tokens.ExpiresIn,
        tokens.User.MustChangePassword,
        new AuthActorResponseDto(
            tokens.User.Id,
            tokens.User.DisplayName,
            tokens.User.RoleCode switch
            {
                RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Supervisor => "SUPERVISOR",
                RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.ProjectManager => "PM",
                RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.DroneOperator => "OPERATOR",
                RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.RepairCrew => "CREW",
                _ => throw new ArgumentOutOfRangeException(nameof(tokens), tokens.User.RoleCode, null)
            },
            Convert.ToBase64String(tokens.User.RowVersion)));
}
