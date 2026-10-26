using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class CollectionQueryContractHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task EveryApprovedCollectionEnforcesItsExactQueryAndErrorContract()
    {
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect,
            RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), source.Road, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package); await db.SaveChangesAsync();
        var actor = await db.Users.SingleAsync(x => x.Id == source.Pm);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var suffixes = new[] { "ai-mock-runs", "repair-packages", $"repair-packages/{package.Id}/items", "exports", "survey-plans", "survey-tasks", "processing-jobs", "offline/devices", "offline/packages", "offline/grants", "retention/evaluations" };
        foreach (var suffix in suffixes)
        {
            var route = $"/api/v1/projects/{source.Project}/{suffix}";
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route.Replace(source.Project.ToString(), "not-a-guid", StringComparison.Ordinal))).StatusCode);
            if (suffix.EndsWith("/items", StringComparison.Ordinal))
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route.Replace(package.Id.ToString(), "not-a-guid", StringComparison.Ordinal))).StatusCode);
            foreach (var valid in new[] { "", "?limit=1", "?limit=25", "?limit=100" })
            {
                var response = await client.GetAsync(route + valid);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
                Assert.True(response.Headers.CacheControl!.NoStore);
                var page = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(new[] { "items", "nextCursor" }, page.EnumerateObject().Select(x => x.Name).OrderBy(x => x));
                Assert.Equal(JsonValueKind.Array, page.GetProperty("items").ValueKind);
            }
            var invalid = new List<string> { "limit=0", "limit=101", "limit=-1", "limit=%2B1", "limit=1.0", "limit=2147483648", "limit=", "limit=1&limit=2", "Limit=1", "cursor=", "cursor=invalid", "cursor=a&cursor=b", "Cursor=x", "cursor=" + new string('a', 4097), "unexpected=x", "pageSize=1" };
            if (suffix.EndsWith("/items", StringComparison.Ordinal))
            {
                foreach (var mode in new[] { "NORMAL", "FAST_TRACK" })
                    Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route + "?mode=" + mode)).StatusCode);
                invalid.AddRange(["mode=", "mode=normal", "mode=UNKNOWN", "mode=NORMAL&mode=FAST_TRACK", "Mode=NORMAL"]);
            }
            else invalid.AddRange(["mode=NORMAL", "mode=FAST_TRACK"]);
            foreach (var query in invalid)
            {
                var response = await client.GetAsync(route + "?" + query);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
                Assert.True(response.Headers.CacheControl!.NoStore);
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("about:blank", problem.GetProperty("type").GetString());
                Assert.Equal("Collection request failed", problem.GetProperty("title").GetString());
                Assert.Equal(400, problem.GetProperty("status").GetInt32());
                Assert.Equal(route, problem.GetProperty("instance").GetString());
                Assert.Equal("validation_error", problem.GetProperty("code").GetString());
                Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("correlationId").GetString()));
            }
        }
        Assert.Equal(1, await db.RepairPackages.CountAsync(x => x.Id == package.Id));
    }
}
