using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
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

    private ObjectResult Map(IdentityOnboardingResult result) => result.Status switch
    {
        IdentityOnboardingStatus.Forbidden => ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
        IdentityOnboardingStatus.NotFound => ProblemResponse(StatusCodes.Status404NotFound, ApiErrorCodes.NotFound, "Not found"),
        IdentityOnboardingStatus.Conflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.EmailConflict, "Conflict"),
        IdentityOnboardingStatus.IdempotencyConflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
        IdentityOnboardingStatus.DeliveryUnavailable => ProblemResponse(StatusCodes.Status503ServiceUnavailable, ApiErrorCodes.InternalError, "Delivery unavailable"),
        _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad request")
    };

    private ObjectResult ProblemResponse(int status, string code, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status503ServiceUnavailable => "Service unavailable",
                _ => "Request failed"
            },
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
