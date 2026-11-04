using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Services.Cases;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/cases/{caseId:guid}/reports")]
public sealed class CaseReportDetailsController(ICaseReportDetailsService service) : ControllerBase
{
    [HttpGet]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [ProducesResponseType<CaseReportDetailPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed, "application/problem+json")]
    public async Task<IActionResult> List(Guid caseId, [FromQuery] int limit = 20, [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var result = await service.ReadAsync(actor, role, caseId, limit, cursor, cancellationToken);
        if (result.Status != 200) return Error(result.Status, result.Code!);
        Response.Headers.CacheControl = "private, no-store";
        return Ok(result.Page);
    }

    [HttpGet("{reportId:guid}/evidence/{evidenceId:guid}/content")]
    [RoadGuardSystem.API.Authentication.WebCookieEligible]
    [Produces("image/jpeg", "image/png")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Download(Guid caseId, Guid reportId, Guid evidenceId,
        CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var result = await service.DownloadAsync(actor, role, caseId, reportId, evidenceId, cancellationToken);
        if (result.Status != 200 || result.Content is null) return Error(result.Status, result.Code!);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.ETag = $"\"{result.Content.FileVersion}\"";
        return File(result.Content.Stream, result.Content.MediaType);
    }

    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = "Case report request failed",
            Detail = "The case report request could not be completed.",
            Instance = Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
