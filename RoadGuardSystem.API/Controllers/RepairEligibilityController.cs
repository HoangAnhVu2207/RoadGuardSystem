using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Authorization;
using RoadGuardSystem.Services.Repairs;
namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RoadGuardSystem.API.Authentication.WebCookieEligible]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/repair-packages/{packageId:guid}/items/{itemId:guid}/eligibility")]
public sealed class RepairEligibilityController(IRepairEligibilityService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid projectId, Guid packageId, Guid itemId, CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actor) ||
            !ProjectRoleClaimParser.TryParse(User.FindFirstValue("role"), out var role)) return Unauthorized();
        var result = await service.ReadAsync(actor, role, projectId, packageId, itemId, token);
        if (result.Code is not null)
        {
            var problem = new ProblemDetails { Status = result.Status, Title = "Repair eligibility source unavailable" };
            problem.Extensions["code"] = result.Code;
            problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
            return new ObjectResult(problem) { StatusCode = result.Status, ContentTypes = { "application/problem+json" } };
        }
        Response.Headers.ETag = $"\"{result.Value!.Version}\"";
        return Ok(result.Value);
    }
}
