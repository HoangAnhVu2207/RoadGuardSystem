using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class LoginWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    public static TheoryData<string, UserRoleCode, string> Cases => new()
    {
        { "API-002", UserRoleCode.Supervisor, "SUPERVISOR" }, { "API-002", UserRoleCode.ProjectManager, "PM" },
        { "API-002", UserRoleCode.DroneOperator, "OPERATOR" }, { "API-002", UserRoleCode.RepairCrew, "CREW" }, { "API-002", UserRoleCode.Reporter, "REPORTER" },
        { "API-006", UserRoleCode.Supervisor, "SUPERVISOR" }, { "API-006", UserRoleCode.ProjectManager, "PM" },
        { "API-006", UserRoleCode.DroneOperator, "OPERATOR" }, { "API-006", UserRoleCode.RepairCrew, "CREW" }, { "API-006", UserRoleCode.Reporter, "REPORTER" },
        { "API-014", UserRoleCode.Supervisor, "SUPERVISOR" }, { "API-014", UserRoleCode.ProjectManager, "PM" },
        { "API-014", UserRoleCode.DroneOperator, "OPERATOR" }, { "API-014", UserRoleCode.RepairCrew, "CREW" }, { "API-014", UserRoleCode.Reporter, "REPORTER" }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ExactLoginWireUsesCurrentIdentityAndFiniteTransportCredentials(string operation, UserRoleCode role, string wireRole)
    {
        var actor = await sql.CreateUserAsync("LOGIN-WIRE-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var other = await sql.CreateUserAsync("LOGIN-OTHER-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var route = operation switch { "API-002" => "/api/v1/auth/android/login", "API-006" => "/api/v1/auth/login", _ => "/api/v1/auth/web/login" };
        if (operation == "API-014")
        {
            var csrf = await client.GetAsync("/api/v1/auth/web/csrf");
            Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await csrf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString());
        }
        var started = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync(route, new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
        Assert.Null(response.Headers.ETag);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var fields = operation == "API-014" ? new[] { "absoluteExpiresAt", "idleExpiresAt", "issuedAt", "mustChangePassword", "renewable", "ticketLifetimeSeconds", "user" }
            : new[] { "accessToken", "expiresIn", "mustChangePassword", "refreshToken", "tokenType", "user" };
        Assert.Equal(fields, body.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        var user = body.GetProperty("user");
        Assert.Equal(new[] { "displayName", "id", "role", "version" }, user.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.Equal(actor.Id, user.GetProperty("id").GetGuid());
        Assert.Equal(actor.DisplayName, user.GetProperty("displayName").GetString());
        Assert.Equal(wireRole, user.GetProperty("role").GetString());
        Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        Assert.DoesNotContain(other.Email!, body.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("Current1!", body.GetRawText(), StringComparison.Ordinal);
        Assert.False(user.TryGetProperty("email", out _));
        Assert.False(user.TryGetProperty("phoneNumber", out _));
        await using var db = sql.CreateDbContext();
        var current = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
        Assert.Equal(Convert.ToBase64String(current.RowVersion), user.GetProperty("version").GetString());
        var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var refresh = await db.RefreshTokens.AsNoTracking().SingleAsync(t => t.SessionId == session.Id);
        Assert.Equal(SessionLifecycle.PersistentRenewable, session.Lifecycle);
        Assert.Equal(role, session.IssuedRole);
        Assert.Null(session.ExpiresAt); Assert.Null(session.RevokedAt);
        Assert.Null(refresh.ExpiresAt); Assert.Null(refresh.RevokedAt);
        Assert.InRange(session.IssuedAt, started.AddSeconds(-2), DateTimeOffset.UtcNow.AddSeconds(2));
        Assert.Equal(64, refresh.TokenHash.Length);
        if (operation == "API-014")
        {
            Assert.Equal(SessionTransport.Web, session.Transport);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("absoluteExpiresAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("idleExpiresAt").ValueKind);
            Assert.True(body.GetProperty("renewable").GetBoolean());
            Assert.Equal(43200, body.GetProperty("ticketLifetimeSeconds").GetInt32());
            Assert.Equal(session.IssuedAt, body.GetProperty("issuedAt").GetDateTimeOffset());
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), h => h.StartsWith("__Host-RoadGuardSession=", StringComparison.Ordinal) && h.Contains("httponly", StringComparison.OrdinalIgnoreCase) && h.Contains("secure", StringComparison.OrdinalIgnoreCase) && h.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) && !h.Contains("domain=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), h => h.StartsWith("__Host-RoadGuardRenewal=", StringComparison.Ordinal));
        }
        else
        {
            Assert.Equal(operation == "API-002" ? SessionTransport.Android : SessionTransport.LegacyBearer, session.Transport);
            Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
            var expectedSeconds = operation == "API-002" ? 900 : 600; // Factory legacy JWT lifetime is ten minutes; Android fixes fifteen.
            Assert.Equal(expectedSeconds, body.GetProperty("expiresIn").GetInt32());
            var token = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("accessToken").GetString());
            Assert.Equal(actor.Id.ToString(), token.Claims.Single(c => c.Type == "sub").Value);
            Assert.Equal(session.Id.ToString(), token.Claims.Single(c => c.Type == "sid").Value);
            Assert.InRange(token.ValidTo - session.IssuedAt.UtcDateTime, TimeSpan.FromSeconds(expectedSeconds - 2), TimeSpan.FromSeconds(expectedSeconds + 2));
            Assert.NotEqual(body.GetProperty("refreshToken").GetString(), refresh.TokenHash);
            Assert.False(string.IsNullOrEmpty(body.GetProperty("refreshToken").GetString()));
        }
        Assert.Empty(await db.Sessions.Where(s => s.UserId == other.Id).ToArrayAsync());
    }
}
