using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.DTOs.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H4RepairLifecycleHttpAdmissionTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData("cancellations")]
    [InlineData("normal-successors")]
    public async Task LifecycleRoutesRequireBearerPreconditionsBeforeMutation(string action)
    {
        var actor = await fixture.CreateUserAsync("h4-lifecycle-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var response = await client.PostAsJsonAsync(Path(action), Input(action));
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal("precondition_required", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }
    [Fact]
    public async Task LifecycleCookieMutationRequiresCsrf()
    {
        var actor = await fixture.CreateUserAsync("h4-lifecycle-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login",
            new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"AAAAAAAAAAA=\"");
        var response = await client.PostAsJsonAsync(Path("cancellations"), Input("cancellations"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
    private static string Path(string action) =>
        $"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{Guid.NewGuid()}/items/{Guid.NewGuid()}/{action}";
    private static object Input(string action) => action == "cancellations"
        ? new RepairCancellationInput("cancel unfinished work", null)
        : new RepairNormalContinuationInput("new plan", "checklist-v2", "normal continuation", null);
}
