using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Trait("TaskId", "V2-P1-011..015")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class V2AccountFlowTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public V2AccountFlowTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task GetMe_ReturnsActorProjectionForCurrentUser()
    {
        var username = $"v2_me_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, username, "Current1!");

        var response = await client.GetAsync("/api/v1/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("id").GetGuid().Should().Be(user.Id);
        body.GetProperty("displayName").GetString().Should().Be(username);
        body.GetProperty("role").GetString().Should().Be("OPERATOR");
        body.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("email", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateMe_WithMatchingEtag_PersistsDisplayNameAndReplays()
    {
        var username = $"v2_update_me_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, username, "Current1!");
        var current = await (await client.GetAsync("/api/v1/me")).Content.ReadFromJsonAsync<JsonElement>();
        var version = current.GetProperty("version").GetString();
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var first = await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "V2 Display Name" });
        var replay = await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "V2 Display Name" });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var verification = _sql.CreateDbContext();
        (await verification.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id)).DisplayName
            .Should().Be("V2 Display Name");
        (await verification.AuditLogs.CountAsync(item =>
            item.EntityId == user.Id && item.EventType == "user_profile_updated"))
            .Should().Be(1);
    }

    [Fact]
    public async Task GetAccount_SupervisorReturnsActorAndEtag_NonSupervisorIsForbidden()
    {
        var supervisorName = $"v2_account_supervisor_{Guid.NewGuid():N}";
        var operatorName = $"v2_account_operator_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var target = await _sql.CreateUserAsync(operatorName, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var supervisor = factory.CreateClient();
        await AuthenticateAsync(supervisor, supervisorName, "Supervisor1!");

        var allowed = await supervisor.GetAsync($"/api/v1/users/{target.Id}");

        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        allowed.Headers.ETag.Should().NotBeNull();
        using var operatorClient = factory.CreateClient();
        await AuthenticateAsync(operatorClient, operatorName, "Current1!");
        (await operatorClient.GetAsync($"/api/v1/users/{target.Id}")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResetPasswordV2_UsesProvidedSecretWithoutEchoAndRevokesSessions()
    {
        var supervisorName = $"v2_reset_supervisor_{Guid.NewGuid():N}";
        var targetName = $"v2_reset_target_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var target = await _sql.CreateUserAsync(targetName, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var targetClient = factory.CreateClient();
        await AuthenticateAsync(targetClient, targetName, "Current1!");
        using var supervisorClient = factory.CreateClient();
        await AuthenticateAsync(supervisorClient, supervisorName, "Supervisor1!");
        supervisorClient.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await supervisorClient.PostAsJsonAsync(
            $"/api/v1/users/{target.Id}/password-reset",
            new { temporaryPassword = "Replacement2!", reason = "ADMINISTRATOR_INITIATED" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.CountAsync(item => item.UserId == target.Id && item.RevokedAt == null))
            .Should().Be(0);
    }

    [Fact]
    public async Task UpdateAccount_WithMatchingEtag_ChangesRoleAndStatusAndRevokesSessions()
    {
        var supervisorName = $"v2_update_supervisor_{Guid.NewGuid():N}";
        var targetName = $"v2_update_target_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var target = await _sql.CreateUserAsync(targetName, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var targetClient = factory.CreateClient();
        await AuthenticateAsync(targetClient, targetName, "Current1!");
        using var supervisorClient = factory.CreateClient();
        await AuthenticateAsync(supervisorClient, supervisorName, "Supervisor1!");
        var current = await supervisorClient.GetAsync($"/api/v1/users/{target.Id}");
        current.Headers.ETag.Should().NotBeNull();
        supervisorClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", current.Headers.ETag!.Tag);
        supervisorClient.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await supervisorClient.PatchAsJsonAsync($"/api/v1/users/{target.Id}", new
        {
            status = "SUSPENDED",
            role = "PM",
            reason = "ADMINISTRATIVE_LOCK"
        });
        var replay = await supervisorClient.PatchAsJsonAsync($"/api/v1/users/{target.Id}", new
        {
            status = "SUSPENDED",
            role = "PM",
            reason = "ADMINISTRATIVE_LOCK"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("role").GetString().Should().Be("PM");
        await using var verification = _sql.CreateDbContext();
        var persisted = await verification.Users.AsNoTracking().SingleAsync(item => item.Id == target.Id);
        persisted.Status.Should().Be(UserStatus.Suspended);
        persisted.RoleCode.Should().Be(UserRoleCode.ProjectManager);
        (await verification.Sessions.CountAsync(item => item.UserId == target.Id && item.RevokedAt == null))
            .Should().Be(0);
        (await verification.AccountStatusChangeLogs.CountAsync(item => item.TargetUserId == target.Id))
            .Should().Be(1);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.GetProperty("accessToken").GetString());
    }
}
