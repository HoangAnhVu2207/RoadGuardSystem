using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class CollectionRoleMatrixAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("ADD-01", "ai-mock-runs", "PM_SUP")]
    [InlineData("ADD-02", "repair-packages", "PM_SUP")]
    [InlineData("ADD-03", "repair-packages/{package}/items", "PM_SUP")]
    [InlineData("ADD-04", "exports", "PM_SUP")]
    [InlineData("ADD-05", "survey-plans", "PM_SUP")]
    [InlineData("ADD-06", "survey-tasks", "PM_SUP_DRONE")]
    [InlineData("ADD-07", "processing-jobs", "PM_SUP_DRONE")]
    [InlineData("ADD-08", "offline/devices", "PM_SUP_CREW")]
    [InlineData("ADD-09", "offline/packages", "PM_SUP_CREW")]
    [InlineData("ADD-10", "offline/grants", "PM_SUP_CREW")]
    [InlineData("ADD-11", "retention/evaluations", "PM_SUP")]
    public async Task EveryUnsupportedRoleDeniesExactCollectionWithoutEffects(string operation, string suffix, string roleGroup)
    {
        Assert.StartsWith("ADD-", operation);
        await using var db = sql.CreateDbContext();
        var source = await GenuineProtectedSource.Seed(db, sql);
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect,
            RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), source.Road, "TEST_ONLY_FRAME_V1", "TEST_ONLY", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package); await db.SaveChangesAsync();
        var route = $"/api/v1/projects/{source.Project}/" + suffix.Replace("{package}", package.Id.ToString(), StringComparison.Ordinal);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        foreach (var role in new[] { UserRoleCode.Reporter, UserRoleCode.DroneOperator, UserRoleCode.RepairCrew })
        {
            if (role == UserRoleCode.DroneOperator && roleGroup == "PM_SUP_DRONE") continue;
            if (role == UserRoleCode.RepairCrew && roleGroup == "PM_SUP_CREW") continue;
            var actor = await sql.CreateUserAsync("COL-ROLE-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            using var web = await WebCookieTestClient.Login(factory, actor.Email!);
            using var bearer = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            var login = await bearer.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            bearer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            var audit = await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id);
            var receipts = await db.IdempotencyRecords.CountAsync(a => a.ActorUserId == actor.Id);
            foreach (var client in new[] { web, bearer })
            {
                var response = await client.GetAsync(route);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
                Assert.True(response.Headers.CacheControl?.NoStore);
                Assert.Null(response.Headers.ETag);
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(403, problem.GetProperty("status").GetInt32());
                Assert.Equal("access_forbidden", problem.GetProperty("code").GetString());
                Assert.Equal(route, problem.GetProperty("instance").GetString());
                Assert.Equal(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), problem.GetProperty("correlationId").GetString());
                Assert.False(problem.TryGetProperty("items", out _));
            }
            Assert.Equal(audit, await db.AuditLogs.CountAsync(a => a.ActorUserId == actor.Id));
            Assert.Equal(receipts, await db.IdempotencyRecords.CountAsync(a => a.ActorUserId == actor.Id));
        }
        Assert.True(await db.Set<RepairPackage>().AnyAsync(p => p.Id == package.Id));
    }
}
