using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.ApiTests.Authentication;

[Collection(AuthenticationApiFixture.Name)]
public sealed class RefreshWireAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    public static IEnumerable<object[]> Cases => LoginWireAcceptanceTests.Cases
        .Where(c => (string)c[0] != "API-014")
        .Select(c => new object[] { (string)c[0] == "API-002" ? "API-003" : "API-009", c[1], c[2] });

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task ExactRefreshWireRotatesOnlyCredentialOwnerAndReturnsCurrentIdentity(string operation, UserRoleCode role, string wireRole)
    {
        var actor = await sql.CreateUserAsync("REFRESH-WIRE-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        var other = await sql.CreateUserAsync("REFRESH-OTHER-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var prefix = operation == "API-003" ? "/api/v1/auth/android/" : "/api/v1/auth/";
        var original = await Login(actor.Email!);
        var unrelated = await Login(other.Email!);
        await using var db = sql.CreateDbContext();
        var session = await db.Sessions.AsNoTracking().SingleAsync(s => s.UserId == actor.Id);
        var otherBefore = await OtherSnapshot();
        var started = DateTimeOffset.UtcNow;
        using var request = new HttpRequestMessage(HttpMethod.Post, prefix + "refresh")
        { Content = JsonContent.Create(new { refreshToken = original }) };
        var correlation = Guid.NewGuid().ToString(); request.Headers.Add("X-Correlation-ID", correlation);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(correlation, Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "accessToken", "expiresIn", "mustChangePassword", "refreshToken", "tokenType", "user" },
            body.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        var user = body.GetProperty("user");
        Assert.Equal(new[] { "displayName", "id", "role", "version" }, user.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        Assert.Equal(actor.Id, user.GetProperty("id").GetGuid());
        Assert.Equal(actor.DisplayName, user.GetProperty("displayName").GetString());
        Assert.Equal(wireRole, user.GetProperty("role").GetString());
        Assert.False(body.GetProperty("mustChangePassword").GetBoolean()); Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        foreach (var secret in new[] { actor.Email!, other.Email!, "Current1!", original, unrelated })
            Assert.DoesNotContain(secret, body.GetRawText(), StringComparison.Ordinal);
        var current = await db.Users.AsNoTracking().SingleAsync(u => u.Id == actor.Id);
        Assert.Equal(Convert.ToBase64String(current.RowVersion), user.GetProperty("version").GetString());
        var expectedSeconds = operation == "API-003" ? 900 : 600;
        Assert.Equal(expectedSeconds, body.GetProperty("expiresIn").GetInt32());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("accessToken").GetString());
        Assert.Equal(actor.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal(session.Id.ToString(), jwt.Claims.Single(c => c.Type == "sid").Value);
        Assert.InRange(jwt.ValidTo, started.UtcDateTime.AddSeconds(expectedSeconds - 2), DateTime.UtcNow.AddSeconds(expectedSeconds + 2));
        var successor = body.GetProperty("refreshToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(successor)); Assert.NotEqual(original, successor);
        var rows = await db.RefreshTokens.AsNoTracking().Where(t => t.SessionId == session.Id).ToArrayAsync();
        Assert.Equal(2, rows.Length);
        var predecessor = Assert.Single(rows, t => t.TokenHash == Hash(original));
        var replacement = Assert.Single(rows, t => t.TokenHash == Hash(successor));
        Assert.NotNull(predecessor.RevokedAt); Assert.Null(replacement.RevokedAt);
        Assert.Equal(predecessor.SessionId, replacement.SessionId); Assert.Null(replacement.ExpiresAt);
        var retained = await db.Sessions.AsNoTracking().SingleAsync(s => s.Id == session.Id);
        Assert.Null(retained.RevokedAt); Assert.Equal(SessionLifecycle.PersistentRenewable, retained.Lifecycle);
        Assert.Equal(operation == "API-003" ? SessionTransport.Android : SessionTransport.LegacyBearer, retained.Transport);
        Assert.Equal(otherBefore, await OtherSnapshot());

        async Task<string> Login(string email)
        {
            using var login = await client.PostAsJsonAsync(prefix + "login", new { email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString()!;
        }
        async Task<string> OtherSnapshot() => JsonSerializer.Serialize(new
        {
            sessions = await db.Sessions.AsNoTracking().Where(s => s.UserId == other.Id).OrderBy(s => s.Id).ToArrayAsync(),
            tokens = await db.RefreshTokens.AsNoTracking().Where(t => t.Session.UserId == other.Id).OrderBy(t => t.Id).ToArrayAsync()
        });
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
