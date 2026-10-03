using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.API.Authentication;

public sealed class WebCookieActivityFilter(IIdentityRepository identity) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.User.Identity?.AuthenticationType == WebCookieConfiguration.Scheme &&
            !http.Request.Path.StartsWithSegments("/api/v1/auth/web", StringComparison.OrdinalIgnoreCase) &&
            !http.Request.Path.Value!.EndsWith("/auth/change-password", StringComparison.OrdinalIgnoreCase) &&
            !HttpMethods.IsOptions(http.Request.Method) &&
            (context.Result as IStatusCodeActionResult)?.StatusCode is null or >= 200 and < 300)
        {
            Guid sessionId = Guid.Empty;
            var valid = Guid.TryParse(http.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) &&
                Guid.TryParse(http.User.FindFirstValue("sid"), out sessionId);
            UserRoleCode role;
            try { role = UserRoleCodeExtensions.FromDbCode(http.User.FindFirstValue("role") ?? ""); }
            catch (ArgumentOutOfRangeException) { role = UserRoleCode.Unknown; }
            var allowedMinimalRead = HttpMethods.IsGet(http.Request.Method) &&
                http.Request.Path.Value?.EndsWith("/me", StringComparison.OrdinalIgnoreCase) == true;
            var state = valid && role != UserRoleCode.Unknown
                ? await identity.TouchWebSessionAsync(userId, sessionId, role, DateTimeOffset.UtcNow,
                    allowMustChangePassword: allowedMinimalRead, cancellationToken: http.RequestAborted)
                : null;
            if (state is null)
            {
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "The session is no longer active.",
                    Instance = http.Request.Path,
                    Type = "about:blank"
                };
                problem.Extensions["code"] = "auth_session_revoked";
                context.Result = new ObjectResult(problem)
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    ContentTypes = { "application/problem+json" }
                };
            }
        }
        await next();
    }
}
