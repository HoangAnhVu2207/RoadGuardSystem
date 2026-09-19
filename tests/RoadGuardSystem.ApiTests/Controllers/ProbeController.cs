using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoadGuardSystem.ApiTests.Controllers;

/// <summary>
/// Test-only probe controller used strictly within ApiTests to verify API platform behaviors:
/// routing, versioning, ProblemDetails error envelopes, unhandled exception masking, and validation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProbeController : ControllerBase
{
    public const string ExceptionSecretMessage = "DB_CONNECTION_PASSWORD_SECRET_12345_INTERNAL_STACK";

    [HttpGet("ok")]
    public IActionResult GetOk()
    {
        return Ok(new { message = "probe_ok" });
    }

    [Authorize]
    [HttpGet("protected")]
    public IActionResult GetProtected() => Ok(new { message = "protected_ok" });

    [HttpGet("throw")]
    public IActionResult ThrowUnhandled()
    {
        throw new InvalidOperationException($"Simulated unhandled domain failure: {ExceptionSecretMessage}");
    }

    [HttpPost("validate")]
    public IActionResult ValidatePayload([FromBody] ProbePayloadDto payload)
    {
        return Ok(new { message = "validated", payload });
    }
}

public class ProbePayloadDto
{
    [Required]
    [StringLength(10, MinimumLength = 3)]
    public string? Name { get; set; }

    [Range(1, 100)]
    public int Score { get; set; }
}
