using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Services.Cases;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/cases")]
public sealed class CasesController : ControllerBase
{
    private ICaseWorkflowService? Service => HttpContext.RequestServices.GetService(typeof(ICaseWorkflowService)) as ICaseWorkflowService;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId = null, [FromQuery] string? status = null,
        [FromQuery] int pageSize = 20, [FromQuery] string? cursor = null, CancellationToken cancellationToken = default)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var service = Service;
        if (service is null) return Error(503, "dependency_unavailable");
        var parsed = status switch
        {
            null => (IncidentCaseStatus?)null,
            "UNASSIGNED" => IncidentCaseStatus.Unassigned,
            "OPEN" => IncidentCaseStatus.Open,
            "AWAITING_EVIDENCE" => IncidentCaseStatus.AwaitingEvidence,
            "CONCLUDED" => IncidentCaseStatus.Concluded,
            "LINKED" => IncidentCaseStatus.Linked,
            _ => (IncidentCaseStatus?)0
        };
        if (status is not null && parsed == 0 || projectId == Guid.Empty) return Error(400, "validation_error", "status");
        return Map(await service.ListAsync(actor, role, projectId, parsed, pageSize, cursor, cancellationToken));
    }

    [HttpGet("{caseId:guid}")]
    public async Task<IActionResult> Read(Guid caseId, CancellationToken cancellationToken)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var service = Service;
        if (service is null) return Error(503, "dependency_unavailable");
        return Map(await service.ReadAsync(actor, role, caseId, cancellationToken));
    }

    [HttpPost("{caseId:guid}/triage")]
    public Task<IActionResult> Triage(Guid caseId, CaseTriageDto request, CancellationToken cancellationToken)
        => Execute(caseId, version => new(caseId, version, "triage", request.Reason ?? "", ProjectId: request.ProjectId,
            Method: request.VerificationMethod switch
            {
                "FIELD" => CaseVerificationMethod.Field,
                "DRONE" => CaseVerificationMethod.Drone,
                "EXISTING_EVIDENCE" => CaseVerificationMethod.ExistingEvidence,
                _ => CaseVerificationMethod.Unknown
            },
            RouteVersionId: request.RouteVersionId, SegmentSetId: request.SegmentSetId, GeometryVersion: request.GeometryVersion), cancellationToken);

    [HttpPost("{caseId:guid}/report-links")]
    public Task<IActionResult> Link(Guid caseId, CaseLinkDto request, CancellationToken cancellationToken)
        => Execute(caseId, version => new(caseId, version, "link", request.Reason ?? "", ReportIds: request.ReportIds, SourceCaseVersions: request.SourceCaseVersions), cancellationToken);

    [HttpPost("{caseId:guid}/report-splits")]
    public Task<IActionResult> Split(Guid caseId, CaseSplitDto request, CancellationToken cancellationToken)
        => Execute(caseId, version => new(caseId, version, "split", request.Reason ?? "", ReportIds: request.ReportIds), cancellationToken);

    [HttpPost("{caseId:guid}/conclusions")]
    public Task<IActionResult> Conclude(Guid caseId, CaseConclusionDto request, CancellationToken cancellationToken)
        => Execute(caseId, version => new(caseId, version, "conclude", request.Reason ?? "", Outcome: request.Outcome switch
        { "CONFIRMED" => CaseConclusionOutcome.Confirmed, "NO_DEFECT" => CaseConclusionOutcome.NoDefect, "NEEDS_EVIDENCE" => CaseConclusionOutcome.NeedsEvidence, _ => null },
            DefectIds: request.DefectIds, EvidenceIds: request.EvidenceIds), cancellationToken);

    [HttpPost("{caseId:guid}/publications")]
    public Task<IActionResult> Publish(Guid caseId, CasePublicationDto request, CancellationToken cancellationToken)
        => Execute(caseId, version => new(caseId, version, "publish", request.Summary ?? "", ReportIds: request.ReportIds,
            DefectIds: request.DefectIds, EvidenceIds: request.EvidenceIds), cancellationToken);

    private async Task<IActionResult> Execute(Guid caseId, Func<string, CaseCommand> command, CancellationToken ct)
    {
        if (!Actor(out var actor, out var role)) return Error(401, "auth_unauthorized");
        var service = Service;
        if (service is null) return Error(503, "dependency_unavailable");
        if (!Request.Headers.TryGetValue("If-Match", out var header) || !Request.Headers.TryGetValue("Idempotency-Key", out var key)) return Error(428, "precondition_required");
        var raw = header.ToString();
        string version;
        try
        {
            if (raw.Length < 3 || raw[0] != '"' || raw[^1] != '"') return Error(400, "validation_error", "If-Match");
            var bytes = Convert.FromBase64String(raw[1..^1]); if (bytes.Length != 8) return Error(400, "validation_error", "If-Match"); version = Convert.ToBase64String(bytes);
        }
        catch (FormatException) { return Error(400, "validation_error", "If-Match"); }
        var correlation = Guid.TryParse(HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString(), out var value) ? value : (Guid?)null;
        return Map(await service.CommandAsync(actor, role, command(version), key.ToString(), correlation, ct));
    }

    private IActionResult Map(CaseWorkflowResult result)
    {
        if (result.Status is not (200 or 201)) return Error(result.Status, result.Code!, errors: result.Errors);
        var incident = result.Write?.Case ?? result.Case;
        if (incident is not null) Response.Headers.ETag = $"\"{incident.Version}\"";
        if (result.Status == 201)
        {
            var resource = result.Write!.Publication;
            return resource is not null ? Created($"/api/v1/cases/{resource.CaseId}", resource) : Created($"/api/v1/cases/{incident!.Id}", incident);
        }
        return result.Page is not null ? Ok(result.Page) : Ok(incident);
    }
    private bool Actor(out Guid actor, out UserRoleCode role)
    {
        role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actor)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? ""); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
    private ObjectResult Error(int status, string code, string? field = null, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails { Status = status, Title = "Case request failed", Detail = "The case request could not be completed.", Instance = Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        if (field is not null) problem.Extensions["errors"] = new Dictionary<string, string[]> { [field] = ["Invalid field."] };
        else if (errors is not null) problem.Extensions["errors"] = errors;
        return new(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
