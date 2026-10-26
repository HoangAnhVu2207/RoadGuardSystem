using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using RoadGuardSystem.API.Extensions;
using Xunit;
namespace RoadGuardSystem.ApiTests.Authentication;

public sealed class HandoffPolicyGateTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task PreviewCannotAuthorizeAiFromFeKeys(bool preview, bool aiEnabled)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Handoff:Enabled"] = preview.ToString(),
            ["AiServiceJwt:Enabled"] = aiEnabled.ToString()
        }).Build();
        var builder = new AuthorizationPolicyBuilder();
        HandoffPolicyGates.ConfigureAiCallback(builder, configuration);
        var policy = builder.Build();
        Assert.Equal(new[] { "AiServiceBearer" }, policy.AuthenticationSchemes);
        Assert.Contains(policy.Requirements, x => x is DenyAnonymousAuthorizationRequirement);
        // RS256 callback identity is validated by AiServiceBearer, independently
        // of the FE key/claim namespace and the preview switch.
        var context = new AuthorizationHandlerContext(policy.Requirements, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "synthetic-ai"), new Claim("client_type", "AI_SERVICE") }, "test")), null);
        var requirement = Assert.Single(policy.Requirements.OfType<AssertionRequirement>());
        Assert.Equal(aiEnabled, await requirement.Handler(context));
    }
}
