using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.API.Constants;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Trait("TaskId", "P1-11")]
public sealed class P111ProfileUpdateTests : IClassFixture<AuthenticationSqlServerFixture>
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P111ProfileUpdateTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact(DisplayName = "P1-11 S3 Positive: profile update changes allowed fields and audits sanitized state")]
    public async Task ProfileUpdate_AllowedFields_PersistsAndAudits()
    {
        var username = $"profile_update_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, username);
        using var currentProfile = JsonDocument.Parse(
            await (await client.GetAsync("/api/v1/profile")).Content.ReadAsStringAsync());
        var expectedVersion = currentProfile.RootElement.GetProperty("rowVersion").GetString();

        var operationId = Guid.NewGuid();
        var response = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = "Updated Profile",
            email = $"{username}@example.test",
            expectedRowVersion = expectedVersion,
            operationId
        });

        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);
        using var body = JsonDocument.Parse(responseBody);
        body.RootElement.GetProperty("displayName").GetString().Should().Be("Updated Profile");
        body.RootElement.GetProperty("email").GetString().Should().Be($"{username}@example.test");
        body.RootElement.GetProperty("rowVersion").GetString().Should().NotBeNullOrWhiteSpace();

        await using var verification = _sql.CreateDbContext();
        var persisted = await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        persisted.DisplayName.Should().Be("Updated Profile");
        persisted.Email.Should().Be($"{username}@example.test");
        persisted.RoleCode.Should().Be(UserRoleCode.DroneOperator);
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "user_profile_updated"))
            .Should().Be(1);
    }

    [Fact(DisplayName = "P1-11 S3 Negative: stale profile version returns stable conflict")]
    public async Task ProfileUpdate_StaleVersion_ReturnsConcurrencyConflict()
    {
        var username = $"profile_stale_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, username);

        var response = await client.PutAsJsonAsync("/api/v1/profile", new
        {
            displayName = "Should Not Persist",
            email = (string?)null,
            expectedRowVersion = Convert.ToBase64String([0, 0, 0, 0]),
            operationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.ConcurrencyConflict);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username,
            password = "Current1!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString());
    }

    private static string? ProblemCode(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("code", out var code)
            ? code.GetString()
            : null;
    }
}
