using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Authentication;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthTokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            new LoginCommand(
                request.Email!,
                request.Password!),
            cancellationToken);
        return MapResult(result);
    }

    [AllowAnonymous]
    [HttpPost("android/login")]
    public async Task<IActionResult> AndroidLogin(LoginRequestDto request, CancellationToken cancellationToken)
        => MapResult(await _authService.LoginAsync(
            new LoginCommand(request.Email!, request.Password!, SessionTransport.Android), cancellationToken));

    [AllowAnonymous]
    [HttpPost("android/refresh")]
    public async Task<IActionResult> AndroidRefresh(RefreshRequestDto request, CancellationToken cancellationToken)
        => MapResult(await _authService.RefreshAsync(
            new RefreshCommand(request.RefreshToken!, CorrelationId(), SessionTransport.Android), cancellationToken));

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthTokenResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Refresh(RefreshRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(
            new RefreshCommand(request.RefreshToken!, CorrelationId()),
            cancellationToken);
        return MapResult(result);
    }

    [AllowAnonymous]
    [HttpPost("forced-password-change")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ForcedPasswordChange(
        ForcedPasswordChangeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.CompleteForcedPasswordChangeAsync(
            new ForcedPasswordChangeCommand(
                request.Username!,
                request.CurrentPassword!,
                request.NewPassword!,
                request.ConfirmPassword!,
                request.OperationId,
                CorrelationId()),
            cancellationToken);
        return MapResult(result, noContentOnSuccess: true);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Logout(
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ||
            !Guid.TryParse(User.FindFirstValue("sid"), out var sessionId))
        {
            return AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return AuthProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request");
        }

        var result = await _authService.LogoutAsync(
            userId,
            sessionId,
            idempotencyKey,
            CorrelationId(),
            cancellationToken);
        return MapResult(result, noContentOnSuccess: true);
    }

    [AllowAnonymous]
    [HttpPost("password-recovery-requests")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> RequestPasswordRecovery(
        PasswordRecoveryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RequestPasswordRecoveryAsync(
            new PasswordRecoveryCommand(request.Email!, CorrelationId()),
            cancellationToken);
        return Accepted($"/api/v1/auth/password-recovery-requests/{result.RequestId:D}");
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
        {
            return AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return AuthProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request");
        }

        var result = await _authService.ChangePasswordAsync(
            new ChangePasswordCommand(
                userId,
                request.CurrentPassword!,
                request.NewPassword!,
                idempotencyKey,
                CorrelationId()),
            cancellationToken);
        return MapResult(result, noContentOnSuccess: true);
    }

    private IActionResult MapResult(AuthResult result, bool noContentOnSuccess = false)
    {
        if (result.Status == AuthStatus.Success && !noContentOnSuccess)
            Response.Headers.CacheControl = "no-store";
        return result.Status switch
    {
        AuthStatus.Success when noContentOnSuccess => NoContent(),
        AuthStatus.Success => Ok(new AuthTokenResponseDto(
            result.Tokens!.AccessToken,
            result.Tokens.RefreshToken,
            "Bearer",
            result.Tokens.ExpiresIn,
            result.Tokens.User.MustChangePassword,
            new AuthActorResponseDto(
                result.Tokens.User.Id,
                result.Tokens.User.DisplayName,
                ToV2Role(result.Tokens.User.RoleCode),
                Convert.ToBase64String(result.Tokens.User.RowVersion)))),
        AuthStatus.InvalidInput => AuthProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request"),
        AuthStatus.InvalidCredentials => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.InvalidCredentials, "Unauthorized"),
        AuthStatus.PasswordChangeRequired => AuthProblem(StatusCodes.Status403Forbidden, ApiErrorCodes.PasswordChangeRequired, "Password change required"),
        AuthStatus.PasswordPolicyRejected => AuthProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Password policy rejected"),
        AuthStatus.NotRequired => AuthProblem(StatusCodes.Status409Conflict, ApiErrorCodes.ConcurrencyConflict, "Password change is not required"),
        AuthStatus.SessionRevoked => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.SessionRevoked, "Session revoked"),
        AuthStatus.RefreshTokenInvalid => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.RefreshTokenInvalid, "Invalid refresh token"),
        AuthStatus.RefreshTokenExpired => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.RefreshTokenExpired, "Expired refresh token"),
        AuthStatus.IdempotentConflict => AuthProblem(StatusCodes.Status409Conflict, ApiErrorCodes.IdempotencyKeyReused, "Idempotency key reused"),
        AuthStatus.Conflict => AuthProblem(StatusCodes.Status409Conflict, ApiErrorCodes.ConcurrencyConflict, "Conflict"),
        _ => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized")
        };
    }

    private ObjectResult AuthProblem(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The authentication request could not be completed.",
            Instance = Request.Path,
            Type = status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
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

    private static string ToV2Role(UserRoleCode roleCode) => roleCode switch
    {
        UserRoleCode.Supervisor => "SUPERVISOR",
        UserRoleCode.ProjectManager => "PM",
        UserRoleCode.DroneOperator => "OPERATOR",
        UserRoleCode.RepairCrew => "CREW",
        UserRoleCode.Reporter => "REPORTER",
        _ => throw new ArgumentOutOfRangeException(nameof(roleCode), roleCode, "Role is not supported by the V2 actor contract.")
    };
}
