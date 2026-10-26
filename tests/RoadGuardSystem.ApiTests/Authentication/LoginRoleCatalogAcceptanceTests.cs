using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LoginRoleCatalogAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("API-002", "/api/v1/auth/android/login")]
    [InlineData("API-006", "/api/v1/auth/login")]
    [InlineData("API-014", "/api/v1/auth/web/login")]
    public async Task LoginRechecksActiveRoleCatalogAndRecoveryWithoutCreatingDeniedCredentials(string operation, string route)
    {
        var actor = await sql.CreateUserAsync("LOGIN-ROLE-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        if (operation == "API-014")
        {
            var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        }
        await using var db = sql.CreateDbContext();
        var role = await db.Roles.AsNoTracking().SingleAsync(r => r.Code == UserRoleCode.Reporter);
        Assert.True(role.IsActive);
        try
        {
            await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, false));
            using var denied = await client.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(operation == "API-014" ? "auth_unauthorized" : "auth_invalid_credentials", problem.GetProperty("code").GetString());
            Assert.Equal(route, problem.GetProperty("instance").GetString());
            Assert.False(problem.TryGetProperty("user", out _));
            Assert.False(problem.TryGetProperty("accessToken", out _));
            Assert.Empty(await db.Sessions.Where(s => s.UserId == actor.Id).ToArrayAsync());
            Assert.Empty(await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id).ToArrayAsync());
            Assert.Null((await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id)).LastLoginAt);
            Assert.Empty(await db.AuditLogs.Where(a => a.ActorUserId == actor.Id).ToArrayAsync());
        }
        finally
        {
            // Catalog belongs solely to this disposable fixture; restore even if assertion fails.
            await db.Roles.Where(r => r.Code == UserRoleCode.Reporter).ExecuteUpdateAsync(r => r.SetProperty(v => v.IsActive, role.IsActive));
        }
        using var recovered = await client.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(actor.Id, (await recovered.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user").GetProperty("id").GetGuid());
        Assert.Single(await db.Sessions.Where(s => s.UserId == actor.Id).ToArrayAsync());
        Assert.Single(await db.RefreshTokens.Where(t => t.Session.UserId == actor.Id).ToArrayAsync());
    }
}
