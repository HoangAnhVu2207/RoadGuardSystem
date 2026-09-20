using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Trait("TaskId", "P1-11")]
public sealed class P111ProfileReadTests : IClassFixture<AuthenticationSqlServerFixture>
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P111ProfileReadTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact(DisplayName = "P1-11 S2 Negative: profile read requires authentication")]
    public async Task ProfileRead_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "P1-11 S2 Positive: profile read returns safe authenticated projection")]
    public async Task ProfileRead_AuthenticatedUser_ReturnsSafeProfile()
    {
        var username = $"profile_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username,
            password = "Current1!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginJson = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync());
        var accessToken = loginJson.RootElement.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/profile");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var profile = JsonDocument.Parse(body);
        var root = profile.RootElement;
        root.GetProperty("userId").GetGuid().Should().Be(user.Id);
        root.GetProperty("username").GetString().Should().Be(username);
        root.GetProperty("displayName").GetString().Should().Be(username);
        root.GetProperty("roleCode").GetString().Should().Be("DRONE_OPERATOR");
        root.GetProperty("status").GetString().Should().Be("ACTIVE");
        root.EnumerateObject().Select(property => property.Name)
            .Should().NotContain(new[] { "passwordHash", "securityStamp", "mustChangePassword", "sessions", "refreshTokens" });
    }
}
