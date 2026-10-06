using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Services.Offline;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/offline")]
public sealed class OfflineWorkflowController(IOfflineWorkflowService service) : ControllerBase
{
    [HttpPost("devices")]
    public Task<IActionResult> RegisterDevice(Guid projectId, OfflineDeviceRegisterInput input, CancellationToken cancellationToken)
        => Run(projectId, "device-register", input, null, cancellationToken);

    [HttpGet("devices/{registrationId:guid}")]
    public Task<IActionResult> GetDevice(Guid projectId, Guid registrationId, CancellationToken cancellationToken)
        => Run(projectId, "device-get", null, registrationId, cancellationToken);

    [HttpPost("devices/{registrationId:guid}/revoke")]
    public Task<IActionResult> RevokeDevice(Guid projectId, Guid registrationId, OfflineDeviceRevokeInput input,
        CancellationToken cancellationToken)
        => Run(projectId, "device-revoke", input, registrationId, cancellationToken);

    [HttpPost("snapshots")]
    public Task<IActionResult> CaptureSnapshot(Guid projectId, OfflineSnapshotInput input, CancellationToken cancellationToken)
        => Run(projectId, "snapshot-create", input, null, cancellationToken);

    [HttpGet("snapshots/{snapshotId:guid}")]
    public Task<IActionResult> GetSnapshot(Guid projectId, Guid snapshotId, CancellationToken cancellationToken)
        => Run(projectId, "snapshot-get", null, snapshotId, cancellationToken);

    [HttpPost("sync")]
    public Task<IActionResult> Synchronize(Guid projectId, OfflineSignedBatchInput input, CancellationToken cancellationToken)
        => Run(projectId, "sync", input, null, cancellationToken);

    [HttpPost("packages/export")]
    public Task<IActionResult> ExportPackage(Guid projectId, OfflinePackageExportInput input, CancellationToken token)
        => Run(projectId, "package-export", input, null, token);

    [HttpPost("packages/prepare")]
    public Task<IActionResult> PreparePackage(Guid projectId, OfflinePackageExportInput input, CancellationToken token)
        => Run(projectId, "package-prepare", input, null, token);

    [HttpGet("packages/{packageId:guid}")]
    public Task<IActionResult> GetPackage(Guid projectId, Guid packageId, CancellationToken token)
        => Run(projectId, "package-get", null, packageId, token);

    [HttpPost("grants")]
    public Task<IActionResult> IssueGrant(Guid projectId, OfflineHandoverGrantInput input, CancellationToken token)
        => Run(projectId, "grant-issue", input, null, token);

    [HttpGet("grants/{grantId:guid}")]
    public Task<IActionResult> GetGrant(Guid projectId, Guid grantId, CancellationToken token)
        => Run(projectId, "grant-get", null, grantId, token);

    [HttpPost("grants/{grantId:guid}/revoke")]
    public Task<IActionResult> RevokeGrant(Guid projectId, Guid grantId, OfflineGrantRevokeInput input, CancellationToken token)
        => Run(projectId, "grant-revoke", input, grantId, token);

    [HttpPost("import")]
    public Task<IActionResult> ImportPackage(Guid projectId, OfflinePackageImportInput input, CancellationToken token)
        => Run(projectId, "import", input, null, token);

    [HttpGet("batches/{batchId:guid}")]
    public Task<IActionResult> GetBatch(Guid projectId, Guid batchId, CancellationToken token)
        => Run(projectId, "batch-get", null, batchId, token);

    [HttpGet("origins/{originId:guid}")]
    public Task<IActionResult> GetOrigin(Guid projectId, Guid originId, CancellationToken token)
        => Run(projectId, "origin-get", null, originId, token);

    [HttpPost("starts/reconcile")]
    public Task<IActionResult> ReconcileStart(Guid projectId, OfflineStartReconcileInput input, CancellationToken token)
        => Run(projectId, "start-reconcile", input, null, token);

    [HttpPost("artifacts")]
    public Task<IActionResult> RegisterArtifact(Guid projectId, OfflineCaptureArtifactInput input, CancellationToken token)
        => Run(projectId, "artifact-register", input, null, token);

    [HttpGet("artifacts/{artifactId:guid}")]
    public Task<IActionResult> GetArtifact(Guid projectId, Guid artifactId, CancellationToken token)
        => Run(projectId, "artifact-get", null, artifactId, token);

    private async Task<IActionResult> Run(Guid projectId, string action, object? input, Guid? resourceId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        string? key = null;
        if (Request.Headers.TryGetValue("Idempotency-Key", out var keys))
        {
            if (keys.Count != 1) return Error(400, "validation_error");
            key = keys.ToString();
        }
        if (Request.Headers.IfMatch.Count > 1) return Error(400, "validation_error");
        var result = await service.ExecuteAsync(actor, role, projectId, action, input, resourceId, key,
            Request.Headers.IfMatch.Count == 0 ? null : Request.Headers.IfMatch.ToString(), cancellationToken);
        if (result.Code is not null) return Error(result.Status, result.Code);
        if (result.Version is not null) Response.Headers.ETag = $"\"{result.Version}\"";
        return StatusCode(result.Status, result.Value);
    }

    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Offline request failed", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
