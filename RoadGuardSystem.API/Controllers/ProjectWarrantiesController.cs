using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Warranties;
using RoadGuardSystem.Services.Warranties;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/warranties")]
public sealed class ProjectWarrantiesController : ControllerBase
{
    private readonly IWarrantyCreationService _service;

    public ProjectWarrantiesController(IWarrantyCreationService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<CreateWarrantyResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Create(
        Guid projectId,
        CreateWarrantyRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorUserId) ||
            !TryParseRole(User.FindFirstValue("role"), out var actorRole))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.CreateAsync(
            actorUserId,
            actorRole,
            projectId,
            new CreateWarrantyCommand(
                request.RoadSectionId,
                request.HandoverDocumentId,
                request.HandoverDate,
                request.WarrantyStartDate,
                request.WarrantyEndDate,
                request.RetainedValue,
                request.Scope,
                request.Terms,
                request.SourceDocumentId,
                request.Status,
                request.OperationId,
                CorrelationId()),
            cancellationToken);

        return result.Status switch
        {
            WarrantyCreationStatus.Success or WarrantyCreationStatus.Replayed when result.Warranty is not null =>
                StatusCode(StatusCodes.Status201Created, ToResponse(result.Warranty)),
            WarrantyCreationStatus.Forbidden => ProblemResponse(
                StatusCodes.Status403Forbidden, ApiErrorCodes.AccessForbidden, "Forbidden"),
            WarrantyCreationStatus.ProjectNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.ProjectNotFound, "Not found"),
            WarrantyCreationStatus.RoadSectionNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.WarrantyRoadSectionNotFound, "Not found"),
            WarrantyCreationStatus.HandoverDocumentNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.WarrantyHandoverDocumentNotFound, "Not found"),
            WarrantyCreationStatus.SourceDocumentNotFound => ProblemResponse(
                StatusCodes.Status404NotFound, ApiErrorCodes.WarrantySourceDocumentNotFound, "Not found"),
            WarrantyCreationStatus.ProjectClosed => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.ProjectClosed, "Conflict"),
            WarrantyCreationStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    private static CreateWarrantyResponseDto ToResponse(CreatedWarrantyView warranty) => new(
        warranty.WarrantyId,
        warranty.ProjectId,
        warranty.RoadSectionId,
        warranty.HandoverDocumentId,
        warranty.HandoverDate,
        warranty.WarrantyStartDate,
        warranty.WarrantyEndDate,
        warranty.RetainedValue,
        warranty.Scope,
        warranty.Terms,
        warranty.SourceDocumentId,
        warranty.Status);

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The warranty request could not be completed.",
            Instance = Request.Path,
            Type = status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                _ => "https://tools.ietf.org/html/rfc9110#section-15.5.10"
            }
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]
            ?? HttpContext.TraceIdentifier;
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private Guid? CorrelationId()
    {
        var value = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        return Guid.TryParse(value, out var correlationId) ? correlationId : null;
    }

    private static bool TryParseRole(string? role, out UserRoleCode result)
    {
        result = role?.ToUpperInvariant() switch
        {
            "SUPERVISOR" => UserRoleCode.Supervisor,
            "PM" => UserRoleCode.ProjectManager,
            "DRONE_OPERATOR" => UserRoleCode.DroneOperator,
            "REPAIR_CREW" => UserRoleCode.RepairCrew,
            _ => default
        };
        return result != default;
    }
}
