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
[Route("api/v{version:apiVersion}")]
public sealed class SurveyV2Controller : ControllerBase
{
    private readonly ISurveyV2Service _service;

    public SurveyV2Controller(ISurveyV2Service service) => _service = service;

    [Authorize]
    [HttpPost("projects/{projectId:guid}/survey-plans")]
    public async Task<IActionResult> CreatePlan(Guid projectId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CreateSurveyPlanV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreatePlanAsync(actor, role, projectId, request, idempotencyKey, CorrelationId(), cancellationToken);
        if (result.Plan is not null) Response.Headers.ETag = $"\"{result.Plan.Version}\"";
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success or SurveyV2ServiceStatus.Replayed when result.Plan is not null => Created($"/api/v1/survey-plans/{result.Plan.Id}", result.Plan),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyV2ServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.ProjectNotFound, "Not found"),
            SurveyV2ServiceStatus.Conflict or SurveyV2ServiceStatus.IdempotentConflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Conflict"),
            _ => ProblemResponse(422, ApiErrorCodes.SurveyValidationFailed, "Survey plan validation failed")
        };
    }

    [Authorize]
    [HttpPost("survey-plans/{planId:guid}/postpone")]
    public async Task<IActionResult> Postpone(Guid planId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, PostponeSurveyPlanV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        var result = await _service.PostponePlanAsync(actor, role, planId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken);
        if (result.Plan is not null) Response.Headers.ETag = $"\"{result.Plan.Version}\"";
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success or SurveyV2ServiceStatus.Replayed when result.Plan is not null => Ok(result.Plan),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyV2ServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.SurveyPlanNotFound, "Not found"),
            SurveyV2ServiceStatus.ConcurrencyConflict => ProblemResponse(412, ApiErrorCodes.ConcurrencyConflict, "Precondition failed"),
            SurveyV2ServiceStatus.ScopeIncompatible => ProblemResponse(409, "survey_scope_incompatible", "Survey scope is incompatible"),
            SurveyV2ServiceStatus.IdempotentConflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
            _ => ProblemResponse(422, ApiErrorCodes.SurveyValidationFailed, "Survey plan validation failed")
        };
    }

    [Authorize]
    [HttpPost("projects/{projectId:guid}/survey-tasks")]
    public async Task<IActionResult> CreateTask(Guid projectId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CreateSurveyTaskV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Idempotency-Key is required");
        var result = await _service.CreateTaskAsync(actor, role, projectId, request, idempotencyKey, CorrelationId(), cancellationToken);
        if (result.Task is not null) Response.Headers.ETag = $"\"{result.Task.Version}\"";
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success or SurveyV2ServiceStatus.Replayed when result.Task is not null => Created($"/api/v1/survey-tasks/{result.Task.Id}", result.Task),
            SurveyV2ServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.ProjectNotFound, "Not found"),
            SurveyV2ServiceStatus.OperatorNotFound => ProblemResponse(404, ApiErrorCodes.IdentityUserNotFound, "Operator not found"),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyV2ServiceStatus.Conflict or SurveyV2ServiceStatus.IdempotentConflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Conflict"),
            _ => ProblemResponse(422, ApiErrorCodes.SurveyValidationFailed, "Survey task validation failed")
        };
    }

    [Authorize]
    [HttpGet("survey-tasks/{taskId:guid}")]
    public async Task<IActionResult> GetTask(Guid taskId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.GetTaskAsync(actor, role, taskId, cancellationToken);
        if (result.Task is not null) Response.Headers.ETag = $"\"{result.Task.Version}\"";
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success when result.Task is not null => Ok(result.Task),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            _ => ProblemResponse(404, ApiErrorCodes.SurveyRequestNotFound, "Not found")
        };
    }

    [Authorize, HttpGet("survey-plans/{id:guid}")]
    public Task<IActionResult> ReadPlan(Guid id, CancellationToken token) => ReadResource(id, "plan", token);
    [Authorize, HttpGet("datasets/{id:guid}")]
    public Task<IActionResult> ReadDataset(Guid id, CancellationToken token) => ReadResource(id, "dataset", token);
    [Authorize, HttpGet("survey-tasks/{id:guid}/work-package")]
    public Task<IActionResult> ReadWorkPackage(Guid id, CancellationToken token) => ReadResource(id, "work-package", token);
    private async Task<IActionResult> ReadResource(Guid id, string resource, CancellationToken token)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.ReadResourceAsync(actor, role, id, resource, token);
        if (result.Version is not null) Response.Headers.ETag = $"\"{result.Version}\"";
        return result.Status == SurveyV2ServiceStatus.Success ? Ok(result.Value) : result.Status == SurveyV2ServiceStatus.Forbidden ?
            ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden") : ProblemResponse(404, ApiErrorCodes.NotFound, "Not found");
    }

    [Authorize]
    [HttpGet("me/survey-tasks")]
    public async Task<IActionResult> ListMyTasks([FromQuery] string? cursor, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (role != UserRoleCode.DroneOperator) return ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden");
        if (limit is < 1 or > 100) return ProblemResponse(400, ApiErrorCodes.ValidationError, "Limit must be between 1 and 100");
        var page = await _service.ListMyTasksAsync(actor, role, cursor, limit, cancellationToken);
        return page is null ? ProblemResponse(400, ApiErrorCodes.ValidationError, "Invalid survey task list parameters") : Ok(page);
    }

    [Authorize]
    [HttpPost("survey-tasks/{taskId:guid}/datasets")]
    public async Task<IActionResult> SubmitDataset(
        Guid taskId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        SubmitDatasetRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        var result = await _service.SubmitDatasetAsync(actor, role, taskId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken);
        if (result.Dataset is not null) Response.Headers.ETag = $"\"{result.Dataset.Version}\"";
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success or SurveyV2ServiceStatus.Replayed when result.Dataset is not null => Created($"/api/v1/datasets/{result.Dataset.Id}", result.Dataset),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyV2ServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.SurveyRequestNotFound, "Not found"),
            SurveyV2ServiceStatus.ConcurrencyConflict => ProblemResponse(412, ApiErrorCodes.ConcurrencyConflict, "Precondition failed"),
            SurveyV2ServiceStatus.IdempotentConflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
            SurveyV2ServiceStatus.Conflict => ProblemResponse(409, ApiErrorCodes.SurveyInvalidStateTransition, "Dataset submission is not allowed"),
            SurveyV2ServiceStatus.ScopeIncompatible => ProblemResponse(409, "survey_scope_incompatible", "Survey scope is incompatible"),
            _ => ProblemResponse(422, ApiErrorCodes.SurveyValidationFailed, "Dataset validation failed")
        };
    }

    [Authorize]
    [HttpGet("datasets/{datasetId:guid}/coverage")]
    public async Task<IActionResult> GetDatasetCoverage(Guid datasetId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        var result = await _service.GetDatasetCoverageAsync(actor, role, datasetId, cancellationToken);
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success when result.Coverage is not null => Ok(result.Coverage),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyV2ServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.NotFound, "Not found"),
            _ => ProblemResponse(422, ApiErrorCodes.SurveyValidationFailed, "Coverage data is invalid")
        };
    }

    [Authorize]
    [HttpPost("survey-tasks/{taskId:guid}/accept")]
    public async Task<IActionResult> AcceptTask(Guid taskId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return MapTaskMutation(await _service.AcceptTaskAsync(actor, role, taskId, idempotencyKey, ifMatch, CorrelationId(), cancellationToken));
    }

    [Authorize]
    [HttpPost("survey-tasks/{taskId:guid}/decline")]
    public async Task<IActionResult> DeclineTask(Guid taskId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, SurveyTaskReasonV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return MapTaskMutation(await _service.DeclineTaskAsync(actor, role, taskId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken));
    }

    [Authorize]
    [HttpPost("survey-tasks/{taskId:guid}/cancel")]
    public async Task<IActionResult> CancelTask(Guid taskId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, SurveyTaskReasonV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return MapTaskMutation(await _service.CancelTaskAsync(actor, role, taskId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken));
    }

    [Authorize]
    [HttpPost("survey-tasks/{taskId:guid}/reassign")]
    public async Task<IActionResult> ReassignTask(Guid taskId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, ReassignSurveyTaskV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        return MapTaskMutation(await _service.ReassignTaskAsync(actor, role, taskId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken));
    }

    [Authorize]
    [HttpPost("survey-tasks/{taskId:guid}/supplements")]
    public async Task<IActionResult> RequestSupplement(Guid taskId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, [FromHeader(Name = "If-Match")] string? ifMatch, SupplementSurveyTaskV2RequestDto request, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actor, out var role)) return ProblemResponse(401, ApiErrorCodes.Unauthorized, "Unauthorized");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch)) return ProblemResponse(428, ApiErrorCodes.ValidationError, "Required precondition headers are missing");
        var result = await _service.RequestSupplementAsync(actor, role, taskId, request, idempotencyKey, ifMatch, CorrelationId(), cancellationToken);
        var response = MapTaskMutation(result);
        if (result.Task is not null && response is ObjectResult { StatusCode: 200 })
        {
            response = Created($"/api/v1/survey-tasks/{result.Task.SupplementTaskId ?? result.Task.Id}", result.Task);
        }
        return response;
    }

    private ObjectResult MapTaskMutation(SurveyV2ServiceResult result)
    {
        if (result.Task is not null) Response.Headers.ETag = $"\"{result.Task.Version}\"";
        return result.Status switch
        {
            SurveyV2ServiceStatus.Success or SurveyV2ServiceStatus.Replayed when result.Task is not null => Ok(result.Task),
            SurveyV2ServiceStatus.Forbidden => ProblemResponse(403, ApiErrorCodes.AccessForbidden, "Forbidden"),
            SurveyV2ServiceStatus.NotFound => ProblemResponse(404, ApiErrorCodes.SurveyRequestNotFound, "Not found"),
            SurveyV2ServiceStatus.ConcurrencyConflict => ProblemResponse(412, ApiErrorCodes.ConcurrencyConflict, "Precondition failed"),
            SurveyV2ServiceStatus.IdempotentConflict => ProblemResponse(409, ApiErrorCodes.DuplicateRequest, "Idempotency key was reused with a different request"),
            SurveyV2ServiceStatus.OperatorNotFound => ProblemResponse(404, ApiErrorCodes.IdentityUserNotFound, "Operator not found"),
            SurveyV2ServiceStatus.Conflict => ProblemResponse(409, ApiErrorCodes.SurveyInvalidStateTransition, "Survey task transition is not allowed"),
            SurveyV2ServiceStatus.ScopeIncompatible => ProblemResponse(409, "survey_scope_incompatible", "Survey scope is incompatible"),
            _ => ProblemResponse(422, ApiErrorCodes.SurveyValidationFailed, "Survey task validation failed")
        };
    }

    private bool TryGetActor(out Guid id, out UserRoleCode role)
    {
        id = Guid.Empty; role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out id)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private Guid? CorrelationId() => Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : null;
    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = "The survey request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
