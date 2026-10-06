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
public sealed class H4RepairSafetyHttpAdmissionTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData("create", UserRoleCode.ProjectManager)]
    [InlineData("install", UserRoleCode.RepairCrew)]
    [InlineData("check", UserRoleCode.RepairCrew)]
    [InlineData("ack", UserRoleCode.RepairCrew)]
    public async Task SafetyMutationsRequirePreconditionsBeforeResourceMutation(string action, UserRoleCode role)
    {
        var actor = await fixture.CreateUserAsync("safety-http-" + Guid.NewGuid().ToString("N"),
            "Current1!", role);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var response = await client.PostAsJsonAsync(Path(action), Input(action));
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal("precondition_required", (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task CrewCannotConfigureMeasureEvenWithValidHeaders()
    {
        var actor = await fixture.CreateUserAsync("safety-role-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.RepairCrew);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"AAAAAAAAAAA=\"");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Path("create"),
            Input("create"))).StatusCode);
    }

    private static string Path(string action)
    {
        var root = $"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{Guid.NewGuid()}/items/{Guid.NewGuid()}/safety-measures";
        var measure = root + "/" + Guid.NewGuid();
        return action switch
        {
            "create" => root,
            "install" => measure + "/installation",
            "check" => measure + "/checks",
            "ack" => measure + "/warnings/" + Guid.NewGuid() + "/acknowledgement",
            _ => throw new ArgumentException("Unknown safety action.")
        };
    }

    private static object Input(string action) => action switch
    {
        "create" => new RepairSafetyCreateInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "each shift", "replace if displaced", "after repair", "approved scope"),
        "install" => new RepairSafetyInstallInput(DateTimeOffset.UtcNow.AddHours(12), "installed"),
        "check" => new RepairSafetyCheckInput("Safe", "barrier intact", [Guid.NewGuid()]),
        "ack" => new RepairSafetyAcknowledgementInput("received and checked"),
        _ => throw new ArgumentException("Unknown safety action.")
    };
}
