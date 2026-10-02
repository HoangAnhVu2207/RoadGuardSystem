using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
public sealed class ReportsController(IReporterReportService service) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReporterReportRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorId, out var role)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return StatusCode(428, new { code = "precondition_required" });
        var result = await service.CreateAsync(actorId, role, request, idempotencyKey, cancellationToken);
        if (result.Report is not null) Response.Headers.ETag = $"\"{result.Report.Version}\"";
        return result.Status switch
        {
            ReporterReportCommandStatus.Created or ReporterReportCommandStatus.Replayed when result.Report is not null => Created($"/api/v1/reports/{result.Report.Id:D}", result.Report),
            ReporterReportCommandStatus.Forbidden => StatusCode(403),
            ReporterReportCommandStatus.NotFound => NotFound(),
            ReporterReportCommandStatus.SourceNotReady => Conflict(new { code = "source_not_ready" }),
            ReporterReportCommandStatus.IdempotencyConflict => Conflict(new { code = "idempotency_key_reused" }),
            _ => UnprocessableEntity(new { code = "validation_error" })
        };
    }

    private bool TryGetActor(out Guid actorId, out UserRoleCode role)
    {
        actorId = Guid.Empty; role = UserRoleCode.Unknown;
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out actorId)) return false;
        try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role") ?? string.Empty); return role != UserRoleCode.Unknown; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}
