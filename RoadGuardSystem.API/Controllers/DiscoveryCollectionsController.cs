using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Collections;
using RoadGuardSystem.Repositories.Collections;
using RoadGuardSystem.Services.Collections;
namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible, Route("api/v{version:apiVersion}/projects/{projectId:guid}")]
public sealed class DiscoveryCollectionsController(CollectionReadService service, IDataProtectionProvider protection) : ControllerBase
{
    [HttpGet("repair-packages")]
    [ProducesResponseType<CollectionPage<RepairPackageSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> RepairPackages(Guid projectId, CancellationToken ct) => Read<RepairPackageSummary>(projectId, CollectionResource.RepairPackages, null, ct);

    [HttpGet("repair-packages/{packageId:guid}/items")]
    [ProducesResponseType<CollectionPage<RepairItemSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> RepairItems(Guid projectId, Guid packageId, CancellationToken ct) => Read<RepairItemSummary>(projectId, CollectionResource.RepairItems, packageId, ct);

    [HttpGet("exports")]
    [ProducesResponseType<CollectionPage<ExportSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> Exports(Guid projectId, CancellationToken ct) => Read<ExportSummary>(projectId, CollectionResource.Exports, null, ct);

    [HttpGet("survey-plans")]
    [ProducesResponseType<CollectionPage<SurveyPlanSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> SurveyPlans(Guid projectId, CancellationToken ct) => Read<SurveyPlanSummary>(projectId, CollectionResource.SurveyPlans, null, ct);

    [HttpGet("survey-tasks")]
    [ProducesResponseType<CollectionPage<SurveyTaskSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> SurveyTasks(Guid projectId, CancellationToken ct) => Read<SurveyTaskSummary>(projectId, CollectionResource.SurveyTasks, null, ct);

    [HttpGet("processing-jobs")]
    [ProducesResponseType<CollectionPage<ProcessingJobSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> ProcessingJobs(Guid projectId, CancellationToken ct) => Read<ProcessingJobSummary>(projectId, CollectionResource.ProcessingJobs, null, ct);

    [HttpGet("ai-mock-runs")]
    [ProducesResponseType<CollectionPage<MockRunSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> MockRuns(Guid projectId, CancellationToken ct) => Read<MockRunSummary>(projectId, CollectionResource.MockRuns, null, ct);

    [HttpGet("offline/devices")]
    [ProducesResponseType<CollectionPage<OfflineDeviceSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> OfflineDevices(Guid projectId, CancellationToken ct) => Read<OfflineDeviceSummary>(projectId, CollectionResource.OfflineDevices, null, ct);

    [HttpGet("offline/packages")]
    [ProducesResponseType<CollectionPage<OfflinePackageSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> OfflinePackages(Guid projectId, CancellationToken ct) => Read<OfflinePackageSummary>(projectId, CollectionResource.OfflinePackages, null, ct);

    [HttpGet("offline/grants")]
    [ProducesResponseType<CollectionPage<OfflineGrantSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> OfflineGrants(Guid projectId, CancellationToken ct) => Read<OfflineGrantSummary>(projectId, CollectionResource.OfflineGrants, null, ct);

    [HttpGet("retention/evaluations")]
    [ProducesResponseType<CollectionPage<RetentionEvaluationSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public Task<IActionResult> RetentionEvaluations(Guid projectId, CancellationToken ct) => Read<RetentionEvaluationSummary>(projectId, CollectionResource.RetentionEvaluations, null, ct);

    private async Task<IActionResult> Read<T>(Guid project, CollectionResource resource, Guid? parent, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) || actor == Guid.Empty)
            return Error(401, "unauthorized");
        UserRoleCode role;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role")); }
        catch (ArgumentOutOfRangeException) { return Error(401, "unauthorized"); }
        try
        {
            // Admission precedes query/token validation to avoid revealing non-visible parents.
            await service.AuthorizeAsync(actor, role, project, resource, parent, ct);
            var allowed = resource == CollectionResource.RepairItems ? new[] { "limit", "cursor", "mode" } : new[] { "limit", "cursor" };
            if (Request.Query.Any(x => !allowed.Contains(x.Key, StringComparer.Ordinal) || x.Value.Count != 1)) return Error(400, "validation_error");
            var limit = 25;
            if (Request.Query.TryGetValue("limit", out var size) && (!int.TryParse(size.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out limit) || limit is < 1 or > 100)) return Error(400, "validation_error");
            string? mode = Request.Query.TryGetValue("mode", out var filter) ? filter.ToString() : null;
            if (mode is not (null or "NORMAL" or "FAST_TRACK")) return Error(400, "validation_error");
            var cursor = protection.CreateProtector("RoadGuard.Collection.v1", actor.ToString("D"), project.ToString("D"), resource.ToString(), parent?.ToString("D") ?? "", mode ?? "");
            Guid? after = null;
            if (Request.Query.TryGetValue("cursor", out var token))
            {
                if (token.ToString().Length is 0 or > 4096) return Error(400, "validation_error");
                try
                {
                    if (!Guid.TryParseExact(cursor.Unprotect(token.ToString()), "D", out var id) || id == Guid.Empty) return Error(400, "validation_error");
                    after = id;
                }
                catch (CryptographicException) { return Error(400, "validation_error"); }
            }
            var page = await service.ReadAsync<T>(actor, role, project, resource, parent, mode, after, limit, ct);
            Response.Headers.CacheControl = "private, no-store";
            return Ok(new CollectionPage<T>(page.Items, page.NextId is { } next ? cursor.Protect(next.ToString("D")) : null));
        }
        catch (CollectionRequestException ex) { return Error(ex.Status, ex.Code); }
        catch (RoadGuardSystem.Repositories.Processing.AiRequestException ex) { return Error(ex.Status, ex.Code); }
        catch (RoadGuardSystem.Repositories.Retention.RetentionRequestException ex) { return Error(ex.Status, ex.Code); }
    }
    private ObjectResult Error(int status, string code)
    {
        Response.Headers.CacheControl = "private, no-store";
        var problem = new ProblemDetails { Type = "about:blank", Title = "Collection request failed", Status = status, Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] ?? HttpContext.TraceIdentifier;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
