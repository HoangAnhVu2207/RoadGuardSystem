using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RoadGuardSystem.Services.Processing;

namespace RoadGuardSystem.API.Authentication;

internal static class AiServiceJwtBearerConfiguration
{
    internal const string Scheme = "AiServiceBearer";
    internal const string Audience = AiCallbackTokens.Audience;
    public static void Configure(JwtBearerOptions options, AiCallbackTokens tokens)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokens.ValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                try { tokens.ValidateClaims(context.Principal!); }
                catch (SecurityTokenException) { context.Fail("AI service identity claims are invalid."); }
                return Task.CompletedTask;
            },
            OnChallenge = JwtBearerConfiguration.WriteChallengeAsync
        };
    }
}
