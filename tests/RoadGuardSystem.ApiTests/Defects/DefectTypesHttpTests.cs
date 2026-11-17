using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.DTOs.Defects;
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

    [Fact]
    public async Task SupervisorCatalogueWebWritesAreVersionedAndInactiveCodesDisappearFromPmOptions()
    {
        var sql = new AuthenticationSqlServerFixture();
        await sql.InitializeAsync();
        try
        {
            const string path = "/api/v1/catalog/defect-types/admin";
            var suffix = Guid.NewGuid().ToString("N");
            var pm = await sql.CreateUserAsync($"catalog-admin-pm-{suffix}", "Current1!", UserRoleCode.ProjectManager);
            var supervisor = await sql.CreateUserAsync($"catalog-admin-sup-{suffix}", "Current1!", UserRoleCode.Supervisor);
            await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
            using var supWeb = await WebCookieTestClient.Login(factory, supervisor.Email!);
            using var pmWeb = await WebCookieTestClient.Login(factory, pm.Email!);
            Assert.Equal(HttpStatusCode.Forbidden, (await pmWeb.GetAsync(path)).StatusCode);
            var input = new CreateDefectTypeDto($" defect_{suffix} ", "New defect", "Demo", true, "Add valid code");
            var key = Guid.NewGuid().ToString("N");
            supWeb.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            Assert.Equal(HttpStatusCode.Forbidden, (await Write(HttpMethod.Post, path, input, key)).StatusCode);
            supWeb.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
            Assert.Equal(HttpStatusCode.Forbidden, (await Write(HttpMethod.Post, path, input, key)).StatusCode);
            await WebCookieTestClient.RefreshCsrf(supWeb);
            using var created = await Write(HttpMethod.Post, path, input, key);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var item = (await created.Content.ReadFromJsonAsync<DefectTypeAdminDto>())!;
            Assert.Equal(input.Code.Trim().ToUpperInvariant(), item.Code);
            Assert.Equal('"' + item.Version + '"', created.Headers.ETag!.Tag);
            using var replay = await Write(HttpMethod.Post, path, input, key);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Equal(item, await replay.Content.ReadFromJsonAsync<DefectTypeAdminDto>());
            Assert.Equal(HttpStatusCode.Conflict,
                (await Write(HttpMethod.Post, path, input with { Name = "Changed" }, key)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,
                (await Write(HttpMethod.Post, path, input, Guid.NewGuid().ToString("N"))).StatusCode);

            var rows = await supWeb.GetFromJsonAsync<DefectTypeAdminDto[]>(path);
            Assert.Equal(item, Assert.Single(rows!, row => row.Code == item.Code));
            var pmOptions = await pmWeb.GetFromJsonAsync<DefectTypeOptionDto[]>("/api/v1/catalog/defect-types");
            Assert.Contains(pmOptions!, row => row.Code == item.Code);
            var update = new UpdateDefectTypeDto("Retired label", "Kept for history", false, "Phase out");
            var updatePath = path + "/" + item.Code;
            var updates = await Task.WhenAll(
                Write(HttpMethod.Put, updatePath, update, Guid.NewGuid().ToString("N"), created.Headers.ETag.Tag),
                Write(HttpMethod.Put, updatePath, update, Guid.NewGuid().ToString("N"), created.Headers.ETag.Tag));
            Assert.Single(updates, response => response.StatusCode == HttpStatusCode.OK);
            Assert.Single(updates, response => response.StatusCode == HttpStatusCode.Conflict);
            var inactive = Assert.Single((await supWeb.GetFromJsonAsync<DefectTypeAdminDto[]>(path))!, row => row.Code == item.Code);
            Assert.False(inactive.IsActive);
            Assert.Equal(update.Name, inactive.Name);
            Assert.NotEqual(item.Version, inactive.Version);
            Assert.DoesNotContain((await pmWeb.GetFromJsonAsync<DefectTypeOptionDto[]>("/api/v1/catalog/defect-types"))!,
                row => row.Code == item.Code);

            async Task<HttpResponseMessage> Write(HttpMethod method, string url, object inputBody, string requestKey,
                string? etag = null)
            {
                using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(inputBody) };
                request.Headers.Add("Idempotency-Key", requestKey);
                if (etag is not null) request.Headers.Add("If-Match", etag);
                return await supWeb.SendAsync(request);
            }
        }
        finally { await sql.DisposeAsync(); }
    }

    private static void AssertOption(JsonElement option, string code, string name)
    {
        Assert.Equal(2, option.EnumerateObject().Count());
        Assert.Equal(code, option.GetProperty("code").GetString());
        Assert.Equal(name, option.GetProperty("name").GetString());
    }
}
