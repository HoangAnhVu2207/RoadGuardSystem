using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class AdminResetBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ResetRequiresCurrentBearerAndRejectsCookieEvenWithValidCsrf()
    {
        var actor = await sql.CreateUserAsync("reset-boundary-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-boundary-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        var sid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(c => c.Type == "sid").Value);
        string version;
        await using (var db = sql.CreateDbContext()) version = Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id)).RowVersion);
        var before = await Snapshot(target.Id);
        var path = $"/api/v1/admin/users/{target.Id}/password-reset";
        var input = new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() };
        foreach (var credential in new string?[] { null, "tampered", new AccessTokenFactory(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value).Create(actor.Id, sid, UserRoleCode.Supervisor, DateTimeOffset.UtcNow.AddMinutes(-20), 15) })
        {
            client.DefaultRequestHeaders.Authorization = credential is null ? null : new AuthenticationHeaderValue("Bearer", credential);
            using var denied = await client.PostAsJsonAsync(path, input); await Error(denied, 401, "auth_unauthorized");
            Assert.Equal(before, await Snapshot(target.Id));
        }
        using var web = await WebCookieTestClient.Login(factory, actor.Email!);
        using (var denied = await web.PostAsJsonAsync(path, input)) await Error(denied, 401, "auth_unauthorized");
        Assert.Equal(before, await Snapshot(target.Id));
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(s => s.Id == sid).ExecuteUpdateAsync(u => u.SetProperty(s => s.RevokedAt, DateTimeOffset.UtcNow));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using (var denied = await client.PostAsJsonAsync(path, input)) await Error(denied, 401, "auth_session_revoked");
        Assert.Equal(before, await Snapshot(target.Id));
    }

    [Fact]
    public async Task ResetRejectsInvalidEnvelopeUnknownAndInactiveTargetsWithoutWrites()
    {
        var actor = await sql.CreateUserAsync("reset-input-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var target = await sql.CreateUserAsync("reset-input-target-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var inactive = await sql.CreateUserAsync("reset-input-inactive-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter, UserStatus.Suspended);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        string version, inactiveVersion;
        await using (var db = sql.CreateDbContext())
        {
            version = Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == target.Id)).RowVersion);
            inactiveVersion = Convert.ToBase64String((await db.Users.AsNoTracking().SingleAsync(u => u.Id == inactive.Id)).RowVersion);
        }
        var before = await Snapshot(target.Id); var inactiveBefore = await Snapshot(inactive.Id);
        var path = $"/api/v1/admin/users/{target.Id}/password-reset";
        foreach (var json in new[] { "null", "{", "[]", "{}", "{\"operationId\":\"not-a-guid\"}", "{\"expectedTargetRowVersion\":123}", JsonSerializer.Serialize(new { expectedTargetRowVersion = "bad-base64", operationId = Guid.NewGuid() }), JsonSerializer.Serialize(new { expectedTargetRowVersion = version, operationId = Guid.Empty }) })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request); await Error(response, 400, "validation_error");
            Assert.Equal(before, await Snapshot(target.Id));
        }
        using (var zero = await client.PostAsJsonAsync("/api/v1/admin/users/" + Guid.Empty + "/password-reset", new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() })) await Error(zero, 400, "validation_error");
        using (var missing = await client.PostAsJsonAsync("/api/v1/admin/users/" + Guid.NewGuid() + "/password-reset", new { expectedTargetRowVersion = version, operationId = Guid.NewGuid() })) await Error(missing, 404, "identity_user_not_found");
        using (var denied = await client.PostAsJsonAsync("/api/v1/admin/users/" + inactive.Id + "/password-reset", new { expectedTargetRowVersion = inactiveVersion, operationId = Guid.NewGuid() })) await Error(denied, 409, "identity_user_inactive");
        Assert.Equal(before, await Snapshot(target.Id)); Assert.Equal(inactiveBefore, await Snapshot(inactive.Id));
    }

    private async Task<string> Snapshot(Guid target)
    {
        await using var db = sql.CreateDbContext();
        return JsonSerializer.Serialize(new
        {
            user = await db.Users.AsNoTracking().Where(u => u.Id == target).Select(u => new { u.PasswordHash, u.SecurityStamp, u.MustChangePassword, u.RowVersion }).SingleAsync(),
            resets = await db.PasswordResetLogs.Where(r => r.TargetUserId == target).Select(r => r.Id).ToArrayAsync(),
            audits = await db.AuditLogs.Where(a => a.EntityId == target).Select(a => a.Id).ToArrayAsync()
        });
    }
    private static async Task Error(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal((HttpStatusCode)status, response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, error.GetProperty("status").GetInt32()); Assert.Equal(code, error.GetProperty("code").GetString());
        Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), error.GetProperty("correlationId").GetString());
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("temporaryPassword", error.GetRawText()); Assert.DoesNotContain("passwordHash", error.GetRawText());
    }
}
