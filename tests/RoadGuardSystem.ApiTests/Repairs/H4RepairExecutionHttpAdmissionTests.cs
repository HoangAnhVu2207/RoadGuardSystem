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
public sealed class H4RepairExecutionHttpAdmissionTests(AuthenticationSqlServerFixture fixture)
{
    [Theory]
    [InlineData("assessment", UserRoleCode.RepairCrew)]
    [InlineData("execution-start", UserRoleCode.RepairCrew)]
    [InlineData("execution-finish", UserRoleCode.RepairCrew)]
    [InlineData("attempts", UserRoleCode.RepairCrew)]
    [InlineData("attempts/supplements", UserRoleCode.RepairCrew)]
    [InlineData("attempts/reviews", UserRoleCode.ProjectManager)]
    public async Task RepairExecutionRoutesRequirePreconditionsBeforeAnyScopeOrReceiptMutation(string action, UserRoleCode role)
    {
        var actor = await fixture.CreateUserAsync("h4-execution-http-" + Guid.NewGuid().ToString("N"), "Current1!", role);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var path = $"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{Guid.NewGuid()}/items/{Guid.NewGuid()}/tasks/{Guid.NewGuid()}/{action}";
        object body = action switch
        {
            "assessment" => new RepairMeasurementAssessmentInput(Guid.NewGuid(), Guid.NewGuid(), null, null, null),
            "execution-start" => new RepairExecutionStartInput(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid()),
            "execution-finish" => new RepairExecutionFinishInput(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow),
            "attempts" => new RepairAttemptSubmitInput(Guid.NewGuid(), new string('a', 64), Guid.NewGuid()),
            "attempts/supplements" => new RepairAttemptSupplementInput(Guid.NewGuid(), new string('a', 64)),
            "attempts/reviews" => new RepairAttemptReviewInput(Guid.NewGuid(), "SUPPLEMENT", "missing evidence"),
            _ => throw new ArgumentException("Unknown finite test action.")
        };
        var response = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal("precondition_required", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }

    [Fact]
    public async Task SupervisorFinalRouteRequiresCommandPreconditions()
    {
        var actor = await fixture.CreateUserAsync("h4-final-http-" + Guid.NewGuid().ToString("N"),
            "Current1!", UserRoleCode.Supervisor);
        await using var factory = new AuthenticationWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = actor.Email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var path = $"/api/v1/projects/{Guid.NewGuid()}/repair-packages/{Guid.NewGuid()}/items/{Guid.NewGuid()}/final-confirmation";
        var response = await client.PostAsJsonAsync(path, new RepairDecisionInput("verified normal repair"));
        Assert.Equal(HttpStatusCode.PreconditionRequired, response.StatusCode);
        Assert.Equal("precondition_required", (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());
        await using var db = fixture.CreateDbContext();
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.ActorUserId == actor.Id));
    }
}
