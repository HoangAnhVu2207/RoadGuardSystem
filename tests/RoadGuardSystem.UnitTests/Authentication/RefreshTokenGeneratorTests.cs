using FluentAssertions;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Generators;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class RefreshTokenGeneratorTests
{
    [Fact(DisplayName = "P1-10 Negative: token hash never accepts a blank token")]
    public void Hash_BlankToken_Throws()
    {
        var act = () => RefreshTokenGenerator.Hash(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "P1-10 Positive: generated token is opaque and hash-only material is deterministic")]
    public void Generate_ReturnsHighEntropyPlaintextAndHash()
    {
        var first = RefreshTokenGenerator.Generate();
        var second = RefreshTokenGenerator.Generate();

        first.Plaintext.Should().NotBeNullOrWhiteSpace();
        first.Plaintext.Should().NotBe(second.Plaintext);
        first.HashHex.Should().MatchRegex("^[0-9a-f]{64}$");
        RefreshTokenGenerator.Hash(first.Plaintext).Should().Be(first.HashHex);
        first.HashHex.Should().NotContain(first.Plaintext);
    }
}
