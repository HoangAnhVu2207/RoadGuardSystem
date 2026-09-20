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
[Route("api/v{version:apiVersion}/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectCreationService _service;
    private readonly IProjectUpdateService _updateService;

    public ProjectsController(IProjectCreationService service, IProjectUpdateService updateService)
    {
        _service = service;
        _updateService = updateService;
    }

    [Authorize]
    [HttpPut("{projectId:guid}")]
    [ProducesResponseType<UpdateProjectResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Update(
        Guid projectId,
        UpdateProjectRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId) ||
            !TryParseRole(User.FindFirstValue("role"), out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _updateService.UpdateAsync(
            actorUserId,
            actorRole,
            projectId,
            new UpdateProjectCommand(
                request.Name,
                request.Description,
                request.EngineeringUtmSrid,
                request.StartDate,
                request.EndDate,
                request.ExpectedRowVersion,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            ProjectUpdateStatus.Success or ProjectUpdateStatus.Replayed when result.Project is not null =>
                Ok(ToResponse(result.Project)),
            ProjectUpdateStatus.Forbidden => ProblemResponse(
                StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            ProjectUpdateStatus.NotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.ProjectNotFound, "Not found"),
            ProjectUpdateStatus.StaleConcurrency => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.ProjectConcurrencyConflict, "Conflict"),
            ProjectUpdateStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<CreateProjectResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Create(
        CreateProjectRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId) ||
            !TryParseRole(User.FindFirstValue("role"), out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        if (request.Handover?.HandoverDate is null)
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request");
        }

        var result = await _service.CreateAsync(
            actorUserId,
            actorRole,
            new CreateProjectCommand(
                request.ProjectCode,
                request.Name,
                request.Description,
                request.EngineeringUtmSrid,
                request.StartDate,
                request.EndDate,
                request.PrimaryProjectManagerUserId,
                request.Handover.DocumentNo,
                request.Handover.HandoverDate.Value,
                request.Handover.FileId,
                request.Handover.Notes,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            ProjectCreationStatus.Success or ProjectCreationStatus.Replayed when result.Project is not null =>
                Created($"/api/v1/projects/{result.Project.ProjectId}/work-package", ToResponse(result.Project)),
            ProjectCreationStatus.Forbidden => ProblemResponse(
                StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            ProjectCreationStatus.ProjectManagerNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.ProjectPrimaryManagerNotFound, "Not found"),
            ProjectCreationStatus.HandoverFileNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.ProjectHandoverFileNotFound, "Not found"),
            ProjectCreationStatus.ProjectCodeConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.ProjectCodeConflict, "Conflict"),
            ProjectCreationStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    private static CreateProjectResponseDto ToResponse(CreatedProjectView project) => new(
        project.ProjectId,
        project.ProjectCode,
        project.Name,
        project.Status,
        project.PrimaryProjectManagerUserId,
        project.HandoverDocumentId,
        project.HandoverDate,
        project.RowVersion);

    private static UpdateProjectResponseDto ToResponse(UpdatedProjectView project) => new(
        project.ProjectId,
        project.ProjectCode,
        project.Name,
        project.Description,
        project.EngineeringUtmSrid,
        project.StartDate,
        project.EndDate,
        project.Status,
        project.RowVersion);

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The project request could not be completed.",
            Instance = Request.Path,
            Type = status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                _ => "about:blank"
            }
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

    private Guid? CorrelationId() =>
        Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value)
            ? value
            : null;

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
