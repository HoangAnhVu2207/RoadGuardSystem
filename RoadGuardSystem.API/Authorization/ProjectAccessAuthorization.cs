using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Authorization;

public sealed class ProjectAccessRequirement : IAuthorizationRequirement;

public static class ProjectAuthorizationPolicies
{
    public const string WorkPackageRead = "project_work_package_read";
    public const string AccessScopeItemKey = "RoadGuard.ProjectAccessScope";
    public const string AccessDeniedItemKey = "RoadGuard.ProjectAccessDenied";
}

public sealed class ProjectAccessAuthorizationHandler : AuthorizationHandler<ProjectAccessRequirement>
{
    private readonly IProjectScopeGuard _scopeGuard;

    public ProjectAccessAuthorizationHandler(IProjectScopeGuard scopeGuard)
    {
        _scopeGuard = scopeGuard;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext ||
            !Guid.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ||
            !Guid.TryParse(httpContext.Request.RouteValues["projectId"]?.ToString(), out var projectId) ||
            !TryParseRole(context.User.FindFirstValue("role"), out var role))
        {
            return;
        }

        var scope = await _scopeGuard.AuthorizeAsync(
            userId,
            role,
            projectId,
            httpContext.RequestAborted);
        if (scope is null)
        {
            httpContext.Items[ProjectAuthorizationPolicies.AccessDeniedItemKey] = true;
            return;
        }

        httpContext.Items[ProjectAuthorizationPolicies.AccessScopeItemKey] = scope;
        context.Succeed(requirement);
    }

    private static bool TryParseRole(string? value, out UserRoleCode role)
    {
        try
        {
            role = UserRoleCodeExtensions.FromDbCode(value ?? string.Empty);
            return role != UserRoleCode.Unknown;
        }
        catch (ArgumentOutOfRangeException)
        {
            role = UserRoleCode.Unknown;
            return false;
        }
    }
}
