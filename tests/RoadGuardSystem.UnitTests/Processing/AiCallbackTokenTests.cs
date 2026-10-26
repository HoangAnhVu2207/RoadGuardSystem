using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using RoadGuardSystem.Services.Processing;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiCallbackTokenTests
{
    [Fact]
    public void RepeatedAndConcurrentMintDoNotReuseDisposedRsaHandles()
    {
        using var rsa = RSA.Create(2048);
        var tokens = new AiCallbackTokens(Options(rsa), TimeProvider.System);
        var job = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var values = Enumerable.Range(0, 20).AsParallel().Select(_ => tokens.Mint(job, attempt)).ToArray();
        values.Select(raw => tokens.Validate(raw).FindFirst("jti")!.Value).Distinct().Should().HaveCount(20);
    }
    [Fact]
    public void MintUsesDedicatedRsaIdentityAndExactlyFiveMinutes()
    {
        using var rsa = RSA.Create(2048);
        var options = Options(rsa);
        var tokens = new AiCallbackTokens(options, TimeProvider.System);
        var job = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var raw = tokens.Mint(job, attempt);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);
        jwt.Header.Alg.Should().Be("RS256"); jwt.Header.Kid.Should().Be("current");
        jwt.Issuer.Should().Be("roadguard-backend"); jwt.Audiences.Should().Equal("roadguard-ai-callback");
        (jwt.ValidTo - jwt.ValidFrom).TotalSeconds.Should().Be(300);
        var principal = tokens.Validate(raw);
        AiCallbackTokens.Matches(principal, job, attempt).Should().BeTrue();
        AiCallbackTokens.Matches(principal, Guid.NewGuid(), attempt).Should().BeFalse();
        AiCallbackTokens.Matches(principal, job, Guid.NewGuid()).Should().BeFalse();
        jwt.Claims.Single(c => c.Type == "sub").Value.Should().Be(options.Subject);
        jwt.Claims.Single(c => c.Type == "jti").Value.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("iss")]
    [InlineData("aud")]
    [InlineData("sub")]
    [InlineData("kid")]
    [InlineData("jti")]
    [InlineData("iat")]
    [InlineData("exp")]
    [InlineData("jobId")]
    [InlineData("attemptId")]
    [InlineData("expired")]
    [InlineData("long")]
    [InlineData("future")]
    [InlineData("hs256")]
    public void InvalidIdentityAndLifetimeAreRejected(string change)
    {
        using var rsa = RSA.Create(2048);
        var options = Options(rsa); var tokens = new AiCallbackTokens(options, TimeProvider.System);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new JwtPayload
        {
            ["iss"] = AiCallbackTokens.Issuer,
            ["aud"] = AiCallbackTokens.Audience,
            ["sub"] = options.Subject,
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["iat"] = now,
            ["nbf"] = now,
            ["exp"] = now + 300,
            ["jobId"] = Guid.NewGuid().ToString(),
            ["attemptId"] = Guid.NewGuid().ToString()
        };
        if (change is "iss" or "aud" or "sub") payload[change] = "wrong";
        else if (change is "jti" or "iat" or "exp" or "jobId" or "attemptId") payload.Remove(change);
        else if (change == "expired") { payload["iat"] = now - 600; payload["nbf"] = now - 600; payload["exp"] = now - 300; }
        else if (change == "long") payload["exp"] = now + 301;
        else if (change == "future") payload["iat"] = now + 5;
        SecurityKey key = change == "hs256" ? new SymmetricSecurityKey(new byte[32]) : new RsaSecurityKey(rsa);
        key.KeyId = change == "kid" ? "unknown" : "current";
        var jwt = new JwtSecurityToken(new JwtHeader(new SigningCredentials(key, change == "hs256" ? "HS256" : "RS256")), payload);
        var raw = new JwtSecurityTokenHandler().WriteToken(jwt);
        Action validate = () => tokens.Validate(raw);
        validate.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void RetiredPublicKeyValidatesUntilTokenExpiryAndCannotMint()
    {
        using var old = RSA.Create(2048); using var next = RSA.Create(2048);
        var original = Options(old);
        var token = new AiCallbackTokens(original, TimeProvider.System).Mint(Guid.NewGuid(), Guid.NewGuid());
        var rotated = new AiServiceTokenOptions
        {
            Subject = original.Subject,
            ActiveKeyId = "next",
            PrivateKeyPem = next.ExportRSAPrivateKeyPem(),
            PublicKeys = new() { ["current"] = old.ExportRSAPublicKeyPem(), ["next"] = next.ExportRSAPublicKeyPem() }
        };
        new AiCallbackTokens(rotated, TimeProvider.System).Validate(token).Identity!.IsAuthenticated.Should().BeTrue();
        original.PrivateKeyPem = "";
        Action mint = () => new AiCallbackTokens(original, TimeProvider.System).Mint(Guid.NewGuid(), Guid.NewGuid());
        mint.Should().Throw<InvalidOperationException>();
    }

    private static AiServiceTokenOptions Options(RSA rsa) => new()
    {
        Subject = "roadguard-ai-service",
        ActiveKeyId = "current",
        PrivateKeyPem = rsa.ExportRSAPrivateKeyPem(),
        PublicKeys = new() { ["current"] = rsa.ExportRSAPublicKeyPem() }
    };
}
