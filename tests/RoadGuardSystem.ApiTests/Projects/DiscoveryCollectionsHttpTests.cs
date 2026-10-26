using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class DiscoveryCollectionsHttpTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("repair-packages")]
    [InlineData("exports")]
    [InlineData("survey-plans")]
    [InlineData("survey-tasks")]
    [InlineData("processing-jobs")]
    [InlineData("ai-mock-runs")]
    [InlineData("offline/devices")]
    [InlineData("offline/packages")]
    [InlineData("offline/grants")]
    [InlineData("retention/evaluations")]
    public async Task EmptyCollectionsEnforceCurrentAuthorityAndStrictQueryWithoutWrites(string suffix)
    {
        var actor = await User(UserRoleCode.ProjectManager); var wrong = await User(UserRoleCode.Reporter);
        var a = await Project(); var b = await Project(); var membership = await Member(a.Id, actor.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await Login(factory, actor); using var reporter = await Login(factory, wrong);
        using var anonymous = factory.CreateClient();
        var route = $"/api/v1/projects/{a.Id}/{suffix}";
        using (var denied = await anonymous.GetAsync(route))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("about:blank", problem.GetProperty("type").GetString());
            Assert.Equal("Collection request failed", problem.GetProperty("title").GetString());
            Assert.Equal("unauthorized", problem.GetProperty("code").GetString());
            Assert.Equal(route, problem.GetProperty("instance").GetString());
            Assert.True(denied.Headers.CacheControl?.NoStore);
        }
        await Status(reporter, route, HttpStatusCode.Forbidden);
        await Status(client, $"/api/v1/projects/{b.Id}/{suffix}?cursor=invalid", HttpStatusCode.Forbidden);
        var page = await Page(client, route);
        Assert.Empty(page.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, page.GetProperty("nextCursor").ValueKind);
        foreach (var query in new[] { "limit=0", "limit=101", "limit=1&limit=2", "limit=", "limit=+1", "cursor=", "cursor=invalid", "unexpected=x", "mode=NORMAL" })
            await Status(client, route + "?" + query, HttpStatusCode.BadRequest);
        await using var db = sql.CreateDbContext();
        Assert.Equal(membership.RowVersion, (await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.Id == membership.Id)).RowVersion);
        (await db.ProjectMembers.SingleAsync(x => x.Id == membership.Id)).Status = ProjectMemberStatus.Ended;
        await db.SaveChangesAsync();
        await Status(client, route, HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DevicePagesUseSqlGuidOrderBindCursorAndProjectOnlySafeFields(bool web)
    {
        var actor = await User(UserRoleCode.ProjectManager); var other = await User(UserRoleCode.ProjectManager);
        var a = await Project(); var b = await Project(); await Member(a.Id, actor.Id); await Member(b.Id, actor.Id); await Member(a.Id, other.Id);
        var ids = new List<Guid>();
        await using (var db = sql.CreateDbContext())
        {
            for (var i = 0; i < 5; i++)
            {
                var id = Guid.NewGuid(); var device = Guid.NewGuid(); ids.Add(id);
                using var keys = OfflineDeviceKeys.Generate(actor.Id, device.ToString("D"));
                db.Add(OfflineDeviceRegistration.Register(id, a.Id, actor.Id, device, 1, UserRoleCode.ProjectManager,
                    keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow));
            }
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = web ? await WebCookieTestClient.Login(factory, actor.Email!) : await Login(factory, actor); using var second = web ? await WebCookieTestClient.Login(factory, other.Email!) : await Login(factory, other);
        var route = $"/api/v1/projects/{a.Id}/offline/devices";
        var first = await Page(client, route + "?limit=2");
        var cursor = Uri.EscapeDataString(first.GetProperty("nextCursor").GetString()!);
        await Status(second, route + "?cursor=" + cursor, HttpStatusCode.BadRequest);
        await Status(client, $"/api/v1/projects/{b.Id}/offline/devices?cursor={cursor}", HttpStatusCode.BadRequest);
        await Status(client, $"/api/v1/projects/{a.Id}/offline/packages?cursor={cursor}", HttpStatusCode.BadRequest);
        var seen = new List<Guid>(); var page = first;
        do
        {
            foreach (var row in page.GetProperty("items").EnumerateArray())
            {
                Assert.Equal(new[] { "actorId", "deviceId", "id", "projectId", "registeredAt", "revision", "revokedAt", "role" }, row.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
                Assert.Equal(a.Id, row.GetProperty("projectId").GetGuid());
                Assert.Equal(actor.Id, row.GetProperty("actorId").GetGuid());
                seen.Add(row.GetProperty("id").GetGuid());
            }
            var next = page.GetProperty("nextCursor").GetString();
            if (next is null) break;
            page = await Page(client, route + "?limit=2&cursor=" + Uri.EscapeDataString(next));
        } while (true);
        await using var verification = sql.CreateDbContext();
        var expected = await verification.Set<OfflineDeviceRegistration>().Where(x => x.ProjectId == a.Id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync();
        Assert.Equal(expected, seen); Assert.Equal(5, seen.Distinct().Count());
        var revoke = OfflineDeviceRevocation.Record(Guid.NewGuid(), a.Id, seen[0], actor.Id, DateTimeOffset.UtcNow, "Synthetic retirement");
        verification.Add(revoke); await verification.SaveChangesAsync();
        Assert.Equal(JsonValueKind.String, (await Page(client, route)).GetProperty("items")[0].GetProperty("revokedAt").ValueKind);
        Assert.Equal(5, await verification.Set<OfflineDeviceRegistration>().CountAsync(x => x.ProjectId == a.Id));
    }

    [Theory]
    [InlineData("repair-packages")]
    [InlineData("exports")]
    [InlineData("survey-plans")]
    [InlineData("survey-tasks")]
    [InlineData("processing-jobs")]
    [InlineData("ai-mock-runs")]
    [InlineData("offline/devices")]
    [InlineData("offline/packages")]
    [InlineData("offline/grants")]
    [InlineData("retention/evaluations")]
    [InlineData("repair-packages/00000000-0000-0000-0000-000000000001/items")]
    public async Task WebCookieCollectionsEnforceProjectAndSessionAuthority(string suffix)
    {
        var actor = await User(UserRoleCode.ProjectManager); var wrong = await User(UserRoleCode.Reporter);
        var a = await Project(); var b = await Project(); await Member(a.Id, actor.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = await WebCookieTestClient.Login(factory, actor.Email!);
        using var reporter = await WebCookieTestClient.Login(factory, wrong.Email!);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); // GET requires no CSRF.
        var route = $"/api/v1/projects/{a.Id}/{suffix}";
        if (suffix.EndsWith("/items", StringComparison.Ordinal)) await Status(client, route, HttpStatusCode.NotFound);
        else { var page = await Page(client, route); Assert.Empty(page.GetProperty("items").EnumerateArray()); }
        await Status(reporter, route, HttpStatusCode.Forbidden);
        await Status(client, $"/api/v1/projects/{b.Id}/{suffix}", HttpStatusCode.Forbidden);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        await Status(client, route, HttpStatusCode.Unauthorized); // Never falls back to valid cookie.
        client.DefaultRequestHeaders.Authorization = null;
        await using var db = sql.CreateDbContext();
        await db.ProjectMembers.Where(x => x.ProjectId == a.Id && x.UserId == actor.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        await Status(client, route, HttpStatusCode.Forbidden);
        await db.Sessions.Where(x => x.UserId == actor.Id).ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        await Status(client, route, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepairItemCollectionAcceptsBothTransportsWithApprovedMode(bool web)
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var obligation = RoadGuardSystem.BusinessObjects.Repairs.RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect,
            RoadGuardSystem.BusinessObjects.Repairs.RepairObligationKind.FormalRepair, true,
            RoadGuardSystem.BusinessObjects.Repairs.RepairActualScope.Create(Guid.NewGuid(), source.Road, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        var package = RoadGuardSystem.BusinessObjects.Repairs.RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package); await db.SaveChangesAsync();
        var actor = await db.Users.SingleAsync(x => x.Id == source.Pm);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = web ? await WebCookieTestClient.Login(factory, actor.Email!) : await Login(factory, actor);
        var route = $"/api/v1/projects/{source.Project}/repair-packages/{package.Id}/items";
        foreach (var mode in new[] { "NORMAL", "FAST_TRACK" })
            Assert.Empty((await Page(client, route + "?mode=" + mode)).GetProperty("items").EnumerateArray());
        await Status(client, route + "?mode=unknown", HttpStatusCode.BadRequest);
        await Status(client, route + "?cursor=invalid", HttpStatusCode.BadRequest);
        await Status(client, $"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{package.Id}/items", HttpStatusCode.Forbidden);
        await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == actor.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        await Status(client, route, HttpStatusCode.Forbidden);
    }

    private Task<ApplicationUser> User(UserRoleCode role) => sql.CreateUserAsync("DISC-" + Guid.NewGuid().ToString("N"), "Current1!", role);
    private async Task<Project> Project()
    {
        var row = RoadGuardSystem.BusinessObjects.Projects.Project.Create(Guid.NewGuid(), "DISC-" + Guid.NewGuid().ToString("N"), "Discovery fixture", null, null, null, null, DateTimeOffset.UtcNow);
        await using var db = sql.CreateDbContext(); db.Add(row); await db.SaveChangesAsync(); return row;
    }
    private async Task<ProjectMember> Member(Guid project, Guid user)
    {
        var row = new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = user, RoleCode = UserRoleCode.ProjectManager, Status = ProjectMemberStatus.Active, ValidFrom = new DateOnly(2000, 1, 1) };
        await using var db = sql.CreateDbContext(); db.Add(row); await db.SaveChangesAsync(); return row;
    }
    private static async Task<HttpClient> Login(AuthenticationWebApplicationFactory factory, ApplicationUser user)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        return client;
    }
    private static async Task<JsonElement> Page(HttpClient client, string route)
    {
        var response = await client.GetAsync(route); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "items", "nextCursor" }, body.EnumerateObject().Select(x => x.Name).OrderBy(x => x)); return body;
    }
    private static async Task Status(HttpClient client, string route, HttpStatusCode status)
    {
        var response = await client.GetAsync(route); Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("code", out _)); Assert.True(body.TryGetProperty("correlationId", out _));
    }
}
