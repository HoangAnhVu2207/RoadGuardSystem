using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class SelfReadWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(UserRoleCode.Supervisor, "SUPERVISOR", "SUPERVISOR")]
    [InlineData(UserRoleCode.ProjectManager, "PM", "PM")]
    [InlineData(UserRoleCode.DroneOperator, "OPERATOR", "DRONE_OPERATOR")]
    [InlineData(UserRoleCode.RepairCrew, "CREW", "REPAIR_CREW")]
    [InlineData(UserRoleCode.Reporter, "REPORTER", "REPORTER")]
    public async Task EveryRoleReadsExactCurrentSelfFieldsAndNeverAnotherIdentity(UserRoleCode role, string wireRole, string profileRole)
    {
        var actor = await sql.CreateUserAsync("SELF-WIRE-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var other = await sql.CreateUserAsync("OTHER-SELF-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var web = await WebCookieTestClient.Login(factory, actor.Email!);
        using var bearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        await using var db = sql.CreateDbContext();
        var audits = await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id);
        var receipts = await db.IdempotencyRecords.CountAsync(a => a.ActorUserId == actor.Id);
        foreach (var display in new[] { actor.DisplayName, "Updated synthetic name" })
        {
            await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.DisplayName, display));
            var expected = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
            foreach (var client in new[] { web, bearer })
                foreach (var route in new[] { "/api/v1/me", "/api/v1/profile" })
                {
                    var response = await client.GetAsync(route);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
                    Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var keys = route.EndsWith("/me", StringComparison.Ordinal) ? new[] { "displayName", "id", "role", "version" } : new[] { "displayName", "email", "roleCode", "rowVersion", "status", "userId", "username" };
                    Assert.Equal(keys, body.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
                    Assert.Equal(expected.Id, body.GetProperty(route.EndsWith("/me", StringComparison.Ordinal) ? "id" : "userId").GetGuid());
                    Assert.Equal(expected.DisplayName, body.GetProperty("displayName").GetString());
                    Assert.Equal(route.EndsWith("/me", StringComparison.Ordinal) ? wireRole : profileRole, body.GetProperty(route.EndsWith("/me", StringComparison.Ordinal) ? "role" : "roleCode").GetString());
                    Assert.Equal(Convert.ToBase64String(expected.RowVersion), body.GetProperty(route.EndsWith("/me", StringComparison.Ordinal) ? "version" : "rowVersion").GetString());
                    if (route.EndsWith("/profile", StringComparison.Ordinal))
                    {
                        Assert.Equal(expected.UserName, body.GetProperty("username").GetString());
                        Assert.Equal(expected.Email, body.GetProperty("email").GetString());
                        Assert.Equal("ACTIVE", body.GetProperty("status").GetString());
                    }
                    Assert.DoesNotContain(other.Email!, body.GetRawText(), StringComparison.Ordinal);
                    Assert.False(body.TryGetProperty("phoneNumber", out _));
                    Assert.False(body.TryGetProperty("passwordHash", out _));
                    Assert.False(body.TryGetProperty("securityStamp", out _));
                }
            Assert.Equal(expected.RowVersion, (await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).RowVersion);
        }
        await db.Users.Where(u => u.Id == actor.Id).ExecuteUpdateAsync(u => u.SetProperty(v => v.Status, UserStatus.Suspended));
        foreach (var client in new[] { web, bearer })
            foreach (var route in new[] { "/api/v1/me", "/api/v1/profile" })
            {
                var response = await client.GetAsync(route);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
                Assert.Null(response.Headers.ETag);
                Assert.DoesNotContain(other.Email!, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }
        Assert.Equal(audits, await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id));
        Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync(a => a.ActorUserId == actor.Id));
    }
}
