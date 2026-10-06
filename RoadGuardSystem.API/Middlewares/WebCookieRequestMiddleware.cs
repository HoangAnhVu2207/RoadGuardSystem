using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Middlewares;

public sealed class WebCookieRequestMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (context.Request.Headers.ContainsKey("Authorization") &&
            context.Request.Cookies.ContainsKey(WebCookieConfiguration.CookieName) &&
            WebCookieConfiguration.IsCookieEligibleRequest(context) &&
            context.User.Identity?.IsAuthenticated == true)
        {
            var cookie = await context.AuthenticateAsync(WebCookieConfiguration.Scheme);
            var bearerActor = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var cookieActor = cookie.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (cookie.Succeeded && cookieActor is not null && cookieActor != bearerActor)
            {
                await DenyAsync(context, StatusCodes.Status400BadRequest, "validation_error");
                return;
            }
        }

        if (context.User.Identity?.AuthenticationType != WebCookieConfiguration.Scheme ||
            context.Request.Path.StartsWithSegments("/api/v1/auth/web", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) &&
            !HttpMethods.IsOptions(context.Request.Method))
        {
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException)
            {
                await DenyAsync(context, StatusCodes.Status403Forbidden, "csrf_failed");
                return;
            }
        }

        await next(context);
    }

    private static Task DenyAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = "about:blank",
            title = status switch { 400 => "Bad Request", 403 => "Forbidden", _ => "Unauthorized" },
            status,
            detail = status switch
            {
                400 => "Credentials identify different actors.",
                403 => "The CSRF token is invalid or missing.",
                _ => "The session is no longer active."
            },
            instance = context.Request.Path.Value,
            code,
            correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
        }), context.RequestAborted);
    }
}
