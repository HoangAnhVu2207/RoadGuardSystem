using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Trait("TaskId", "V2-P1-001..005")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class V2AuthenticationFlowTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public V2AuthenticationFlowTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact]
    public async Task Login_ByEmail_ReturnsV2TokenPairAndActor()
    {
        var username = $"v2_login_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!", UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password = "Current1!"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("tokenType").GetString().Should().Be("Bearer");
        body.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("mustChangePassword").GetBoolean().Should().BeFalse();
        var actor = body.GetProperty("user");
        actor.GetProperty("id").GetGuid().Should().Be(user.Id);
        actor.GetProperty("displayName").GetString().Should().Be(username);
        actor.GetProperty("role").GetString().Should().Be("OPERATOR");
        actor.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        body.TryGetProperty("accessTokenExpiresAt", out _).Should().BeFalse();
    }

    [Fact]
    public async Task PasswordRecovery_KnownAndUnknownEmails_ReturnNeutralAcceptedAndDurableRequests()
    {
        var targetName = $"v2_recovery_{Guid.NewGuid():N}";
        var target = await _sql.CreateUserAsync(targetName, "Current1!");
        var supervisor = await _sql.CreateUserAsync(
            $"v2_supervisor_{Guid.NewGuid():N}",
            "Current1!",
            UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();

        var known = await client.PostAsJsonAsync("/api/v1/auth/password-recovery-requests", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(targetName)
        });
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/password-recovery-requests", new
        {
            email = $"missing-{Guid.NewGuid():N}@example.test"
        });

        known.StatusCode.Should().Be(HttpStatusCode.Accepted);
        unknown.StatusCode.Should().Be(HttpStatusCode.Accepted);
        known.Headers.Location.Should().NotBeNull();
        unknown.Headers.Location.Should().NotBeNull();
        await using var verification = _sql.CreateDbContext();
        var requests = await verification.PasswordRecoveryRequests.AsNoTracking()
            .OrderByDescending(item => item.RequestedAtUtc)
            .Take(2)
            .ToListAsync();
        requests.Should().ContainSingle(item => item.TargetUserId == target.Id);
        requests.Should().ContainSingle(item => item.TargetUserId == null);
        var knownRequest = requests.Single(item => item.TargetUserId == target.Id);
        (await verification.Notifications.CountAsync(item =>
            item.RecipientUserId == supervisor.Id && item.SourceEntityId == knownRequest.Id))
            .Should().Be(1);
    }

    [Fact]
    public async Task ChangePassword_RevokesSessionsAndAllowsOnlyReplacementPassword()
    {
        var username = $"v2_change_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        var tokens = await LoginAsync(client, username, "Current1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var idempotencyKey = Guid.NewGuid().ToString("N");
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        var request = new
        {
            currentPassword = "Current1!",
            newPassword = "Replacement2!"
        };
        var changed = await client.PostAsJsonAsync("/api/v1/auth/change-password", request);
        var replay = await client.PostAsJsonAsync("/api/v1/auth/change-password", request);

        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        replay.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await using (var verification = _sql.CreateDbContext())
        {
            (await verification.Sessions.CountAsync(item => item.UserId == user.Id && item.RevokedAt == null))
                .Should().Be(0);
            (await verification.AuditLogs.CountAsync(item =>
                item.EntityId == user.Id && item.EventType == "auth_password_changed"))
                .Should().Be(1);
        }

        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        (await PostLoginAsync(client, username, "Current1!")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostLoginAsync(client, username, "Replacement2!")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithIdempotencyKey_PersistsReceiptAndRevokesSession()
    {
        var username = $"v2_logout_{Guid.NewGuid():N}";
        var user = await _sql.CreateUserAsync(username, "Current1!");
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        var tokens = await LoginAsync(client, username, "Current1!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await client.PostAsync("/api/v1/auth/logout", null);
        var replay = await client.PostAsync("/api/v1/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        replay.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await using var verification = _sql.CreateDbContext();
        (await verification.Sessions.CountAsync(item => item.UserId == user.Id && item.RevokedAt != null))
            .Should().Be(1);
        (await verification.IdempotencyRecords.CountAsync(item =>
            item.ActorUserId == user.Id && item.Operation == "Logout"))
            .Should().Be(1);
    }

    private static async Task<TokenPair> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await PostLoginAsync(client, username, password);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new TokenPair(
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = AuthenticationSqlServerFixture.EmailFor(username),
            password
        });

    private sealed record TokenPair(string AccessToken, string RefreshToken);
}
