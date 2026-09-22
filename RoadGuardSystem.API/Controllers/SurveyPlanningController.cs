using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Services.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}")]
public sealed class SurveyPlanningController : ControllerBase
{
    private readonly ISurveyPlanningService _service;

    public SurveyPlanningController(ISurveyPlanningService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost("road-sections/{roadSectionId:guid}/survey-plans")]
    [ProducesResponseType<CreateSurveyPlanResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> CreatePlan(
        Guid projectId,
        Guid roadSectionId,
        CreateSurveyPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.CreatePlanAsync(
            actorUserId,
            actorRole,
            new CreateSurveyPlanCommand(
                projectId,
                roadSectionId,
                request.RoadSectionVersionId,
                request.PlannedStartAt,
                request.PlannedEndAt,
                request.SurveyType,
                request.OutputRequirements,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            SurveyPlanningServiceStatus.Success or SurveyPlanningServiceStatus.Replayed when result.Plan is not null =>
                Created($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans/{result.Plan.PlanId}", ToResponse(result.Plan)),
            SurveyPlanningServiceStatus.Forbidden => ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyPlanningServiceStatus.NotFound => ProblemResponse(StatusCodes.Status404NotFound, ApiErrorCodes.ProjectNotFound, "Not found"),
            SurveyPlanningServiceStatus.Conflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.SurveyInvalidStateTransition, "Conflict"),
            SurveyPlanningServiceStatus.IdempotentConflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status422UnprocessableEntity, ApiErrorCodes.SurveyValidationFailed, "Survey plan validation failed")
        };
    }

    [Authorize]
    [HttpPost("road-sections/{roadSectionId:guid}/survey-requests")]
    [ProducesResponseType<CreateSurveyRequestResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> CreateRequest(
        Guid projectId,
        Guid roadSectionId,
        CreateSurveyRequestRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.CreateRequestAsync(
            actorUserId,
            actorRole,
            new CreateSurveyRequestCommand(
                projectId,
                roadSectionId,
                request.RoadSectionVersionId,
                request.SurveyPlanId,
                request.SurveyType,
                request.DueAt,
                request.OutputRequirements,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            SurveyPlanningServiceStatus.Success or SurveyPlanningServiceStatus.Replayed when result.Request is not null =>
                Created($"/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests/{result.Request.RequestId}", ToResponse(result.Request)),
            SurveyPlanningServiceStatus.Forbidden => ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyPlanningServiceStatus.NotFound => ProblemResponse(StatusCodes.Status404NotFound, request.SurveyPlanId is null ? ApiErrorCodes.ProjectNotFound : ApiErrorCodes.SurveyPlanNotFound, "Not found"),
            SurveyPlanningServiceStatus.Conflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.SurveyInvalidStateTransition, "Conflict"),
            SurveyPlanningServiceStatus.IdempotentConflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status422UnprocessableEntity, ApiErrorCodes.SurveyValidationFailed, "Survey request validation failed")
        };
    }

    [Authorize]
    [HttpPost("survey-plans/{surveyPlanId:guid}/postpone")]
    [ProducesResponseType<PostponeSurveyPlanResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> PostponePlan(
        Guid projectId,
        Guid surveyPlanId,
        PostponeSurveyPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId, out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.PostponePlanAsync(
            actorUserId,
            actorRole,
            new PostponeSurveyPlanCommand(
                projectId,
                surveyPlanId,
                request.NewPlannedStartAt,
                request.Reason,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            SurveyPlanningServiceStatus.Success or SurveyPlanningServiceStatus.Replayed when result.Postponement is not null => Ok(ToResponse(result.Postponement)),
            SurveyPlanningServiceStatus.Forbidden => ProblemResponse(StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyPlanningServiceStatus.NotFound => ProblemResponse(StatusCodes.Status404NotFound, ApiErrorCodes.SurveyPlanNotFound, "Not found"),
            SurveyPlanningServiceStatus.Conflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.SurveyInvalidStateTransition, "Conflict"),
            SurveyPlanningServiceStatus.IdempotentConflict => ProblemResponse(StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status422UnprocessableEntity, ApiErrorCodes.SurveyValidationFailed, "Survey postponement validation failed")
        };
    }

    private bool TryGetActor(out Guid actorUserId, out UserRoleCode actorRole)
    {
        actorUserId = Guid.Empty;
        actorRole = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actorUserId))
        {
            return false;
        }

        try
        {
            actorRole = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty);
            return actorRole != UserRoleCode.Unknown;
        }
        catch (ArgumentOutOfRangeException)
        {
            actorRole = UserRoleCode.Unknown;
            return false;
        }
    }

    private static CreateSurveyPlanResponseDto ToResponse(RoadGuardSystem.Repositories.Surveys.SurveyPlanPersistenceView value) => new(
        value.PlanId, value.ProjectId, value.RoadSectionId, value.RoadSectionVersionId, value.PlannedStartAt, value.PlannedEndAt,
        value.SurveyType, value.Status, value.OutputRequirements);

    private static CreateSurveyRequestResponseDto ToResponse(RoadGuardSystem.Repositories.Surveys.SurveyRequestPersistenceView value) => new(
        value.RequestId, value.ProjectId, value.RoadSectionId, value.RoadSectionVersionId, value.SurveyPlanId, value.RequestedByUserId,
        value.SurveyType, value.Status, value.RequestedAt, value.DueAt, value.OutputRequirements);

    private static PostponeSurveyPlanResponseDto ToResponse(RoadGuardSystem.Repositories.Surveys.SurveyPlanPostponementPersistenceView value) => new(
        value.PlanId, value.Status, value.NewPlannedStartAt, value.PostponementId);

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The survey planning request could not be completed.",
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
}
