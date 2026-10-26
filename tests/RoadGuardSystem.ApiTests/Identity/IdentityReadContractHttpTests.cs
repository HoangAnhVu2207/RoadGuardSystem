using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using Xunit;

namespace RoadGuardSystem.ApiTests.Identity;

[Collection(AuthenticationApiFixture.Name)]
public sealed class IdentityReadContractHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task CsrfBootstrapIsAnonymousNoStoreAndCreatesNoDurableSession()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await using var db = sql.CreateDbContext();
        var sessions = await db.Sessions.CountAsync(); var audits = await db.AuditLogs.CountAsync();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var response = await client.GetAsync("/api/v1/auth/web/csrf");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl!.NoStore);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Keys(body, "requestToken", "headerName");
            Assert.Equal("X-CSRF-TOKEN", body.GetProperty("headerName").GetString());
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("requestToken").GetString()));
        }
        Assert.Equal(sessions, await db.Sessions.CountAsync());
        Assert.Equal(audits, await db.AuditLogs.CountAsync());
    }

    [Theory]
    [InlineData("/api/v1/me", false)]
    [InlineData("/api/v1/me", true)]
    [InlineData("/api/v1/profile", false)]
    [InlineData("/api/v1/profile", true)]
    public async Task AnonymousAndMalformedBearerCannotReadSelfProjection(string route, bool malformed)
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { HandleCookies = false });
        if (malformed) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
    }

    [Theory]
    [InlineData(UserRoleCode.Supervisor, "SUPERVISOR")]
    [InlineData(UserRoleCode.ProjectManager, "PM")]
    [InlineData(UserRoleCode.DroneOperator, "OPERATOR")]
    [InlineData(UserRoleCode.RepairCrew, "CREW")]
    [InlineData(UserRoleCode.Reporter, "REPORTER")]
    public async Task SelfReadsBindCurrentActorAndVersionWithoutLeakingOtherAccounts(UserRoleCode role, string wireRole)
    {
        var user = await User(role); var other = await User(UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, user);
        await using var db = sql.CreateDbContext();
        var before = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        var audits = await db.AuditLogs.CountAsync(x => x.EntityId == user.Id);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var me = await Get(client, "/api/v1/me");
            Keys(me, "id", "displayName", "role", "version");
            Assert.Equal(user.Id, me.GetProperty("id").GetGuid());
            Assert.Equal(wireRole, me.GetProperty("role").GetString());
            Assert.Equal(Convert.ToBase64String(before.RowVersion), me.GetProperty("version").GetString());
            var profile = await Get(client, "/api/v1/profile");
            Keys(profile, "userId", "username", "displayName", "email", "roleCode", "status", "rowVersion");
            Assert.Equal(user.Id, profile.GetProperty("userId").GetGuid());
            Assert.Equal(user.Email, profile.GetProperty("email").GetString());
            Assert.Equal(Convert.ToBase64String(before.RowVersion), profile.GetProperty("rowVersion").GetString());
            Assert.DoesNotContain(other.Email!, profile.GetRawText());
        }
        Assert.Equal(before.RowVersion, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).RowVersion);
        Assert.Equal(audits, await db.AuditLogs.CountAsync(x => x.EntityId == user.Id));
        foreach (var session in await db.Sessions.Where(x => x.UserId == user.Id).ToArrayAsync()) session.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        foreach (var route in new[] { "/api/v1/me", "/api/v1/profile" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task CookieSessionHasDeclaredProjectionAndRevocationIsCheckedAtReadTime()
    {
        var user = await User(UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var cookie = await Login(factory, user, true);
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var bearer = await Login(factory, user);
        foreach (var denied in new[] { anonymous, bearer })
            Assert.Equal(HttpStatusCode.Unauthorized, (await denied.GetAsync("/api/v1/auth/web/session")).StatusCode);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var response = await cookie.GetAsync("/api/v1/auth/web/session");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl!.NoStore);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Keys(body, "user", "mustChangePassword", "issuedAt", "absoluteExpiresAt", "idleExpiresAt", "renewable", "ticketLifetimeSeconds");
            Keys(body.GetProperty("user"), "id", "displayName", "role", "version");
            Assert.Equal(user.Id, body.GetProperty("user").GetProperty("id").GetGuid());
            Assert.True(body.GetProperty("renewable").GetBoolean());
            Assert.Equal(43200, body.GetProperty("ticketLifetimeSeconds").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("absoluteExpiresAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("idleExpiresAt").ValueKind);
        }
        await using var db = sql.CreateDbContext();
        Assert.Equal(2, await db.Sessions.CountAsync(x => x.UserId == user.Id));
        var row = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id);
        foreach (var session in await db.Sessions.Where(x => x.UserId == user.Id).ToArrayAsync()) session.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.GetAsync("/api/v1/auth/web/session")).StatusCode);
        Assert.Equal(row.RowVersion, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).RowVersion);
    }

    [Fact]
    public async Task ProfileWorkflowReplaysOneMutationRejectsStaleVersionAndRevokesReads()
    {
        var user = await User(UserRoleCode.DroneOperator);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, user);
        var before = await Get(client, "/api/v1/me");
        var version = before.GetProperty("version").GetString();
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"" + version + "\"");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        for (var repeat = 0; repeat < 2; repeat++)
            Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "Synthetic updated actor" })).StatusCode);
        var after = await Get(client, "/api/v1/me");
        Assert.NotEqual(version, after.GetProperty("version").GetString());
        Assert.Equal("Synthetic updated actor", (await Get(client, "/api/v1/profile")).GetProperty("displayName").GetString());
        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await client.PatchAsJsonAsync("/api/v1/me", new { displayName = "Stale overwrite" })).StatusCode);
        await using var db = sql.CreateDbContext();
        Assert.Equal("Synthetic updated actor", (await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).DisplayName);
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.EntityId == user.Id && x.EventType == "user_profile_updated"));
        foreach (var session in await db.Sessions.Where(x => x.UserId == user.Id).ToArrayAsync()) session.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/profile")).StatusCode);
    }

    [Fact]
    public async Task DirectoryReadHasExactSchemaActorBoundCursorAndNoUserMutation()
    {
        var sup = await User(UserRoleCode.Supervisor); var otherSup = await User(UserRoleCode.Supervisor);
        var prefix = "DIRECTORY26-" + Guid.NewGuid().ToString("N");
        var a = await sql.CreateUserAsync(prefix + "-A", "Current1!", UserRoleCode.RepairCrew);
        var b = await sql.CreateUserAsync(prefix + "-B", "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, sup); using var other = await Login(factory, otherSup);
        var first = await Get(client, "/api/v1/users?search=" + prefix + "&limit=1");
        Keys(first, "items", "nextCursor", "asOf");
        var row = Assert.Single(first.GetProperty("items").EnumerateArray());
        Keys(row, "userId", "displayName", "email", "phoneNumber", "role", "status");
        var cursor = Uri.EscapeDataString(first.GetProperty("nextCursor").GetString()!);
        Assert.Equal(HttpStatusCode.BadRequest, (await other.GetAsync("/api/v1/users?search=" + prefix + "&limit=1&cursor=" + cursor)).StatusCode);
        var next = await Get(client, "/api/v1/users?search=" + prefix + "&limit=1&cursor=" + cursor);
        Assert.NotEqual(row.GetProperty("userId").GetGuid(), Assert.Single(next.GetProperty("items").EnumerateArray()).GetProperty("userId").GetGuid());
        await using var db = sql.CreateDbContext();
        Assert.Equal(a.RowVersion, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == a.Id)).RowVersion);
        Assert.Equal(b.RowVersion, (await db.Users.AsNoTracking().SingleAsync(x => x.Id == b.Id)).RowVersion);
    }

    private Task<ApplicationUser> User(UserRoleCode role) => sql.CreateUserAsync("READ26-" + Guid.NewGuid().ToString("N"), "Current1!", role);
    private static void Keys(JsonElement value, params string[] names) => Assert.Equal(names.OrderBy(x => x), value.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
    private static async Task<JsonElement> Get(HttpClient client, string route)
    {
        var response = await client.GetAsync(route); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static async Task<HttpClient> Login(AuthenticationWebApplicationFactory factory, ApplicationUser user, bool cookie = false)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = cookie });
        if (cookie) client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", (await Get(client, "/api/v1/auth/web/csrf")).GetProperty("requestToken").GetString());
        var response = await client.PostAsJsonAsync(cookie ? "/api/v1/auth/web/login" : "/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        if (cookie) client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        else client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }
}
