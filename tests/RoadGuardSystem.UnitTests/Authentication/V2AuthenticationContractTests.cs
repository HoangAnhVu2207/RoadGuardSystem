using FluentAssertions;
using RoadGuardSystem.DTOs.Authentication;
using RoadGuardSystem.Services.Authentication;
using Xunit;

namespace RoadGuardSystem.UnitTests.Authentication;

[Trait("TaskId", "V2-P1-001..005")]
public sealed class V2AuthenticationContractTests
{
    [Fact]
    public void LoginRequest_UsesEmailInsteadOfUsername()
    {
        var request = new LoginRequestDto
        {
            Email = "field.user@example.test",
            Password = "Current1!"
        };

        request.Email.Should().Be("field.user@example.test");
        typeof(LoginRequestDto).GetProperty("Username").Should().BeNull();
    }

    [Fact]
    public void TokenPair_ContainsV2ActorAndLifetimeShape()
    {
        var actor = new AuthActorResponseDto(
            Guid.Parse("d6d4cd4a-b261-4f79-91cf-87faafc2a931"),
            "Field User",
            "OPERATOR",
            "AQID");
        var response = new AuthTokenResponseDto(
            "access",
            "refresh",
            "Bearer",
            900,
            false,
            actor);

        response.ExpiresIn.Should().Be(900);
        response.User.Should().Be(actor);
    }

    [Fact]
    public void PasswordMutationRequests_ExposeOnlyV2Fields()
    {
        var recovery = new PasswordRecoveryRequestDto { Email = "field.user@example.test" };
        var change = new ChangePasswordRequestDto
        {
            CurrentPassword = "Current1!",
            NewPassword = "Replacement2!"
        };

        recovery.Email.Should().Be("field.user@example.test");
        change.CurrentPassword.Should().Be("Current1!");
        change.NewPassword.Should().Be("Replacement2!");
    }

    [Theory]
    [InlineData(AuthStatus.RefreshTokenInvalid)]
    [InlineData(AuthStatus.RefreshTokenExpired)]
    [InlineData(AuthStatus.SessionRevoked)]
    public void RefreshFailures_HaveDistinctServiceStatuses(AuthStatus status)
    {
        status.Should().NotBe(AuthStatus.InvalidCredentials);
    }
}
