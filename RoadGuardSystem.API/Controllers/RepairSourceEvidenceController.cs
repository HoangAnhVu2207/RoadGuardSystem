using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/source-evidence")]
public sealed class RepairSourceEvidenceController(RepairSourceEvidenceService service, IUploadRepository uploads) : ControllerBase
{
    [HttpGet("{evidenceId:guid}/content")]
    public Task<IActionResult> Content(Guid projectId, Guid packageId, Guid itemId, Guid evidenceId, CancellationToken token)
        => Read(projectId, packageId, itemId, evidenceId, token);
    private async Task<IActionResult> Read(Guid project, Guid package, Guid item, Guid? evidence, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        var result = await service.ReadAsync(actor, role, project, package, item, evidence, token);
        if (result.Code is not null) return Error(result.Status, result.Code);
        Response.Headers.CacheControl = "no-store";
        if (evidence is null) return Ok(result.Evidence);
        Stream stream;
        try { stream = await uploads.OpenFileAsync(result.File!.ObjectKey, token); }
        catch (RoadGuardSystem.Repositories.Storage.FileStorageException) { return Error(503, "storage_unavailable"); }
        try
        {
            var current = await service.ReadAsync(actor, role, project, package, item, evidence, token);
            if (current.Code is not null || current.File is not { } file || file.FileId != result.File.FileId ||
                file.Checksum != result.File.Checksum || file.ObjectKey != result.File.ObjectKey)
            {
                await stream.DisposeAsync(); return Error(current.Code is not null ? current.Status : 409, current.Code ?? "source_not_ready");
            }
            return File(stream, file.MediaType);
        }
        catch { await stream.DisposeAsync(); throw; }
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Repair source evidence request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
