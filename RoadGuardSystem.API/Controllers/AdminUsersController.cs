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
[Route("api/v{version:apiVersion}/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AdminUsersController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [Authorize]
    [HttpPost("{userId:guid}/password-reset")]
    [ProducesResponseType<AdminPasswordResetResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        AdminPasswordResetRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _identityService.ResetPasswordAsync(
            actorUserId,
            userId,
            new AdminPasswordResetCommand(
                request.ExpectedTargetRowVersion,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            AdminPasswordResetServiceStatus.Success or AdminPasswordResetServiceStatus.IdempotentReplay => Ok(
                new AdminPasswordResetResponseDto(
                    result.TargetUserId,
                    result.MustChangePassword,
                    result.TemporaryPassword)),
            AdminPasswordResetServiceStatus.ActorNotAuthorized => ProblemResponse(
                StatusCodes.Status403Forbidden,
                ApiErrorCodes.AccessForbidden,
                "Forbidden"),
            AdminPasswordResetServiceStatus.UserNotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                ApiErrorCodes.IdentityUserNotFound,
                "Not found"),
            AdminPasswordResetServiceStatus.TargetNotActive => ProblemResponse(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.IdentityUserInactive,
                "Invalid user state"),
            AdminPasswordResetServiceStatus.StaleConcurrency => ProblemResponse(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.ConcurrencyConflict,
                "Conflict"),
            AdminPasswordResetServiceStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.DuplicateRequest,
                "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The password reset request could not be completed.",
            Instance = Request.Path,
            Type = status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
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
