using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.API.Authentication;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Services.Defects;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/catalog/defect-types")]
public sealed class DefectTypesController(IDefectWorkflowService service) : ControllerBase
{
    [HttpGet, WebCookieEligible]
    [ProducesResponseType<DefectTypeOptionDto[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role) ||
            role != UserRoleCode.ProjectManager)
            return Forbid();

        return Ok(await service.ListActiveTypesAsync(cancellationToken));
    }
}
