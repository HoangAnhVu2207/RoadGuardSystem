using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Services.Files;
using System.Security.Cryptography;

namespace RoadGuardSystem.ApiTests.Inspections;

[Collection(AuthenticationApiFixture.Name)]
public sealed partial class FieldEvidenceLocationHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task SubmissionReadbackPreservesDeclaredProofForExactRevision()
    {
        var pm = await sql.CreateUserAsync($"gps-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var crew = await sql.CreateUserAsync($"gps-crew-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var other = await sql.CreateUserAsync($"gps-other-{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var reporter = await sql.CreateUserAsync($"gps-reporter-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var source = await H3FieldInspectionWorkflowHttpTests.Seed(sql, pm.Id, crew.Id, other.Id, reporter.Id);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        await Login(client, pm.UserName!);
        var root = $"/api/v1/projects/{source.Project}/field-inspection-tasks";
        using var created = await Post(client, root, new
        {
            defectId = source.Defect,
            defectVersion = source.Version,
            sourceKind = "REPORTER",
            routeVersionId = source.Route,
            segmentSetId = source.Set,
            purpose = "PRE_MEASUREMENT",
            requiredMeasurementType = 4,
            measurementScope = "{}",
            assignedToUserId = crew.Id,
            dueAt = DateTimeOffset.UtcNow.AddDays(1)
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var path = root + "/" + (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Login(client, crew.UserName!);
        using var accepted = await Post(client, path + "/accept", new { reason = "Accept" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        using var started = await Post(client, path + "/start", new { originId = Guid.NewGuid(), claimedAt = DateTimeOffset.UtcNow }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        var start = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var submitted = await Post(client, path + "/submissions", new
        {
            originId = Guid.NewGuid(),
            startOriginId = start,
            captureType = "MEASUREMENT",
            locationProof = new { kind = "POSITION_CHECKLIST", checklist = "ROUTE_CHAINAGE_MARKINGS_CONFIRMED", observedRouteVersionId = source.Route, observedChainageMeters = 5.25 }
        }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, submitted.StatusCode);
        var id = (await submitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var reopened = factory.CreateClient();
        await Login(reopened, crew.UserName!);
        using var response = await reopened.GetAsync(path + "/submissions/" + id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("locationProof", out var proof), "API-096 must return the declared proof, currently omitted on baseline.");
        Assert.Equal("POSITION_CHECKLIST", proof.GetProperty("kind").GetString());
        Assert.Equal(5.25, proof.GetProperty("observedChainageMeters").GetDouble());
        Assert.Equal(source.Route, proof.GetProperty("observedRouteVersionId").GetGuid());
        await Login(client, pm.UserName!);
        using var review = await Post(client, path + "/reviews", new { submissionId = id, decision = "SUPPLEMENT", reason = "Add measurements" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);
        await Login(client, crew.UserName!);
        using var supplement = await Post(client, path + "/submissions", new
        {
            originId = Guid.NewGuid(),
            startOriginId = start,
            parentSubmissionId = id,
            captureType = "MEASUREMENT",
            locationProof = new { kind = "UNKNOWN" }
        }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, supplement.StatusCode);
        var next = (await supplement.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var old = await reopened.GetFromJsonAsync<JsonElement>(path + "/submissions/" + id);
        Assert.Equal(5.25, old.GetProperty("locationProof").GetProperty("observedChainageMeters").GetDouble());
        var unknown = await reopened.GetFromJsonAsync<JsonElement>(path + "/submissions/" + next);
        Assert.Equal("UNKNOWN", unknown.GetProperty("locationProof").GetProperty("kind").GetString());
        await Login(client, pm.UserName!);
        using var again = await Post(client, path + "/reviews", new { submissionId = next, decision = "SUPPLEMENT", reason = "Still missing" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        await Login(client, crew.UserName!);
        using var absent = await Post(client, path + "/submissions", new { originId = Guid.NewGuid(), startOriginId = start, parentSubmissionId = next, captureType = "MEASUREMENT" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, absent.StatusCode);
        var absentBody = await absent.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, absentBody.GetProperty("locationProof").ValueKind);
        foreach (var deniedUser in new[] { other.UserName!, reporter.UserName! })
        {
            await Login(client, deniedUser);
            using var denied = await client.GetAsync(path + "/submissions/" + id);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        await Login(client, pm.UserName!);
        using var wrongProject = await client.GetAsync(path.Replace(source.Project.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal) + "/submissions/" + id);
        Assert.Equal(HttpStatusCode.Forbidden, wrongProject.StatusCode);
        var absentId = absentBody.GetProperty("id").GetGuid();
        using var beforeReassign = await Post(client, path + "/reviews", new { submissionId = absentId, decision = "SUPPLEMENT", reason = "Reassign pending work" }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, beforeReassign.StatusCode);
        using var changed = await Post(client, path + "/reassign", new { reason = "New Crew", assignedToUserId = other.Id }, version: await Version(client, path));
        Assert.Equal(HttpStatusCode.Created, changed.StatusCode);
        using var former = await reopened.GetAsync(path + "/submissions/" + id);
        Assert.Equal(HttpStatusCode.Forbidden, former.StatusCode);
        await using var db = sql.CreateDbContext();
        db.Add(FieldInspectionLocationProof.Create(Guid.NewGuid(), source.Project, (await db.Set<FieldInspectionSubmission>().SingleAsync(x => x.Id == id)).TaskId, id, "UNKNOWN", "{\"kind\":\"UNKNOWN\"}"));
        await db.SaveChangesAsync();
        using var inconsistent = await client.GetAsync(path + "/submissions/" + id);
        Assert.Equal(HttpStatusCode.Conflict, inconsistent.StatusCode);
        Assert.Equal("field_submission_snapshot_inconsistent", (await inconsistent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

    }

    private static async Task Login(HttpClient client, string username)
    {
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
    }
    private static async Task<string> Version(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!;
    }
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object input, string? key = null, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", "\"" + version + "\"");
        return client.SendAsync(request);
    }
}
