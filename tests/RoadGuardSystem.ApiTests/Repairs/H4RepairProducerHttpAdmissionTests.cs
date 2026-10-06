using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Repairs;
using Xunit;

namespace RoadGuardSystem.ApiTests.Repairs;

[Collection(AuthenticationApiFixture.Name)]
public sealed class H4RepairProducerHttpAdmissionTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData("create", UserRoleCode.ProjectManager)]
    [InlineData("propose", UserRoleCode.ProjectManager)]
    [InlineData("approve", UserRoleCode.Supervisor)]
    [InlineData("assign", UserRoleCode.ProjectManager)]
    public async Task ActualProducingRoutesRequireBearerPreconditionsBeforeScopeMutation(string action, UserRoleCode role)
    {
        var actor = await fixture.CreateUserAsync("h4-producing-http-" + Guid.NewGuid().ToString("N"), "Current1!", role);
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
    public async Task CookiePackageCreationRequiresCsrfBeforeProducingAdmission()
    {
        var actor = await fixture.CreateUserAsync("h4-producing-cookie-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var csrf = (await (await client.GetAsync("/api/v1/auth/web/csrf")).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("requestToken").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/web/login",
            new { email = actor.Email, password = "Current1!" })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "cookie-package");
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"AAAAAAAAAAA=\"");
        var response = await client.PostAsJsonAsync(Path("create"), Input("create"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("csrf_failed", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext(); Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }
    private static string Path(string action)
    {
        var root = $"/api/v1/projects/{Guid.NewGuid()}/repair-packages";
        return action switch
        {
            "create" => root,
            "propose" => root + $"/{Guid.NewGuid()}/items",
            "approve" => root + $"/{Guid.NewGuid()}/items/{Guid.NewGuid()}/approve",
            "assign" => root + $"/{Guid.NewGuid()}/items/{Guid.NewGuid()}/assign",
            _ => throw new ArgumentException("Unknown test action.")
        };
    }
    private static object Input(string action) => action switch
    {
        "create" => new RepairPackageCreateInput(Guid.NewGuid(), "AAAAAAAAAAA=",
            [new("FORMAL_REPAIR", true, new(Guid.NewGuid(), Guid.NewGuid(), null, null, null, 1, 2, 0, 1), "actual obligation")], "create"),
        "propose" => new RepairItemProposeInput(Guid.NewGuid(), "NORMAL", "repair plan", "checklist-v1", "propose"),
        "approve" => new RepairDecisionInput("approve"),
        "assign" => new RepairItemAssignInput(new FieldTaskCreateInput(Guid.NewGuid(), "AAAAAAAAAAA=", null, "REPORTER",
            Guid.NewGuid(), null, null, null, "POST_REPAIR", 1, "{}", null, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1)), null, "assign"),
        _ => throw new ArgumentException("Unknown test action.")
    };
}
