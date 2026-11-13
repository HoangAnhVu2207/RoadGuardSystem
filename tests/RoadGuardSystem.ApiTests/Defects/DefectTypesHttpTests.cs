using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Defects;

public sealed class DefectTypesHttpTests
{
    [Fact]
    public async Task WebCatalogRequiresPmAndReturnsOnlyActiveTypesInCodeOrder()
    {
        var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAsync();
        try
        {
            const string path = "/api/v1/catalog/defect-types";
            var suffix = Guid.NewGuid().ToString("N");
            var pm = await sql.CreateUserAsync($"catalog-pm-{suffix}", "Current1!", UserRoleCode.ProjectManager);
            var supervisor = await sql.CreateUserAsync($"catalog-sup-{suffix}", "Current1!", UserRoleCode.Supervisor);
            await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
            using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            using var webPm = await WebCookieTestClient.Login(factory, pm.Email!);
            using var webSupervisor = await WebCookieTestClient.Login(factory, supervisor.Email!);
            webPm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await webSupervisor.GetAsync(path)).StatusCode);
            var firstCode = $"A_TEST_{suffix}";
            var lastCode = $"Z_TEST_{suffix}";
            var inactiveCode = $"I_TEST_{suffix}";
            await using (var db = sql.CreateDbContext())
            {
                db.DefectTypes.AddRange(
                    DefectType.Create(lastCode, "Last", "Internal description"),
                    DefectType.Create(inactiveCode, "Retired", isActive: false),
                    DefectType.Create(firstCode, "First"));
                await db.SaveChangesAsync();
            }

            var options = await webPm.GetFromJsonAsync<JsonElement>(path);
            var rows = options.EnumerateArray().ToArray();
            var firstIndex = Array.FindIndex(rows, option => option.GetProperty("code").GetString() == firstCode);
            var lastIndex = Array.FindIndex(rows, option => option.GetProperty("code").GetString() == lastCode);
            Assert.True(firstIndex >= 0 && lastIndex > firstIndex);
            Assert.DoesNotContain(rows, option => option.GetProperty("code").GetString() == inactiveCode);
            AssertOption(rows[firstIndex], firstCode, "First");
            AssertOption(rows[lastIndex], lastCode, "Last");
        }
        finally
        {
            await sql.DisposeAsync();
        }
    }

    private static void AssertOption(JsonElement option, string code, string name)
    {
        Assert.Equal(2, option.EnumerateObject().Count());
        Assert.Equal(code, option.GetProperty("code").GetString());
        Assert.Equal(name, option.GetProperty("name").GetString());
    }
}
