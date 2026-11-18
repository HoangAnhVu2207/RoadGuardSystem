using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.DTOs.Web;
using RoadGuardSystem.Services.Web;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, WebCookieEligible, Route("api/v{version:apiVersion}")]
public sealed class WebCoordinationController(WebCoordinationService service, IDataProtectionProvider protection) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/task-assignees")]
    [ProducesResponseType<TaskAssignee[]>(200)]
    public Task<IActionResult> Assignees(Guid projectId, [FromQuery] Guid[] taskIds, CancellationToken token)
        => Run(async (actor, role) => Ok(await service.TaskAssigneesAsync(actor, role, projectId, taskIds, token)));
    [HttpGet("users/{userId:guid}/organization")]
    [ProducesResponseType<OrganizationProfile>(200)]
    public Task<IActionResult> Organization(Guid userId, CancellationToken token)
        => Run(async (actor, role) => Render(await service.OrganizationAsync(actor, role, userId, token)));
    [HttpGet("projects/overviews")]
    [ProducesResponseType<ProjectOverview[]>(200)]
    public Task<IActionResult> Overviews([FromQuery] Guid[] ids, CancellationToken token)
        => Run(async (actor, role) => Ok(await service.OverviewsAsync(actor, role, ids, token)));

    [HttpGet("projects/{projectId:guid}/member-contacts")]
    [ProducesResponseType<WebPage<ProjectContact>>(200)]
    public Task<IActionResult> Contacts(Guid projectId, [FromQuery] int limit = 25, [FromQuery] string? cursor = null,
        CancellationToken token = default) => Run(async (actor, role) =>
    {
        await service.GuardAsync(actor, role, [projectId], token);
        var seal = Cursor(actor, role, projectId, "contacts");
        var rows = await service.ContactsAsync(actor, role, projectId, ParseCursor(seal, cursor, limit), limit + 1, token);
        return Ok(new WebPage<ProjectContact>(rows.Take(limit).ToArray(), rows.Length > limit ? seal.Protect(rows[limit - 1].MembershipId.ToString("D")) : null));
    });

    [HttpGet("projects/{projectId:guid}/repair-policies")]
    [ProducesResponseType<WebPage<PolicyDiscovery>>(200)]
    public Task<IActionResult> Policies(Guid projectId, [FromQuery] string kind = "REVISION", [FromQuery] string? defectTypeCode = null,
        [FromQuery] bool applicableOnly = false, [FromQuery] int limit = 25, [FromQuery] string? cursor = null,
        CancellationToken token = default) => Run(async (actor, role) =>
    {
        await service.GuardAsync(actor, role, [projectId], token);
        var seal = Cursor(actor, role, projectId, $"policies|{kind}|{defectTypeCode}|{applicableOnly}");
        var rows = await service.PoliciesAsync(actor, role, projectId, kind, ParseCursor(seal, cursor, limit), limit + 1, defectTypeCode, applicableOnly, token);
        return Ok(new WebPage<PolicyDiscovery>(rows.Take(limit).ToArray(), rows.Length > limit ? seal.Protect(rows[limit - 1].Id.ToString("D")) : null));
    });

    [HttpGet("projects/{projectId:guid}/repair-packages/{packageId:guid}/planning")]
    [ProducesResponseType<PackagePlanning>(200)]
    public Task<IActionResult> Package(Guid projectId, Guid packageId, CancellationToken token)
        => Run(async (actor, role) => Render(await service.PackageAsync(actor, role, projectId, packageId, token)));

    [HttpGet("projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/planning")]
    [ProducesResponseType<ItemPlanning>(200)]
    public Task<IActionResult> Item(Guid projectId, Guid packageId, Guid itemId, CancellationToken token)
        => Run(async (actor, role) => Render(await service.ItemAsync(actor, role, projectId, packageId, itemId, token)));

    [HttpGet("projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/source-defect")]
    [ProducesResponseType<DefectSourceReview>(200)]
    public Task<IActionResult> Defect(Guid projectId, Guid packageId, Guid itemId, CancellationToken token)
        => Run(async (actor, role) => Render(await service.DefectAsync(actor, role, projectId, packageId, itemId, token)));

    [HttpPut("projects/{projectId:guid}/locality")]
    [ProducesResponseType<WebMetadataVersion>(200)]
    public Task<IActionResult> Locality(Guid projectId, ProjectLocalityInput input, CancellationToken token)
        => Write("locality", projectId, projectId, null, input.Locality, null, null, null, null, null, input.Reason, token);

    [HttpPut("users/{userId:guid}/organization")]
    [ProducesResponseType<WebMetadataVersion>(200)]
    public Task<IActionResult> Organization(Guid userId, OrganizationInput input, CancellationToken token)
        => Write("organization", userId, null, null, input.Organization, null, null, null, null, null, input.Reason, token);

    [HttpPut("projects/{projectId:guid}/repair-packages/{packageId:guid}/planning")]
    [ProducesResponseType<WebMetadataVersion>(200)]
    public Task<IActionResult> Package(Guid projectId, Guid packageId, PackagePlanningInput input, CancellationToken token)
        => Write("package", packageId, projectId, null, input.Name, input.Description, input.PlannedDueAt, null, null, null, input.Reason, token);

    [HttpPut("projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/planning")]
    [ProducesResponseType<WebMetadataVersion>(200)]
    public Task<IActionResult> Item(Guid projectId, Guid packageId, Guid itemId, ItemPlanningInput input, CancellationToken token)
        => Write("item", itemId, projectId, packageId, null, null, null, input.PlannedQuantity, input.PlannedUnit, input.PlannedMaterials, input.Reason, token);

    private Task<IActionResult> Write(string action, Guid target, Guid? project, Guid? package, string? text, string? description,
        DateTimeOffset? due, decimal? quantity, string? unit, string? materials, string reason, CancellationToken token)
        => Run(async (actor, role) =>
    {
        if (Request.Headers["Idempotency-Key"].Count != 1 || Request.Headers.IfMatch.Count != 1) return Error(428, "precondition_required");
        var etag = Request.Headers.IfMatch.ToString();
        var bytes = new byte[8];
        if (etag.Length != 14 || etag[0] != '"' || etag[^1] != '"' || !Convert.TryFromBase64Chars(etag.AsSpan(1, 12), bytes, out var size) || size != 8)
            return Error(400, "validation_error");
        var result = await service.WriteAsync(actor, role, new(action, target, project, package, text, description, due,
            quantity, unit, materials, reason, Request.Headers["Idempotency-Key"].ToString(), etag[1..^1]), token);
        if (result.Code is not null) return Error(result.Status, result.Code);
        Response.Headers.ETag = $"\"{result.Version}\"";
        return Ok(new WebMetadataVersion(result.Version!));
    });

    private IDataProtector Cursor(Guid actor, UserRoleCode role, Guid project, string filter)
        => protection.CreateProtector("RoadGuard.WebCoordination.v1", actor.ToString("D"), role.ToString(), project.ToString("D"), filter);
    private static Guid? ParseCursor(IDataProtector seal, string? cursor, int limit)
    {
        if (limit is < 1 or > 100 || cursor?.Length > 4096) throw new WebReadException(400, "validation_error");
        if (cursor is null) return null;
        try { return Guid.TryParseExact(seal.Unprotect(cursor), "D", out var id) && id != Guid.Empty ? id : throw new WebReadException(400, "validation_error"); }
        catch (CryptographicException) { throw new WebReadException(400, "validation_error"); }
    }
    private IActionResult Render(object? value)
    {
        if (value is null) return Error(404, "not_found");
        var version = value switch { PackagePlanning row => row.Version, ItemPlanning row => row.Version, DefectSourceReview row => row.Version, OrganizationProfile row => row.Version, _ => null };
        if (version is not null) Response.Headers.ETag = $"\"{version}\"";
        return Ok(value);
    }
    private async Task<IActionResult> Run(Func<Guid, UserRoleCode, Task<IActionResult>> execute)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Error(401, "unauthorized");
        try { return await execute(actor, role); }
        catch (WebReadException error) { return Error(error.Status, error.Code); }
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Web coordination request failed", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return new(problem) { StatusCode = status };
    }
}
