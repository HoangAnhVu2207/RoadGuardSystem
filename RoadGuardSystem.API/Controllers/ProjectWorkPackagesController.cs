using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Projects;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/work-package")]
public sealed class ProjectWorkPackagesController : ControllerBase
{
    private readonly IProjectWorkPackageService _service;

    public ProjectWorkPackagesController(IProjectWorkPackageService service)
    {
        _service = service;
    }

    [Authorize(Policy = ProjectAuthorizationPolicies.WorkPackageRead)]
    [HttpGet]
    [ProducesResponseType<ProjectWorkPackageResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken cancellationToken)
    {
        if (HttpContext.Items[ProjectAuthorizationPolicies.AccessScopeItemKey] is not ProjectAccessScope scope)
        {
            return ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.ProjectAccessForbidden, "Forbidden");
        }

        var workPackage = await _service.GetAsync(projectId, scope.AccessRole, cancellationToken);
        return workPackage is null
            ? ProblemResponse(StatusCodes.Status404NotFound, ApiErrorCodes.ProjectNotFound, "Not found")
            : Ok(workPackage);
    }

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status404NotFound
                ? "The requested project was not found."
                : "The current user does not have access to the requested project.",
            Instance = Request.Path,
            Type = status == StatusCodes.Status404NotFound
                ? "https://tools.ietf.org/html/rfc9110#section-15.5.5"
                : "https://tools.ietf.org/html/rfc9110#section-15.5.4"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] =
            HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
