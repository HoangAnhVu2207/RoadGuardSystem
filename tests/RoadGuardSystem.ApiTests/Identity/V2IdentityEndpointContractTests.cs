using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Trait("TaskId", "V2-P1-006..015")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class V2IdentityEndpointContractTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public V2IdentityEndpointContractTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Theory]
    [InlineData("GET", "/api/v1/me")]
    [InlineData("PATCH", "/api/v1/me")]
    [InlineData("GET", "/api/v1/users/11111111-1111-1111-1111-111111111111")]
    [InlineData("PATCH", "/api/v1/users/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/v1/users/11111111-1111-1111-1111-111111111111/password-reset")]
    [InlineData("POST", "/api/v1/invitations")]
    public async Task ProtectedV2IdentityRoute_WithoutBearer_ReturnsUnauthorized(
        string method,
        string route)
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), route);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v1/auth/reporter-registrations")]
    [InlineData("/api/v1/auth/reporter-registrations/verify")]
    [InlineData("/api/v1/auth/reporter-registrations/resend")]
    [InlineData("/api/v1/invitations/accept")]
    public async Task PublicIdempotentRoute_WithoutIdempotencyKey_ReturnsBadRequest(string route)
    {
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(route, new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
