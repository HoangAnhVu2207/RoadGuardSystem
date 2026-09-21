using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/road-sections")]
public sealed class ProjectRoadSectionsController : ControllerBase
{
    private readonly IRoadSectionVersionService _service;

    public ProjectRoadSectionsController(IRoadSectionVersionService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<RoadSectionVersionResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Create(
        Guid projectId,
        CreateRoadSectionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId) ||
            !TryParseRole(User.FindFirstValue("role"), out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.CreateRoadSectionAsync(
            actorUserId,
            actorRole,
            new CreateRoadSectionCommand(
                projectId,
                request.Code,
                request.Name,
                request.Srid,
                request.Coordinates.Select(coordinate => new RoadSectionCoordinate(coordinate.X, coordinate.Y)).ToArray(),
                request.EffectiveFrom,
                request.ChangeReason,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            RoadSectionVersionServiceStatus.Success or RoadSectionVersionServiceStatus.Replayed when result.Version is not null =>
                Created(
                    $"/api/v1/projects/{result.Version.ProjectId}/road-sections/{result.Version.RoadSectionId}",
                    ToResponse(result.Version)),
            RoadSectionVersionServiceStatus.Forbidden => ProblemResponse(
                StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            RoadSectionVersionServiceStatus.ProjectNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.ProjectNotFound, "Not found"),
            RoadSectionVersionServiceStatus.ProjectClosed => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.ProjectClosed, "Conflict"),
            RoadSectionVersionServiceStatus.RoadSectionCodeConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.RoadSectionCodeConflict, "Conflict"),
            RoadSectionVersionServiceStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    [Authorize]
    [HttpPost("{roadSectionId:guid}/versions")]
    [ProducesResponseType<RoadSectionVersionResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateVersion(
        Guid projectId,
        Guid roadSectionId,
        CreateRoadSectionVersionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId) ||
            !TryParseRole(User.FindFirstValue("role"), out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.CreateRoadSectionVersionAsync(
            actorUserId,
            actorRole,
            new CreateRoadSectionVersionCommand(
                projectId,
                roadSectionId,
                request.Srid,
                request.Coordinates.Select(coordinate => new RoadSectionCoordinate(coordinate.X, coordinate.Y)).ToArray(),
                request.EffectiveFrom,
                request.ChangeReason,
                request.ExpectedCurrentVersionId,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            RoadSectionVersionServiceStatus.Success or RoadSectionVersionServiceStatus.Replayed when result.Version is not null =>
                Created(
                    $"/api/v1/projects/{result.Version.ProjectId}/road-sections/{result.Version.RoadSectionId}/versions/{result.Version.RoadSectionVersionId}",
                    ToResponse(result.Version)),
            RoadSectionVersionServiceStatus.Forbidden => ProblemResponse(
                StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            RoadSectionVersionServiceStatus.ProjectNotFound or RoadSectionVersionServiceStatus.RoadSectionNotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                result.Status == RoadSectionVersionServiceStatus.RoadSectionNotFound ? ApiErrorCodes.RoadSectionNotFound : ApiErrorCodes.ProjectNotFound,
                "Not found"),
            RoadSectionVersionServiceStatus.ProjectClosed => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.ProjectClosed, "Conflict"),
            RoadSectionVersionServiceStatus.StaleConcurrency => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.RoadSectionConcurrencyConflict, "Conflict"),
            RoadSectionVersionServiceStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    private static RoadSectionVersionResponseDto ToResponse(RoadSectionVersionView version) => new(
        version.ProjectId,
        version.RoadSectionId,
        version.RoadSectionVersionId,
        version.Code,
        version.VersionNo,
        version.IsCurrent,
        version.EffectiveFrom,
        version.ChangeReason);

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The road section request could not be completed.",
            Instance = Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private Guid? CorrelationId() => Guid.TryParse(
        HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : null;

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
