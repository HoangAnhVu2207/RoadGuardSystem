using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

public sealed class AdminResetCurrentAuthorityAcceptanceTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("suspended", "auth_unauthorized")]
    [InlineData("role-replaced", "auth_session_revoked")]
    [InlineData("role-inactive", "auth_unauthorized")]
    [InlineData("password-change", "auth_password_change_required")]
    [InlineData("session-revoked", "auth_session_revoked")]
    public async Task CommittedResetCannotReplayAfterCurrentSupervisorAuthorityChanges(string change, string code)
    {
        var actor = await sql.CreateUserAsync("reset-current-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-current-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        await using var db = sql.CreateDbContext();
        var version = Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id)).RowVersion);
        var command = new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() };
        var path = $"/api/v1/admin/users/{target.Id}/password-reset";
        using (var success = await client.PostAsJsonAsync(path, command)) Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        try
        {
            if (change == "suspended") await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, UserStatus.Suspended));
            if (change == "role-replaced") await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.RoleCode, UserRoleCode.RepairCrew));
            if (change == "role-inactive") await db.Roles.Where(r => r.Code == UserRoleCode.Supervisor).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, false));
            if (change == "password-change") await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.MustChangePassword, true));
            if (change == "session-revoked") await db.Sessions.Where(s => s.Id == session.Id).ExecuteUpdateAsync(s => s.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
            var before = await Snapshot(target.Id, actor.Id);
            using var denied = await client.PostAsJsonAsync(path, command);
            await Error(denied, change == "password-change" ? 403 : 401, code);
            Assert.Equal(before, await Snapshot(target.Id, actor.Id));
            if (change == "role-replaced")
            {
                Assert.NotNull((await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id)).RevokedAt);
                Assert.NotNull((await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == session.Id)).RevokedAt);
            }
        }
        finally
        {
            if (change == "role-inactive") await db.Roles.Where(r => r.Code == UserRoleCode.Supervisor).ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, true));
        }
    }

    [Fact]
    public async Task PendingTargetCannotReceiveResetOrCreateHistory()
    {
        var actor = await sql.CreateUserAsync("reset-pending-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-pending-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter, UserStatus.Pending);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        await using var db = sql.CreateDbContext();
        var version = Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id)).RowVersion);
        var before = await Snapshot(target.Id, actor.Id);
        using var response = await client.PostAsJsonAsync($"/api/v1/admin/users/{target.Id}/password-reset", new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() });
        await Error(response, 409, "identity_user_inactive");
        Assert.Equal(before, await Snapshot(target.Id, actor.Id));
    }

    private async Task<string> Snapshot(Guid target, Guid actor)
    {
        await using var db = sql.CreateDbContext();
        return JsonSerializer.Serialize(new
        {
            user = await db.Users.AsNoTracking().Where(u => u.Id == target).Select(u => new { u.PasswordHash, u.SecurityStamp, u.Status, u.MustChangePassword, u.RowVersion }).SingleAsync(),
            resets = await db.PasswordResetLogs.AsNoTracking().Where(r => r.TargetUserId == target).OrderBy(r => r.Id).ToArrayAsync(),
            audits = await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == target).OrderBy(a => a.Id).ToArrayAsync(),
            receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.ActorUserId == actor && r.Operation == "AdminPasswordReset").OrderBy(r => r.Id).ToArrayAsync()
        });
    }
    private static async Task Error(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, body.GetProperty("status").GetInt32()); Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), body.GetProperty("correlationId").GetString());
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("temporaryPassword", body.GetRawText()); Assert.DoesNotContain("passwordHash", body.GetRawText());
    }
}
