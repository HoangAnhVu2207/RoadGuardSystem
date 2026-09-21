using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.API.Middlewares;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Options;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.API.Authentication;

internal static class JwtBearerConfiguration
{
    internal const string AuthErrorCodeItemKey = "RoadGuard.AuthErrorCode";

    public static void Configure(JwtBearerOptions bearerOptions, JwtOptions options)
    {
        bearerOptions.MapInboundClaims = false;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = "role",
            IssuerSigningKeyResolver = (_, _, keyId, _) =>
            {
                if (string.IsNullOrWhiteSpace(keyId) || !options.SigningKeys.TryGetValue(keyId, out var encodedKey))
                {
                    return [];
                }

                return [new SymmetricSecurityKey(JwtOptionsValidator.DecodeKey(encodedKey)) { KeyId = keyId }];
            }
        };
        bearerOptions.Events = new JwtBearerEvents
        {
            OnTokenValidated = ValidateAuthoritativeStateAsync,
            OnChallenge = WriteChallengeAsync
        };
    }

    private static async Task ValidateAuthoritativeStateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var subject = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var session = principal?.FindFirstValue("sid");
        var role = principal?.FindFirstValue("role");
        if (!Guid.TryParse(subject, out var userId) ||
            !Guid.TryParse(session, out var sessionId) ||
            !TryParseRole(role, out var roleCode))
        {
            context.HttpContext.Items[AuthErrorCodeItemKey] = ApiErrorCodes.Unauthorized;
            context.Fail("Required identity claims are invalid.");
            return;
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<AuthoritativeSessionValidator>();
        var validation = await validator.ValidateAsync(
            userId,
            sessionId,
            roleCode,
            DateTimeOffset.UtcNow,
            context.HttpContext.RequestAborted);
        if (validation != AuthoritativeSessionValidation.Success)
        {
            context.HttpContext.Items[AuthErrorCodeItemKey] = validation == AuthoritativeSessionValidation.SessionRevoked
                ? ApiErrorCodes.SessionRevoked
                : ApiErrorCodes.Unauthorized;
            context.Fail("Authoritative session validation failed.");
        }
    }

    private static async Task WriteChallengeAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var errorCode = context.HttpContext.Items[AuthErrorCodeItemKey] as string ?? ApiErrorCodes.Unauthorized;
        var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "Authentication is required or the supplied credential is no longer valid.",
            Instance = context.Request.Path,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
        };
        problem.Extensions["code"] = errorCode;
        problem.Extensions["correlationId"] = correlationId;
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, context.HttpContext.RequestAborted);
    }

    private static bool TryParseRole(string? value, out UserRoleCode roleCode)
    {
        try
        {
            roleCode = UserRoleCodeExtensions.FromDbCode(value ?? string.Empty);
            return roleCode != UserRoleCode.Unknown;
        }
        catch (ArgumentOutOfRangeException)
        {
            roleCode = UserRoleCode.Unknown;
            return false;
        }
    }
}
