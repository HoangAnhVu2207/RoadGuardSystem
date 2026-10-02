using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.API.Authentication;

internal static class AiServiceJwtBearerConfiguration
{
    internal const string Scheme = "AiServiceBearer";
    internal const string Audience = "roadguard-be-ai";

    public static void Configure(JwtBearerOptions bearerOptions, JwtOptions options)
    {
        bearerOptions.MapInboundClaims = false;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            NameClaimType = JwtRegisteredClaimNames.Sub,
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
            OnTokenValidated = context =>
            {
                var subject = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var clientType = context.Principal?.FindFirst("client_type")?.Value;
                if (string.IsNullOrWhiteSpace(subject) || !string.Equals(clientType, "AI_SERVICE", StringComparison.Ordinal))
                {
                    context.Fail("AI service identity claims are invalid.");
                }

                return Task.CompletedTask;
            },
            OnChallenge = JwtBearerConfiguration.WriteChallengeAsync
        };
    }
}
