using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Authentication;
using RoadGuardSystem.Services.Authentication;

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            new LoginCommand(
                request.Username!,
                request.Password!,
                request.DeviceMetadata?.GetRawText()),
            cancellationToken);
        return MapResult(result);
    }

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
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("sid"), out var sessionId))
        {
            return AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _authService.LogoutAsync(sessionId, cancellationToken);
        return MapResult(result, noContentOnSuccess: true);
    }

    private IActionResult MapResult(AuthResult result, bool noContentOnSuccess = false) => result.Status switch
    {
        AuthStatus.Success when noContentOnSuccess => NoContent(),
        AuthStatus.Success => Ok(new AuthTokenResponseDto(
            result.Tokens!.AccessToken,
            result.Tokens.RefreshToken,
            result.Tokens.AccessTokenExpiresAt,
            result.Tokens.RefreshTokenExpiresAt)),
        AuthStatus.InvalidInput => AuthProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request"),
        AuthStatus.InvalidCredentials => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.InvalidCredentials, "Unauthorized"),
        AuthStatus.PasswordChangeRequired => AuthProblem(StatusCodes.Status403Forbidden, ApiErrorCodes.PasswordChangeRequired, "Password change required"),
        AuthStatus.PasswordPolicyRejected => AuthProblem(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Password policy rejected"),
        AuthStatus.NotRequired => AuthProblem(StatusCodes.Status409Conflict, ApiErrorCodes.ConcurrencyConflict, "Password change is not required"),
        AuthStatus.SessionRevoked => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.SessionRevoked, "Session revoked"),
        AuthStatus.Conflict => AuthProblem(StatusCodes.Status409Conflict, ApiErrorCodes.ConcurrencyConflict, "Conflict"),
        _ => AuthProblem(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized")
    };

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
}
