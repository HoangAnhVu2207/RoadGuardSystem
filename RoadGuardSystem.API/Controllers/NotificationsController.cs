using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Services.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet("{notificationId:guid}")]
    [ProducesResponseType<NotificationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.GetAsync(actorUserId, notificationId, cancellationToken);
        if (result.Notification is not null)
        {
            Response.Headers.ETag = $"\"{result.Notification.Version}\"";
        }

        return result.Status switch
        {
            NotificationServiceStatus.Success when result.Notification is not null => Ok(result.Notification),
            NotificationServiceStatus.NotFound => ProblemResponse(StatusCodes.Status404NotFound, ApiErrorCodes.NotificationNotFound, "Not found"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType<NotificationPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> List(
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        var result = await _service.ListAsync(actorUserId, cursor, limit, cancellationToken);
        return result.Status switch
        {
            NotificationServiceStatus.Success when result.Page is not null => Ok(result.Page),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    [Authorize]
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType<NotificationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired, "application/problem+json")]
    public async Task<IActionResult> MarkRead(
        Guid notificationId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorUserId))
        {
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(ifMatch))
        {
            return ProblemResponse(
                StatusCodes.Status428PreconditionRequired,
                ApiErrorCodes.ValidationError,
                "Required precondition headers are missing");
        }

        var role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty);
        if (role == UserRoleCode.Unknown)
            return ProblemResponse(StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized");

        var result = await _service.MarkReadAsync(
            actorUserId,
            notificationId,
            idempotencyKey,
            ifMatch,
            role,
            cancellationToken);

        if (result.Notification is not null)
        {
            Response.Headers.ETag = $"\"{result.Notification.Version}\"";
        }

        return result.Status switch
        {
            NotificationServiceStatus.Success or NotificationServiceStatus.Replayed
                when result.Notification is not null => Ok(result.Notification),
            NotificationServiceStatus.Unauthorized => ProblemResponse(
                StatusCodes.Status401Unauthorized, ApiErrorCodes.Unauthorized, "Unauthorized"),
            NotificationServiceStatus.NotFound => ProblemResponse(
                StatusCodes.Status404NotFound,
                ApiErrorCodes.NotificationNotFound,
                "Not found"),
            NotificationServiceStatus.StaleConcurrency => ProblemResponse(
                StatusCodes.Status412PreconditionFailed,
                ApiErrorCodes.NotificationConcurrencyConflict,
                "Precondition failed"),
            NotificationServiceStatus.IdempotentConflict => ProblemResponse(
                StatusCodes.Status409Conflict,
                ApiErrorCodes.DuplicateRequest,
                "Idempotency key was reused with a different request"),
            _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Bad Request")
        };
    }

    private bool TryGetActor(out Guid actorUserId)
        => Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actorUserId);

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The notification request could not be completed.",
            Instance = Request.Path
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
