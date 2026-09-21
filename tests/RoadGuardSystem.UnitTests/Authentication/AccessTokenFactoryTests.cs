using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class AccessTokenFactoryTests
{
    [Fact(DisplayName = "P1-10 Negative: token creation rejects an unknown role")]
    public void Create_UnknownRole_Throws()
    {
        var factory = new AccessTokenFactory(CreateOptions());

        var act = () => factory.Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.Unknown, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "P1-10 Negative: token creation rejects empty identity claims")]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_EmptyIdentity_Throws(bool emptyUserId)
    {
        var userId = emptyUserId ? Guid.Empty : Guid.NewGuid();
        var sessionId = emptyUserId ? Guid.NewGuid() : Guid.Empty;
        var factory = new AccessTokenFactory(CreateOptions());

        var act = () => factory.Create(userId, sessionId, UserRoleCode.Supervisor, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "P1-10 Positive: access token carries only the authoritative identity snapshot claims")]
    public void Create_ValidIdentity_EmitsRequiredClaims()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 19, 2, 0, 0, TimeSpan.Zero);
        var token = new AccessTokenFactory(CreateOptions()).Create(userId, sessionId, UserRoleCode.ProjectManager, now);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(userId.ToString());
        jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value.Should().NotBeNullOrWhiteSpace();
        jwt.Claims.Single(c => c.Type == "sid").Value.Should().Be(sessionId.ToString());
        jwt.Claims.Single(c => c.Type == "role").Value.Should().Be("PM");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Nbf);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Exp);
        jwt.Issuer.Should().Be("roadguard");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("roadguard-api");
        jwt.Header.Kid.Should().Be("current");
    }

    [Fact(DisplayName = "P1-10 Positive: access token uses the configured issuance and expiry bounds")]
    public void Create_ValidIdentity_UsesConfiguredTemporalBounds()
    {
        var options = CreateOptions();
        var issuedAt = new DateTimeOffset(2026, 9, 19, 3, 15, 20, TimeSpan.Zero);

        var token = new AccessTokenFactory(options)
            .Create(Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.DroneOperator, issuedAt);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.IssuedAt.Should().Be(issuedAt.UtcDateTime);
        jwt.ValidFrom.Should().Be(issuedAt.UtcDateTime);
        jwt.ValidTo.Should().Be(issuedAt.AddMinutes(10).UtcDateTime);
    }

    [Fact(DisplayName = "P1-10 Positive: tokens issued to different users have distinct JWT ids")]
    public void Create_DifferentUsers_EmitsDistinctJtiClaims()
    {
        var factory = new AccessTokenFactory(CreateOptions());
        var issuedAt = new DateTimeOffset(2026, 9, 19, 3, 15, 20, TimeSpan.Zero);
        var firstUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var secondUserId = Guid.Parse("10000000-0000-0000-0000-000000000002");

        var first = new JwtSecurityTokenHandler().ReadJwtToken(
            factory.Create(firstUserId, Guid.Parse("20000000-0000-0000-0000-000000000001"), UserRoleCode.ProjectManager, issuedAt));
        var second = new JwtSecurityTokenHandler().ReadJwtToken(
            factory.Create(secondUserId, Guid.Parse("20000000-0000-0000-0000-000000000002"), UserRoleCode.ProjectManager, issuedAt));

        var firstJti = Guid.Parse(first.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
        var secondJti = Guid.Parse(second.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value);

        firstJti.Should().NotBe(Guid.Empty);
        secondJti.Should().NotBe(Guid.Empty);
        secondJti.Should().NotBe(firstJti);
    }

    private static JwtOptions CreateOptions() => new()
    {
        Issuer = "roadguard",
        Audience = "roadguard-api",
        ActiveKeyId = "current",
        SigningKeys = new Dictionary<string, string> { ["current"] = Convert.ToBase64String(new byte[32]) },
        AccessTokenLifetimeMinutes = 10,
        SessionLifetimeHours = 24,
        RefreshTokenLifetimeDays = 30
    };
}
