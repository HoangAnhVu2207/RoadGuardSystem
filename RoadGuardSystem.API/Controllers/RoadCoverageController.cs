using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/road-coverage")]
public sealed class RoadCoverageController(IRoadCoverageService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Read(Guid projectId, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        return Map(await service.ReadAsync(actor, role, projectId, token));
    }
    [HttpPost]
    public async Task<IActionResult> Confirm(Guid projectId, RoadCoverageInputDto input, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
        return Map(await service.ConfirmAsync(actor, role, projectId, input, Request.Headers["Idempotency-Key"].ToString(),
            Request.Headers.IfMatch.ToString(), token));
    }
    /// <summary>Supervisor document inventory, including documents without a file.</summary>
    /// <remarks>Ordered by document ID; all documents are returned. This is not road-coverage.sources.
    /// Version is the document rowversion, not the If-Match token for attach. After upload VERIFIED,
    /// get a fresh road-coverage ETag for POST attach. No file download authority is granted here.</remarks>
    [HttpGet("/api/v{version:apiVersion}/projects/{projectId:guid}/handover-documents")]
    [ProducesResponseType<RoadGuardSystem.BusinessObjects.Projects.HandoverDocumentsReadFact>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HandoverDocuments(Guid projectId, CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        var result = await service.ReadHandoverDocumentsAsync(actor, role, projectId, token);
        if (result.Code is not null) return Error(result.Status, result.Code);
        Response.Headers.CacheControl = "no-store";
        return StatusCode(result.Status, result.Value);
    }

    [HttpPost("/api/v{version:apiVersion}/projects/{projectId:guid}/handover-documents")]
    [ProducesResponseType<RoadGuardSystem.BusinessObjects.Repairs.RoadCoverageReadFact>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> AttachHandover(Guid projectId, HandoverAttachmentDto input,
        [FromHeader(Name = "Idempotency-Key")] string? key, [FromHeader(Name = "If-Match")] string? version,
        CancellationToken token)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "unauthorized");
        if (Request.Headers["Idempotency-Key"].Count > 1 || Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
        return Map(await service.AttachHandoverAsync(actor, role, projectId, input, key, version, token));
    }

    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = default;
        return Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor) &&
            ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out role);
    }
    private ObjectResult Map(RoadCoverageServiceResult result)
    {
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Value is not null) Response.Headers.ETag = '"' + result.Value.Version + '"';
        return StatusCode(result.Status, result.Value);
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Road coverage request failed", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
