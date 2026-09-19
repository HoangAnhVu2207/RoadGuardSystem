using System.Net;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Trait("TaskId", "P1-10")]
public sealed class AuthenticationOpenApiTests
{
    [Fact(DisplayName = "P1-10 Positive: OpenAPI exposes auth DTOs and bearer security only on protected logout")]
    public async Task OpenApi_AuthenticationContracts_DoNotExposePersistenceEntitiesOrSecureAnonymousRoutes()
    {
        await using var factory = new CustomWebApplicationFactory("Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("components").GetProperty("securitySchemes")
            .TryGetProperty("Bearer", out _).Should().BeTrue();
        root.GetProperty("paths").GetProperty("/api/v1/auth/login").GetProperty("post")
            .TryGetProperty("security", out _).Should().BeFalse();
        root.GetProperty("paths").GetProperty("/api/v1/auth/logout").GetProperty("post")
            .GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
        var loginResponses = root.GetProperty("paths").GetProperty("/api/v1/auth/login")
            .GetProperty("post").GetProperty("responses");
        loginResponses.GetProperty("200").GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith("/AuthTokenResponseDto");
        loginResponses.GetProperty("400").GetProperty("content").GetProperty("application/problem+json")
            .GetProperty("schema").GetProperty("$ref").GetString().Should().EndWith("/ProblemDetails");
        loginResponses.TryGetProperty("401", out _).Should().BeTrue();
        loginResponses.TryGetProperty("403", out _).Should().BeTrue();
        loginResponses.TryGetProperty("409", out _).Should().BeTrue();
        root.GetProperty("paths").GetProperty("/api/v1/auth/logout").GetProperty("post")
            .GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();
        body.Should().NotContain("ApplicationUser")
            .And.NotContain("UserSession")
            .And.NotContain("RefreshTokenSecurityState");
    }
}
