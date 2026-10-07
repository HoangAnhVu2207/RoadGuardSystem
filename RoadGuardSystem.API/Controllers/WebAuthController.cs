using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using RoadGuardSystem.Services.Generators;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Authentication;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/web")]
public sealed class WebAuthController(IAuthService auth, IIdentityRepository identity, IAntiforgery antiforgery, IDataProtectionProvider protection, TimeProvider clock)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public async Task<IActionResult> Csrf()
    {
        var existing = await HttpContext.AuthenticateAsync(WebCookieConfiguration.Scheme);
        if (existing.Succeeded && existing.Principal is not null) HttpContext.User = existing.Principal;
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        NoStore();
        return Ok(new { requestToken = tokens.RequestToken, headerName = "X-CSRF-TOKEN" });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken token)
    {
        NoStore();
        if (!await ValidCsrfAsync()) return ProblemResult(403, "csrf_failed");
        var result = await auth.LoginAsync(new LoginCommand(request.Email!, request.Password!, SessionTransport.Web), token);
        if (result.Status != AuthStatus.Success || result.Tokens is null)
            return ProblemResult(result.Status == AuthStatus.InvalidInput ? 400 : 401,
                result.Status == AuthStatus.InvalidInput ? "validation_error" : "auth_unauthorized");
        var issued = result.Tokens;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, issued.User.Id.ToString()),
            new Claim("sid", issued.SessionId.ToString()),
            new Claim("role", issued.User.RoleCode.ToDbCode())
        };
        await HttpContext.SignInAsync(WebCookieConfiguration.Scheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, WebCookieConfiguration.Scheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = clock.GetUtcNow().AddHours(12) });
        SetRenewalCookie(protection.CreateProtector("RoadGuard.WebRenewal.v1").Protect(issued.RefreshToken));
        return Ok(View(issued.User, issued.SessionIssuedAt, null, null));
    }

    [AllowAnonymous]
    [HttpPost("renew")]
    public async Task<IActionResult> Renew(CancellationToken token)
    {
        NoStore();
        if (Request.Headers.ContainsKey("Authorization")) return ProblemResult(400, "validation_error");
        var existing = await HttpContext.AuthenticateAsync(WebCookieConfiguration.Scheme);
        if (existing.Succeeded && existing.Principal is not null) HttpContext.User = existing.Principal;
        if (!await ValidCsrfAsync()) return ProblemResult(403, "csrf_failed");
        if (!Request.Cookies.TryGetValue(WebCookieConfiguration.RenewalCookieName, out var protectedValue))
            return ProblemResult(401, "auth_session_revoked");
        string credential;
        try { credential = protection.CreateProtector("RoadGuard.WebRenewal.v1").Unprotect(protectedValue); }
        catch (System.Security.Cryptography.CryptographicException) { return ProblemResult(401, "auth_session_revoked"); }
        var hash = RefreshTokenGenerator.Hash(credential);
        if (existing.Succeeded)
        {
            var owner = await identity.FindRefreshTokenByHashAsync(hash, token);
            if (owner is null) return ProblemResult(401, "auth_session_revoked");
            if (existing.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) != owner.UserId.ToString() ||
                existing.Principal?.FindFirstValue("sid") != owner.SessionId.ToString()) return ProblemResult(400, "validation_error");
        }
        var state = await identity.RenewWebSessionAsync(hash, clock.GetUtcNow(), token);
        if (state is null) return ProblemResult(401, "auth_session_revoked");
        if (existing.Succeeded && (existing.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) != state.User.Id.ToString() ||
            existing.Principal?.FindFirstValue("sid") != state.SessionId.ToString())) return ProblemResult(400, "validation_error");
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, state.User.Id.ToString()),
            new Claim("sid", state.SessionId.ToString()), new Claim("role", state.User.RoleCode.ToDbCode()) };
        await HttpContext.SignInAsync(WebCookieConfiguration.Scheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, WebCookieConfiguration.Scheme)),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = clock.GetUtcNow().AddHours(12) });
        SetRenewalCookie(protectedValue);
        return Ok(View(state.User, state.IssuedAt, null, null));
    }

    private void SetRenewalCookie(string value) => Response.Cookies.Append(WebCookieConfiguration.RenewalCookieName,
        value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(365)
        });

    [Authorize(AuthenticationSchemes = WebCookieConfiguration.Scheme)]
    [HttpGet("session")]
    public async Task<IActionResult> Session(CancellationToken token)
    {
        NoStore();
        if (!TryClaims(out var userId, out var sessionId, out var role)) return ProblemResult(401, "auth_unauthorized");
        var state = await identity.TouchWebSessionAsync(userId, sessionId, role, clock.GetUtcNow(),
            allowMustChangePassword: true, cancellationToken: token);
        return state is null ? ProblemResult(401, "auth_session_revoked")
            : Ok(View(state.User, state.IssuedAt, state.AbsoluteExpiresAt, state.IdleExpiresAt));
    }

    [Authorize(AuthenticationSchemes = WebCookieConfiguration.Scheme)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken token)
    {
        NoStore();
        if (!await ValidCsrfAsync()) return ProblemResult(403, "csrf_failed");
        if (!TryClaims(out var userId, out var sessionId, out _)) return ProblemResult(401, "auth_unauthorized");
        var normalized = key?.Trim(' ');
        if (string.IsNullOrEmpty(normalized) || normalized.Length > 200 || normalized.Any(c => c is < ' ' or > '~'))
            return ProblemResult(400, "validation_error");
        var result = await auth.LogoutAsync(userId, sessionId, normalized, cancellationToken: token);
        if (result.Status != AuthStatus.Success) return ProblemResult(409, "idempotency_key_reused");
        await HttpContext.SignOutAsync(WebCookieConfiguration.Scheme);
        Response.Cookies.Delete(WebCookieConfiguration.RenewalCookieName, new CookieOptions { Secure = true, Path = "/" });
        return NoContent();
    }

    private async Task<bool> ValidCsrfAsync()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private bool TryClaims(out Guid userId, out Guid sessionId, out UserRoleCode role)
    {
        sessionId = Guid.Empty;
        var valid = Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId)
            && Guid.TryParse(User.FindFirstValue("sid"), out sessionId);
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); }
        catch (ArgumentOutOfRangeException) { role = UserRoleCode.Unknown; }
        return valid && role != UserRoleCode.Unknown;
    }

    private static object View(UserSecurityState user, DateTimeOffset issuedAt,
        DateTimeOffset? absoluteExpiresAt, DateTimeOffset? idleExpiresAt) => new
        {
            user = new
            {
                id = user.Id,
                displayName = user.DisplayName,
                role = ToV2Role(user.RoleCode),
                version = Convert.ToBase64String(user.RowVersion)
            },
            mustChangePassword = user.MustChangePassword,
            issuedAt,
            absoluteExpiresAt,
            idleExpiresAt,
            renewable = absoluteExpiresAt is null,
            ticketLifetimeSeconds = 43200
        };

    private ObjectResult ProblemResult(int status, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Bad Request", 403 => "Forbidden", 409 => "Conflict", _ => "Unauthorized" },
            Detail = status switch
            {
                403 => "The CSRF token is invalid or missing.",
                400 => "The authentication request is invalid.",
                _ => "The authentication request could not be completed."
            },
            Instance = Request.Path,
            Type = "about:blank"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }

    private void NoStore() => Response.Headers.CacheControl = "no-store";

    private static string ToV2Role(UserRoleCode role) => role switch
    {
        UserRoleCode.Supervisor => "SUPERVISOR",
        UserRoleCode.ProjectManager => "PM",
        UserRoleCode.DroneOperator => "OPERATOR",
        UserRoleCode.RepairCrew => "CREW",
        UserRoleCode.Reporter => "REPORTER",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
