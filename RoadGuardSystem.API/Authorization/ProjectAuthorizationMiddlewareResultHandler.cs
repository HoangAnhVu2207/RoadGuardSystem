using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;

namespace RoadGuardSystem.API.Authorization;

public sealed class ProjectAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden &&
            context.Items.ContainsKey(ProjectAuthorizationPolicies.AccessDeniedItemKey))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = "The current user does not have access to the requested project.",
                Instance = context.Request.Path,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            };
            problem.Extensions["code"] = ApiErrorCodes.ProjectAccessForbidden;
            problem.Extensions["correlationId"] =
                context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
            return;
        }

        await _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
