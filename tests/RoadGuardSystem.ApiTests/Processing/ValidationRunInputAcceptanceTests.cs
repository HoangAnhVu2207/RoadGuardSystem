using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ValidationRunInputAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData("pairs-missing", 400)]
    [InlineData("pairs-null", 400)]
    [InlineData("pairs-empty", 400)]
    [InlineData("pair-null", 400)]
    [InlineData("type-missing", 400)]
    [InlineData("type-null", 400)]
    [InlineData("type-empty", 400)]
    [InlineData("unit-missing", 400)]
    [InlineData("unit-null", 400)]
    [InlineData("unit-empty", 400)]
    [InlineData("model-malformed", 400)]
    [InlineData("split-malformed", 400)]
    [InlineData("truth-malformed", 400)]
    [InlineData("derived-malformed", 400)]
    [InlineData("model-empty", 422)]
    [InlineData("split-empty", 422)]
    [InlineData("truth-empty", 422)]
    [InlineData("derived-empty", 422)]
    [InlineData("type-whitespace", 400)]
    [InlineData("unit-whitespace", 400)]
    [InlineData("key-missing", 428)]
    [InlineData("key-whitespace", 428)]
    [InlineData("body-null", 400)]
    [InlineData("body-malformed", 400)]
    [InlineData("media", 415)]
    public async Task ExactValidationInputRejectsWithoutBusinessEffects(string scenario, int expected)
    {
        var actor = await sql.CreateUserAsync("validation-input-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), "VIN-" + Guid.NewGuid().ToString("N"), "Owned validation input", null, 32648, null, null, DateTimeOffset.UtcNow);
        await using (var db = sql.CreateDbContext())
        {
            db.Add(project);
            db.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = actor.Id, RoleCode = UserRoleCode.ProjectManager, Status = ProjectMemberStatus.Active, ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) });
            await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services => services.RemoveAll<IHostedService>());
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(actor.UserName!), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var pair = new Dictionary<string, object?> { ["groundTruthId"] = Guid.NewGuid(), ["derivedMeasurementId"] = Guid.NewGuid() };
        var input = new Dictionary<string, object?> { ["pairs"] = new object?[] { pair }, ["modelVersionId"] = Guid.NewGuid(), ["datasetSplitId"] = Guid.NewGuid(), ["measurementType"] = "DEPRESSION_DEPTH", ["unit"] = "mm" };
        switch (scenario)
        {
            case "pairs-missing": input.Remove("pairs"); break;
            case "pairs-null": input["pairs"] = null; break;
            case "pairs-empty": input["pairs"] = Array.Empty<object>(); break;
            case "pair-null": input["pairs"] = new object?[] { null }; break;
            case "type-missing": input.Remove("measurementType"); break;
            case "type-null": input["measurementType"] = null; break;
            case "type-empty": input["measurementType"] = ""; break;
            case "type-whitespace": input["measurementType"] = " "; break;
            case "unit-missing": input.Remove("unit"); break;
            case "unit-null": input["unit"] = null; break;
            case "unit-empty": input["unit"] = ""; break;
            case "unit-whitespace": input["unit"] = " "; break;
            case "model-malformed": input["modelVersionId"] = "bad-uuid"; break;
            case "split-malformed": input["datasetSplitId"] = "bad-uuid"; break;
            case "truth-malformed": pair["groundTruthId"] = "bad-uuid"; break;
            case "derived-malformed": pair["derivedMeasurementId"] = "bad-uuid"; break;
            case "model-empty": input["modelVersionId"] = Guid.Empty; break;
            case "split-empty": input["datasetSplitId"] = Guid.Empty; break;
            case "truth-empty": pair["groundTruthId"] = Guid.Empty; break;
            case "derived-empty": pair["derivedMeasurementId"] = Guid.Empty; break;
        }
        var json = scenario == "body-null" ? "null" : scenario == "body-malformed" ? "{" : JsonSerializer.Serialize(input);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{project.Id}/validation-runs")
        { Content = new StringContent(json, Encoding.UTF8, scenario == "media" ? "text/plain" : "application/json") };
        if (scenario != "key-missing") request.Headers.TryAddWithoutValidation("Idempotency-Key", scenario == "key-whitespace" ? "   " : Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expected, body.GetProperty("status").GetInt32());
        Assert.Equal(expected == 415 ? "unsupported_media_type" : "validation_error", body.GetProperty("code").GetString());
        var correlation = Assert.Single(response.Headers.GetValues("X-Correlation-ID"));
        Assert.True(Guid.TryParse(correlation, out _));
        Assert.Equal(correlation, body.GetProperty("correlationId").GetString());
        Assert.Null(response.Headers.ETag); Assert.Null(response.Headers.Location);
        await using var verify = sql.CreateDbContext();
        Assert.Empty(await verify.ValidationRuns.Where(x => x.ProjectId == project.Id).ToArrayAsync());
        Assert.Empty(await verify.IdempotencyRecords.Where(x => x.ProjectId == project.Id).ToArrayAsync());
        Assert.Empty(await verify.AuditLogs.Where(x => x.ActorUserId == actor.Id).ToArrayAsync());
    }
}
