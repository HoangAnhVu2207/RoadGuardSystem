using FluentAssertions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class JwtOptionsTests
{
    [Fact(DisplayName = "P1-10 Negative: missing issuer, audience, active key, and lifetimes are rejected")]
    public void Validate_MissingRequiredSettings_Fails()
    {
        var result = new JwtOptionsValidator().Validate(Options.DefaultName, new JwtOptions());

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Issuer");
        result.FailureMessage.Should().Contain("Audience");
        result.FailureMessage.Should().Contain("ActiveKeyId");
        result.FailureMessage.Should().Contain("AccessTokenLifetimeMinutes");
    }

    [Fact(DisplayName = "P1-10 Negative: unknown active key and unsafe lifetime are rejected")]
    public void Validate_UnknownKeyAndUnsafeLifetime_Fails()
    {
        var options = new JwtOptions
        {
            Issuer = "roadguard",
            Audience = "roadguard-api",
            ActiveKeyId = "current",
            SigningKeys = new Dictionary<string, string> { ["old"] = Convert.ToBase64String(new byte[32]) },
            AccessTokenLifetimeMinutes = 0,
            SessionLifetimeHours = 0,
            RefreshTokenLifetimeDays = 0
        };

        var result = new JwtOptionsValidator().Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("active signing key");
        result.FailureMessage.Should().Contain("positive");
    }

    [Fact(DisplayName = "P1-10 Positive: configured key ring and lifetimes validate")]
    public void Validate_CompleteSettings_Passes()
    {
        var options = new JwtOptions
        {
            Issuer = "roadguard",
            Audience = "roadguard-api",
            ActiveKeyId = "current",
            SigningKeys = new Dictionary<string, string> { ["current"] = Convert.ToBase64String(new byte[32]) },
            AccessTokenLifetimeMinutes = 10,
            SessionLifetimeHours = 24,
            RefreshTokenLifetimeDays = 30
        };

        new JwtOptionsValidator().Validate(Options.DefaultName, options).Succeeded.Should().BeTrue();
    }
}
