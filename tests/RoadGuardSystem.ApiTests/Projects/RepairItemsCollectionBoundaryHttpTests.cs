using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

// Named case binds ADD-03 without relying on a truncated theory route argument.
[Collection(AuthenticationApiFixture.Name)]
public sealed class RepairItemsCollectionBoundaryHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task ExistingPackageItemsRecheckRoleProjectMembershipAndSession()
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect,
            RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), source.Road, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package);
        await db.SaveChangesAsync();
        var email = await db.Users.Where(x => x.Id == source.Pm).Select(x => x.Email).SingleAsync();
        var reporter = await sql.CreateUserAsync("ITEMS-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var pm = await WebCookieTestClient.Login(factory, email!);
        using var wrong = await WebCookieTestClient.Login(factory, reporter.Email!);
        pm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var route = $"/api/v1/projects/{source.Project}/repair-packages/{package.Id}/items";
        var page = await pm.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.True(page.Headers.CacheControl!.NoStore);
        var body = await page.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "items", "nextCursor" }, body.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Empty(body.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("nextCursor").ValueKind);
        var roleDenied = await wrong.GetAsync(route);
        Assert.Equal(HttpStatusCode.Forbidden, roleDenied.StatusCode);
        var projectDenied = await pm.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{package.Id}/items");
        Assert.Equal(HttpStatusCode.Forbidden, projectDenied.StatusCode);
        await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == source.Pm)
            .ExecuteUpdateAsync(x => x.SetProperty(v => v.Status, ProjectMemberStatus.Ended));
        var membershipDenied = await pm.GetAsync(route);
        Assert.Equal(HttpStatusCode.Forbidden, membershipDenied.StatusCode);
        await db.Sessions.Where(x => x.UserId == source.Pm)
            .ExecuteUpdateAsync(x => x.SetProperty(v => v.RevokedAt, DateTimeOffset.UtcNow));
        var sessionDenied = await pm.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, sessionDenied.StatusCode);
        foreach (var response in new[] { roleDenied, projectDenied, membershipDenied, sessionDenied })
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)response.StatusCode, problem.GetProperty("status").GetInt32());
            Assert.True(problem.TryGetProperty("code", out _));
            Assert.True(problem.TryGetProperty("correlationId", out _));
        }
    }
}
