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
[Collection(AuthenticationApiFixture.Name)]
public sealed class P111PasswordResetTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P111PasswordResetTests(AuthenticationSqlServerFixture sql)
    {
        _sql = sql;
    }

    [Fact(DisplayName = "P1-11 S5 Positive: Supervisor reset returns one-time temporary password and forces change")]
    public async Task PasswordReset_SupervisorSuccess_ReturnsOneTimeCredentialAndRevokesOldLogin()
    {
        var supervisorName = $"reset_supervisor_{Guid.NewGuid():N}";
        var targetName = $"reset_target_{Guid.NewGuid():N}";
        var supervisor = await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var target = await _sql.CreateUserAsync(targetName, "Current1!", UserRoleCode.DroneOperator);
        string expectedVersion;
        await using (var context = _sql.CreateDbContext())
        {
            expectedVersion = Convert.ToBase64String(
                (await context.Users.AsNoTracking().SingleAsync(user => user.Id == target.Id)).RowVersion);
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var supervisorClient = factory.CreateClient();
        await AuthenticateAsync(supervisorClient, supervisorName, "Supervisor1!");

        var request = new
        {
            expectedTargetRowVersion = expectedVersion,
            operationId = Guid.NewGuid()
        };
        var response = await supervisorClient.PostAsJsonAsync(
            $"/api/v1/admin/users/{target.Id}/password-reset",
            request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var temporaryPassword = body.RootElement.GetProperty("temporaryPassword").GetString();
        temporaryPassword.Should().NotBeNullOrWhiteSpace();
        body.RootElement.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();
        body.RootElement.ToString().Should().NotContain("passwordHash");

        var replay = await supervisorClient.PostAsJsonAsync(
            $"/api/v1/admin/users/{target.Id}/password-reset",
            request);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        using var replayBody = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        replayBody.RootElement.GetProperty("temporaryPassword").ValueKind.Should().Be(JsonValueKind.Null);
        replayBody.RootElement.ToString().Should().NotContain(temporaryPassword);

        using var targetClient = factory.CreateClient();
        (await targetClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = targetName,
            password = "Current1!"
        })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var forced = await targetClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = targetName,
            password = temporaryPassword
        });
        forced.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ProblemCode(await forced.Content.ReadAsStringAsync()).Should().Be(ApiErrorCodes.PasswordChangeRequired);
    }

    [Fact(DisplayName = "P1-11 S5 Negative: non-Supervisor cannot reset a target user")]
    public async Task PasswordReset_NonSupervisor_ReturnsForbidden()
    {
        var actorName = $"reset_actor_{Guid.NewGuid():N}";
        var targetName = $"reset_forbidden_target_{Guid.NewGuid():N}";
        await _sql.CreateUserAsync(actorName, "Current1!", UserRoleCode.DroneOperator);
        var target = await _sql.CreateUserAsync(targetName, "Current1!");
        string expectedVersion;
        await using (var context = _sql.CreateDbContext())
        {
            expectedVersion = Convert.ToBase64String(
                (await context.Users.AsNoTracking().SingleAsync(user => user.Id == target.Id)).RowVersion);
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, actorName, "Current1!");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/admin/users/{target.Id}/password-reset",
            new { expectedTargetRowVersion = expectedVersion, operationId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ProblemCode(await response.Content.ReadAsStringAsync()).Should().Be("access_forbidden");
    }

    [Fact(DisplayName = "P1-11 S5 Negative: Supervisor cannot reset a suspended target")]
    public async Task PasswordReset_SuspendedTarget_ReturnsConflictWithoutCredentialChanges()
    {
        var supervisorName = $"reset_suspended_supervisor_{Guid.NewGuid():N}";
        var targetName = $"reset_suspended_target_{Guid.NewGuid():N}";
        var supervisor = await _sql.CreateUserAsync(supervisorName, "Supervisor1!", UserRoleCode.Supervisor);
        var target = await _sql.CreateUserAsync(
            targetName,
            "Current1!",
            UserRoleCode.DroneOperator,
            UserStatus.Suspended);
        string expectedVersion;
        string originalPasswordHash;
        await using (var context = _sql.CreateDbContext())
        {
            var persistedTarget = await context.Users.AsNoTracking().SingleAsync(user => user.Id == target.Id);
            expectedVersion = Convert.ToBase64String(persistedTarget.RowVersion);
            originalPasswordHash = persistedTarget.PasswordHash!;
        }

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var supervisorClient = factory.CreateClient();
        await AuthenticateAsync(supervisorClient, supervisorName, "Supervisor1!");

        var response = await supervisorClient.PostAsJsonAsync(
            $"/api/v1/admin/users/{target.Id}/password-reset",
            new { expectedTargetRowVersion = expectedVersion, operationId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        ProblemCode(body).Should().Be(ApiErrorCodes.IdentityUserInactive);
        body.Should().NotContain("temporaryPassword").And.NotContain("passwordHash");

        await using var verification = _sql.CreateDbContext();
        var verificationTarget = await verification.Users.AsNoTracking().SingleAsync(user => user.Id == target.Id);
        verificationTarget.PasswordHash.Should().Be(originalPasswordHash);
        verificationTarget.MustChangePassword.Should().BeFalse();
        (await verification.Sessions.CountAsync(session => session.UserId == target.Id)).Should().Be(0);
        (await verification.PasswordResetLogs.CountAsync(log => log.TargetUserId == target.Id)).Should().Be(0);
        (await verification.AuditLogs.CountAsync(log => log.EntityId == target.Id && log.EventType == "user_password_reset"))
            .Should().Be(0);
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
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
