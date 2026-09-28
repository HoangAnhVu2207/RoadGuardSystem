using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.DTOs.Authentication;
using RoadGuardSystem.Services.Authentication;

namespace RoadGuardSystem.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/auth/reporter-registrations")]
public sealed class ReporterRegistrationsController : ControllerBase
{
    private readonly IIdentityOnboardingService _service;

    public ReporterRegistrationsController(IIdentityOnboardingService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Register(
        RegisterReporterRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _service.RegisterReporterAsync(
            request.Email!, request.Password!, request.DisplayName!, request.ReporterType!,
            idempotencyKey!, cancellationToken);
        if (result.Status is IdentityOnboardingStatus.Success or IdentityOnboardingStatus.IdempotentReplay &&
            result.RegistrationIntent is not null)
        {
            var response = ToResponse(result.RegistrationIntent);
            return Accepted($"/api/v1/auth/reporter-registrations/{response.IntentId:D}", response);
        }

        return Map(result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        VerifyReporterOtpRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _service.VerifyReporterOtpAsync(
            request.IntentId, request.Otp!, idempotencyKey!, cancellationToken);
        return result.Tokens is null ? Map(result) : Ok(ToTokenResponse(result.Tokens));
    }

    [HttpPost("resend")]
    public async Task<IActionResult> Resend(
        ResendReporterOtpRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _service.ResendReporterOtpAsync(
            request.IntentId, idempotencyKey!, cancellationToken);
        if (result.Status is IdentityOnboardingStatus.Success or IdentityOnboardingStatus.IdempotentReplay &&
            result.RegistrationIntent is not null)
        {
            var response = ToResponse(result.RegistrationIntent);
            return Accepted($"/api/v1/auth/reporter-registrations/{response.IntentId:D}", response);
        }

        return Map(result);
    }

    private ObjectResult Map(IdentityOnboardingResult result) => result.Status switch
    {
        IdentityOnboardingStatus.TooManyRequests => ProblemResponse(
            StatusCodes.Status429TooManyRequests, ApiErrorCodes.ValidationError, "Too many requests"),
        IdentityOnboardingStatus.NotFound => ProblemResponse(
            StatusCodes.Status404NotFound, ApiErrorCodes.NotFound, "Not found"),
        IdentityOnboardingStatus.Conflict => ProblemResponse(
            StatusCodes.Status409Conflict, ApiErrorCodes.EmailConflict, "Conflict"),
        IdentityOnboardingStatus.IdempotencyConflict => ProblemResponse(
            StatusCodes.Status409Conflict, ApiErrorCodes.DuplicateRequest, "Duplicate request"),
        IdentityOnboardingStatus.DeliveryUnavailable => ProblemResponse(
            StatusCodes.Status503ServiceUnavailable, ApiErrorCodes.InternalError, "Delivery unavailable"),
        _ => ProblemResponse(StatusCodes.Status400BadRequest, ApiErrorCodes.ValidationError, "Invalid request")
    };

    private ObjectResult ProblemResponse(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = "The reporter registration request could not be completed.",
            Instance = Request.Path,
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{(status == 400 ? "1" : status == 404 ? "5" : status == 409 ? "10" : status == 429 ? "9" : "14")}"
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] =
            HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static RegistrationIntentResponseDto ToResponse(RegistrationIntentView intent) => new(
        intent.Id,
        "PENDING_VERIFICATION",
        intent.ResendAfterSeconds);

    private static AuthTokenResponseDto ToTokenResponse(AuthTokens tokens) => new(
        tokens.AccessToken,
        tokens.RefreshToken,
        "Bearer",
        tokens.ExpiresIn,
        tokens.User.MustChangePassword,
        new AuthActorResponseDto(
            tokens.User.Id,
            tokens.User.DisplayName,
            "REPORTER",
            Convert.ToBase64String(tokens.User.RowVersion)));
}
