using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Authentication;

namespace RoadGuardSystem.API.Controllers;

[ApiController, ApiVersion("1.0"), AllowAnonymous]
[Route("api/v{version:apiVersion}/auth/reporter/password-reset")]
public sealed class ReporterPasswordResetController(ReporterPasswordResetService service) : ControllerBase
{
    public sealed record ReporterPasswordResetRequestDto([Required, EmailAddress, StringLength(254)] string Email);

    public sealed record ReporterPasswordResetConfirmDto(
        [Required, EmailAddress, StringLength(254)] string Email,
        [Required, RegularExpression("^[0-9]{6}$")] string Otp,
        [Required, StringLength(1024)] string NewPassword);

    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestOtp([FromBody, Required] ReporterPasswordResetRequestDto request, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        await service.RequestAsync(request.Email, token);
        return Accepted();
    }

    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm([FromBody, Required] ReporterPasswordResetConfirmDto request, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await service.ConfirmAsync(request.Email, request.Otp, request.NewPassword, token);
        if (result == ReporterResetOutcome.Success) return NoContent();
        var invalidPassword = result == ReporterResetOutcome.InvalidPassword;
        var details = new ProblemDetails
        {
            Status = invalidPassword ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status400BadRequest,
            Title = "Password reset could not be completed"
        };
        details.Extensions["code"] = invalidPassword ? "password_policy_failed" : "invalid_reset_otp";
        return new ObjectResult(details) { StatusCode = details.Status, ContentTypes = { "application/problem+json" } };
    }
}
