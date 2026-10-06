using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Retention;
using RoadGuardSystem.Services.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class RetentionController(IRetentionService service) : ControllerBase
{
    [HttpGet("projects/{projectId:guid}/retention/files/{fileId:guid}")]
    public Task<IActionResult> File(Guid projectId, Guid fileId, CancellationToken token) => Run(async actor =>
    {
        var view = await service.GetFileAsync(actor, projectId, fileId, token); Tag(view.BasisVersion); return Ok(view);
    });
    [HttpPut("projects/{projectId:guid}/retention/files/{fileId:guid}/basis")]
    public Task<IActionResult> Basis(Guid projectId, Guid fileId, ConfirmRetentionBasisRequest request, CancellationToken token) => Run(async actor =>
    {
        var view = await service.ConfirmBasisAsync(actor, projectId, fileId, request, Key(), Expected(), token); Tag(view.Version); return Ok(view);
    });
    [HttpPost("projects/{projectId:guid}/retention/evaluations")]
    public Task<IActionResult> Evaluate(Guid projectId, CreateRetentionEvaluationRequest request, CancellationToken token) => Run(async actor =>
    {
        var view = await service.AdmitEvaluationAsync(actor, projectId, request, Key(), token); Tag(view.Version);
        Response.Headers.Location = $"/api/v1/projects/{projectId}/retention/evaluations/{view.Id}"; return StatusCode(202, view);
    });
    [HttpGet("projects/{projectId:guid}/retention/evaluations/{id:guid}")]
    public Task<IActionResult> Evaluation(Guid projectId, Guid id, CancellationToken token, [FromQuery] Guid? cursor = null, [FromQuery] int pageSize = 50) => Run(async actor =>
    {
        var view = await service.GetEvaluationAsync(actor, projectId, id, cursor, pageSize, token); Tag(view.Version); return Ok(view);
    });
    [HttpPost("retention/holds")]
    public Task<IActionResult> CreateHold(CreateRetentionHoldRequest request, CancellationToken token) => Run(async actor =>
    {
        var view = await service.CreateHoldAsync(actor, request, Key(), token); Tag(view.Version);
        Response.Headers.Location = $"/api/v1/retention/holds/{view.Id}"; return StatusCode(201, view);
    });
    [HttpGet("retention/holds/{id:guid}")]
    public Task<IActionResult> Hold(Guid id, CancellationToken token) => Run(async actor =>
    {
        var view = await service.GetHoldAsync(actor, id, token); Tag(view.Version); return Ok(view);
    });
    [HttpPost("retention/holds/{id:guid}/release")]
    public Task<IActionResult> Release(Guid id, ReleaseRetentionHoldRequest request, CancellationToken token) => Run(async actor =>
    {
        var view = await service.ReleaseHoldAsync(actor, id, request, Key(), Expected(), token); Tag(view.Version); return Ok(view);
    });
    private async Task<IActionResult> Run(Func<Guid, Task<IActionResult>> action)
    {
        if (!Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) || actor == Guid.Empty) return Error(401, "unauthorized");
        try
        {
            UserRoleCode role;
            try { role = UserRoleCodeExtensions.FromDbCode(User.FindFirstValue("role")); }
            catch (ArgumentOutOfRangeException) { return Error(401, "unauthorized"); }
            await service.AuthorizePrincipalAsync(actor, role, HttpContext.RequestAborted);
            return await action(actor);
        }
        catch (RetentionRequestException exception) { return Error(exception.Status, exception.Code); }
    }
    private ObjectResult Error(int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = "Retention request could not be completed", Detail = "The request failed a retention scope, policy, or concurrency check.", Instance = Request.Path };
        problem.Extensions["code"] = code; problem.Extensions["correlationId"] = HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] ?? HttpContext.TraceIdentifier;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
    private string Key() => Request.Headers["Idempotency-Key"].Count == 1 ? Request.Headers["Idempotency-Key"].ToString() : "";
    private string Expected()
    {
        if (!Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value)) throw new RetentionRequestException(428, "validation_error");
        var text = value.ToString();
        if (value.Count != 1 || text.Length < 3 || text[0] != '"' || text[^1] != '"' || text[1..^1].Contains('"')) throw new RetentionRequestException(412, "concurrency_conflict");
        return text[1..^1];
    }
    private void Tag(string version) => Response.Headers.ETag = $"\"{version}\"";
}
