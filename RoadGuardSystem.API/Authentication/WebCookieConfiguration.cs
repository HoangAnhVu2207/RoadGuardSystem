using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Services.Authentication;

namespace RoadGuardSystem.API.Authentication;

internal static class WebCookieConfiguration
{
    internal const string Scheme = "RoadGuardWeb";
    internal const string UserScheme = "RoadGuardUser";
    internal const string CookieName = "__Host-RoadGuardSession";

    internal static string SelectScheme(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("Authorization"))
            return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        return IsCookieEligiblePath(context.Request.Path) && context.Request.Cookies.ContainsKey(CookieName)
            ? Scheme
            : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    }

    internal static bool IsCookieEligiblePath(PathString requestPath)
    {
        var path = requestPath.Value ?? "";
        return path.Equals("/api/v1/me", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/profile", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/v1/auth/change-password", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/reports", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/v1/cases", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/candidate-decisions", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/candidates", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/labels", StringComparison.OrdinalIgnoreCase);
    }

    internal static void Configure(CookieAuthenticationOptions options)
    {
        options.Cookie.Name = CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.SlidingExpiration = false;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var claims = context.Principal;
                if (!Guid.TryParse(claims?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ||
                    !Guid.TryParse(claims?.FindFirstValue("sid"), out var sessionId) ||
                    !TryRole(claims?.FindFirstValue("role"), out var role))
                {
                    context.RejectPrincipal();
                    return;
                }
                var validator = context.HttpContext.RequestServices.GetRequiredService<AuthoritativeSessionValidator>();
                var result = await validator
                    .ValidateAsync(userId, sessionId, role, DateTimeOffset.UtcNow,
                        SessionTransport.Web, context.HttpContext.RequestAborted);
                if (result == AuthoritativeSessionValidation.MustChangePassword &&
                    PasswordChangeAllowed(context.Request.Method, context.Request.Path)) return;
                if (result == AuthoritativeSessionValidation.SessionRevoked &&
                    HttpMethods.IsPost(context.Request.Method) &&
                    context.Request.Path.Value?.EndsWith("/auth/web/logout", StringComparison.OrdinalIgnoreCase) == true &&
                    context.Request.Headers.TryGetValue("Idempotency-Key", out var key) &&
                    !string.IsNullOrWhiteSpace(key.ToString()) &&
                    await validator.HasCommittedReplayAsync(userId, "Logout", key.ToString().Trim(' '),
                        context.HttpContext.RequestAborted)) return;
                if (result != AuthoritativeSessionValidation.Success) context.RejectPrincipal();
            },
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    }

    private static bool PasswordChangeAllowed(string method, PathString path)
    {
        var value = path.Value ?? "";
        if (HttpMethods.IsGet(method))
            return value.EndsWith("/me", StringComparison.OrdinalIgnoreCase) ||
                   value.EndsWith("/auth/web/session", StringComparison.OrdinalIgnoreCase) ||
                   value.EndsWith("/auth/web/csrf", StringComparison.OrdinalIgnoreCase);
        return HttpMethods.IsPost(method) &&
            (value.EndsWith("/auth/web/logout", StringComparison.OrdinalIgnoreCase) ||
             value.EndsWith("/auth/change-password", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryRole(string? value, out UserRoleCode role)
    {
        try { role = UserRoleCodeExtensions.FromDbCode(value ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { role = UserRoleCode.Unknown; return false; }
    }
}
