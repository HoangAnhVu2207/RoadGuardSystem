using Microsoft.AspNetCore.Authorization;
using RoadGuardSystem.API.Authentication;

namespace RoadGuardSystem.API.Extensions;

/// <summary>Preview credentials must never become an adopted AI service identity.</summary>
public static class HandoffPolicyGates
{
    public static void ConfigureAiCallback(AuthorizationPolicyBuilder policy, IConfiguration configuration)
    {
        policy.AddAuthenticationSchemes(AiServiceJwtBearerConfiguration.Scheme);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(_ => configuration.GetValue<bool>("AiServiceJwt:Enabled"));
    }
}
