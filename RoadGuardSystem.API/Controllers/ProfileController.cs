using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Identity;
using RoadGuardSystem.Services.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public ProfileController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType<ProfileResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
        {
            return UnauthorizedProblem();
        }

        var profile = await _identityService.GetProfileAsync(userId, cancellationToken);
        return profile is null
            ? UnauthorizedProblem()
            : Ok(new ProfileResponseDto(
                profile.UserId,
                profile.Username,
                profile.DisplayName,
                profile.Email,
                profile.RoleCode.ToDbCode(),
                profile.Status.ToString().ToUpperInvariant(),
                Convert.ToBase64String(profile.RowVersion)));
    }

    [Authorize]
    [HttpPut]
    [ProducesResponseType<ProfileResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Update(
        ProfileUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
        {
            return UnauthorizedProblem();
        }

        var result = await _identityService.UpdateProfileAsync(
            userId,
            new UpdateProfileCommand(
                request.DisplayName,
                request.Email,
                request.ExpectedRowVersion,
                request.OperationId,
                CorrelationId(),
                request.ExtraFields is { Count: > 0 }),
            cancellationToken);

        return result.Status switch
        {
            ProfileUpdateStatus.Success or ProfileUpdateStatus.IdempotentReplay when result.Profile is not null =>
                Ok(ToResponse(result.Profile)),
            ProfileUpdateStatus.EmailConflict => ProfileProblem(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.EmailConflict,
                "Email conflict"),
            ProfileUpdateStatus.IdempotentConflict => ProfileProblem(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.DuplicateRequest,
                "Duplicate request"),
            ProfileUpdateStatus.StaleConcurrency => ProfileProblem(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.ConcurrencyConflict,
                "Conflict"),
            ProfileUpdateStatus.UserNotFound => UnauthorizedProblem(),
            _ => ProfileProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    private static ProfileResponseDto ToResponse(ProfileView profile) => new(
        profile.UserId,
        profile.Username,
        profile.DisplayName,
        profile.Email,
        profile.RoleCode.ToDbCode(),
        profile.Status.ToString().ToUpperInvariant(),
        Convert.ToBase64String(profile.RowVersion));

    private ObjectResult UnauthorizedProblem()
        => ProfileProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");

    private ObjectResult ProfileProblem(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status401Unauthorized
                ? "Authentication is required or the authoritative user is no longer available."
                : "The profile request could not be completed.",
            Instance = Request.Path,
            Type = status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                _ => "about:blank"
            }
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
            ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private Guid? CorrelationId() =>
        Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value)
            ? value
            : null;
}
