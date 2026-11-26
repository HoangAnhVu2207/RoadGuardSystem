using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/source-tasks/{taskId:guid}")]
public sealed class RepairSourceReadsController(IRepairWorkflowService service, IUploadRepository uploads) : ControllerBase
{
    [HttpGet("geometry")]
    public Task<IActionResult> Geometry(Guid projectId, Guid packageId, Guid itemId, Guid taskId, CancellationToken token)
        => Read(projectId, packageId, itemId, taskId, "geometry", null, false, token);
    [HttpGet("history")]
    [ProducesResponseType<RoadGuardSystem.DTOs.Inspections.FieldHistoryView>(200)]
    public Task<IActionResult> History(Guid projectId, Guid packageId, Guid itemId, Guid taskId, CancellationToken token)
        => Read(projectId, packageId, itemId, taskId, "history", null, false, token);
    [HttpGet("submissions/{submissionId:guid}")]
    [ProducesResponseType<RoadGuardSystem.DTOs.Inspections.FieldSubmissionView>(200)]
    public Task<IActionResult> Submission(Guid projectId, Guid packageId, Guid itemId, Guid taskId, Guid submissionId, CancellationToken token)
        => Read(projectId, packageId, itemId, taskId, "submission", submissionId, false, token);
    [HttpGet("evidence/{fileId:guid}")]
    [ProducesResponseType<FieldTaskEvidenceFile>(200)]
    public Task<IActionResult> Evidence(Guid projectId, Guid packageId, Guid itemId, Guid taskId, Guid fileId, CancellationToken token)
        => Read(projectId, packageId, itemId, taskId, "evidence", fileId, false, token);
    [HttpGet("evidence/{fileId:guid}/content")]
    public Task<IActionResult> Content(Guid projectId, Guid packageId, Guid itemId, Guid taskId, Guid fileId, CancellationToken token)
        => Read(projectId, packageId, itemId, taskId, "evidence", fileId, true, token);
    private async Task<IActionResult> Read(Guid project, Guid package, Guid item, Guid task, string kind,
        Guid? resource, bool content, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        var result = await service.ReadSourceTaskAsync(actor, role, project, package, item, task, kind, resource, token);
        if (result.Code is not null) return Error(result.Status, result.Code);
        Response.Headers.CacheControl = "no-store";
        if (result.Version is not null) Response.Headers.ETag = $"\"{result.Version}\"";
        if (!content) return StatusCode(result.Status, result.Value);
        if (result.Value is not FieldTaskEvidenceFile { State: "VERIFIED" } file) return Error(409, "source_not_ready");
        Stream stream;
        try { stream = await uploads.OpenFileAsync(file.ObjectKey, token); }
        catch (RoadGuardSystem.Repositories.Storage.FileStorageException) { return Error(503, "storage_unavailable"); }
        try
        {
            var current = await service.ReadSourceTaskAsync(actor, role, project, package, item, task, kind, resource, token);
            if (current.Code is not null || current.Value is not FieldTaskEvidenceFile { State: "VERIFIED" } fresh ||
                fresh.FileId != file.FileId || fresh.Checksum != file.Checksum || fresh.ObjectKey != file.ObjectKey)
            {
                await stream.DisposeAsync(); return Error(current.Code is not null ? current.Status : 409, current.Code ?? "source_not_ready");
            }
            return File(stream, fresh.MediaType);
        }
        catch { await stream.DisposeAsync(); throw; }
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Repair source read failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
