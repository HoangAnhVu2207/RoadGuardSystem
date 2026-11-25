using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

// Authentication admission only. Random resource IDs intentionally prevent
// these negative requests from being mistaken for positive workflow evidence.
public sealed class HandoffExpiredCredentialHttpTests(AuthenticationSqlServerFixture sql)
    : IClassFixture<AuthenticationSqlServerFixture>
{
    public static IEnumerable<object[]> Operations()
    {
        const string assignment = "/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor";
        yield return ["ADD-13", "GET", assignment];
        yield return ["ADD-14", "PUT", assignment];
        yield return ["ADD-15", "DELETE", assignment];
        yield return ["ADD-16", "POST", "/api/v1/projects/{projectId}/defects/{defectId}/publications"];
        yield return ["ADD-17", "POST", "/api/v1/projects/{projectId}/publications/{publicId}/withdraw"];
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ExpiredJwtAndRevokedSessionDenyExactProtectedOperation(string operationId, string method, string route)
    {
        Assert.StartsWith("ADD-", operationId);
        var user = await sql.CreateUserAsync($"handoff-expiry-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var liveToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        await using var db = sql.CreateDbContext();
        var session = await db.Sessions.AsNoTracking().SingleAsync(x => x.UserId == user.Id);
        Assert.True(session.IsActiveAt(DateTimeOffset.UtcNow));
        var options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var expired = new AccessTokenFactory(options).Create(user.Id, session.Id, user.RoleCode, DateTimeOffset.UtcNow.AddMinutes(-20), 15);
        var path = Regex.Replace(route, "\\{[^}]+\\}", _ => Guid.NewGuid().ToString());
        var receipts = await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == user.Id);
        var audits = await db.AuditLogs.CountAsync(x => x.ActorUserId == user.Id);
        using (var request = Request(method, path, expired))
        using (var denied = await client.SendAsync(request))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            Assert.False(denied.Headers.Contains("Set-Cookie"));
        }
        Assert.True((await db.Sessions.AsNoTracking().SingleAsync(x => x.Id == session.Id)).IsActiveAt(DateTimeOffset.UtcNow));
        await db.Sessions.Where(x => x.Id == session.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        using (var request = Request(method, path, liveToken!))
        using (var denied = await client.SendAsync(request))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            Assert.False(denied.Headers.Contains("Set-Cookie"));
        }
        Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync(x => x.ActorUserId == user.Id));
        Assert.Equal(audits, await db.AuditLogs.CountAsync(x => x.ActorUserId == user.Id));
    }

    private static HttpRequestMessage Request(string method, string path, string token)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return request;
    }
}
