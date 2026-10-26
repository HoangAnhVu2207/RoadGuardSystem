using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using RoadGuardSystem.API.Controllers;
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
        if (!authorizeResult.Succeeded && context.GetEndpoint()?.Metadata
                .GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.AsType() == typeof(NotificationSupervisorController))
            context.Response.Headers.CacheControl = "private, no-store";

        // The adopted collection profile applies only to these additive endpoints.
        // Existing controllers retain their existing challenge/forbidden contracts.
        if (!authorizeResult.Succeeded && context.GetEndpoint()?.Metadata
                .GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.AsType() == typeof(DiscoveryCollectionsController))
        {
            var status = authorizeResult.Challenged ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden;
            var collectionProblem = new ProblemDetails
            {
                Type = "about:blank",
                Title = "Collection request failed",
                Status = status,
                Instance = context.Request.Path
            };
            collectionProblem.Extensions["code"] = status == 401 ? "unauthorized" : "access_forbidden";
            collectionProblem.Extensions["correlationId"] =
                context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? context.TraceIdentifier;
            context.Response.StatusCode = status;
            context.Response.Headers.CacheControl = "private, no-store";
            await context.Response.WriteAsJsonAsync(collectionProblem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
            return;
        }
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
