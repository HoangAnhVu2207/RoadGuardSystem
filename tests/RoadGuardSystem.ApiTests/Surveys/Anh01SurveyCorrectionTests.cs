using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Surveys;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Anh01SurveyCorrectionTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task DeclinedTaskRemainsManageableButEndedOperatorOnlyReplaysDeclineUntilReassignment()
    {
        var setup = await Setup();
        await using var factory = setup.Factory;
        using var client = setup.Client;
        var task = await CreateTask(client, setup);
        var taskId = (await Body(task)).GetProperty("id").GetGuid();
        var key = Guid.NewGuid().ToString("N");
        var reason = new { reason = "Unable to attend assigned survey" };
        await Login(client, setup.First);
        var decline = await Command(client, $"/api/v1/survey-tasks/{taskId}/decline", reason, task.Headers.ETag!.Tag, key);
        decline.StatusCode.Should().Be(HttpStatusCode.OK);
        await Login(client, setup.Manager);
        var read = await client.GetAsync($"/api/v1/survey-tasks/{taskId}");
        read.StatusCode.Should().Be(HttpStatusCode.OK, "PM manages tasks even after the only assignment ends");
        (await Body(read)).GetProperty("status").GetString().Should().Be("REJECTED");
        await Login(client, setup.First);
        var replay = await Command(client, $"/api/v1/survey-tasks/{taskId}/decline", reason, task.Headers.ETag!.Tag, key);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(replay)).GetProperty("version").GetString().Should().Be((await Body(decline)).GetProperty("version").GetString());
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/decline", new { reason = "Different payload" }, task.Headers.ETag!.Tag, key))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/decline", reason, decline.Headers.ETag!.Tag))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "ended assignment replay eligibility must not reopen the transition for a new key");
        (await client.GetAsync($"/api/v1/survey-tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/accept", null, decline.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var db = sql.CreateDbContext())
        {
            var member = await db.ProjectMembers.SingleAsync(m => m.ProjectId == setup.Project && m.UserId == setup.FirstId);
            member.Status = ProjectMemberStatus.Ended;
            await db.SaveChangesAsync();
        }
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/decline", reason, task.Headers.ETag!.Tag, key)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var db = sql.CreateDbContext())
        {
            var member = await db.ProjectMembers.SingleAsync(m => m.ProjectId == setup.Project && m.UserId == setup.FirstId);
            member.Status = ProjectMemberStatus.Active;
            await db.SaveChangesAsync();
        }
        await Login(client, setup.Manager);
        var reassigned = await Command(client, $"/api/v1/survey-tasks/{taskId}/reassign", new { operatorId = setup.SecondId, reason = "Replacement available" }, read.Headers.ETag!.Tag);
        reassigned.StatusCode.Should().Be(HttpStatusCode.OK);
        await Login(client, setup.First);
        (await client.GetAsync($"/api/v1/survey-tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/decline", reason, task.Headers.ETag!.Tag, key)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Login(client, setup.Second);
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/accept", null, reassigned.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.OK);
        await using var check = sql.CreateDbContext();
        var assignments = await check.SurveyAssignments.Where(a => a.SurveyRequestId == taskId).ToListAsync();
        assignments.Count.Should().Be(2);
        assignments.Single(a => a.EndedAt == null).OperatorUserId.Should().Be(setup.SecondId);
        (await check.IdempotencyRecords.CountAsync(r => r.ActorUserId == setup.FirstId && r.IdempotencyKey == key)).Should().Be(1);
    }

    [Fact]
    public async Task NewWorkRejectsSupersededRouteButAssignedTaskRetainsHistoricalScope()
    {
        var setup = await Setup();
        await using var factory = setup.Factory;
        using var client = setup.Client;
        var task = await CreateTask(client, setup);
        var taskId = (await Body(task)).GetProperty("id").GetGuid();
        await Login(client, setup.Manager);
        var nextDraft = await Command(client, $"/api/v1/projects/{setup.Project}/road-sections/{setup.Section}/geometry-drafts", Draft(setup.Section));
        nextDraft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, setup.Supervisor);
        var version2 = await Command(client, nextDraft.Headers.Location!.OriginalString + "/confirm",
            new { expectedCurrentVersionId = setup.Route, effectiveFrom = "2026-10-03T00:00:00Z", reason = "New alignment" }, nextDraft.Headers.ETag!.Tag);
        version2.StatusCode.Should().Be(HttpStatusCode.Created);
        var newRoute = (await Body(version2)).GetProperty("routeVersionId").GetGuid();
        await Login(client, setup.Manager);
        await Publish(client, setup.Project, setup.Section, newRoute);
        await CreateTask(client, setup, HttpStatusCode.Conflict);
        (await Command(client, $"/api/v1/projects/{setup.Project}/survey-plans", new { scope = Scope(setup), plannedAt = "2026-10-04T08:00:00Z", surveyType = "BASELINE" }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.GetAsync($"/api/v1/survey-tasks/{taskId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        await Login(client, setup.First);
        var work = await client.GetAsync($"/api/v1/survey-tasks/{taskId}/work-package");
        work.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(work)).GetProperty("geometryRefs")[0].GetProperty("routeVersionId").GetGuid().Should().Be(setup.Route);
        (await Command(client, $"/api/v1/survey-tasks/{taskId}/accept", null, task.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.OK);
        await using var check = sql.CreateDbContext();
        (await check.SurveyRequests.CountAsync(t => t.ProjectId == setup.Project)).Should().Be(1);
        (await check.SurveyPlans.CountAsync(t => t.ProjectId == setup.Project)).Should().Be(0);
    }

    private async Task<SetupData> Setup()
    {
        var supervisor = await sql.CreateUserAsync($"fix-super-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"fix-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var first = await sql.CreateUserAsync($"fix-op-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var second = await sql.CreateUserAsync($"fix-op2-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var projectResponse = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = $"FIX-{Guid.NewGuid():N}", name = "Isolated correction test",
            engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "TEST", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
        projectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await Body(projectResponse)).GetProperty("projectId").GetGuid();
        await Login(client, manager.UserName!);
        var draft = await Command(client, $"/api/v1/projects/{project}/road-geometry-drafts", Draft());
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var confirmed = await Command(client, draft.Headers.Location!.OriginalString + "/confirm",
            new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = "2026-10-02T00:00:00Z", reason = "Test geometry" }, draft.Headers.ETag!.Tag);
        confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
        var route = (await Body(confirmed)).GetProperty("routeVersionId").GetGuid();
        var section = (await Body(confirmed)).GetProperty("roadSectionId").GetGuid();
        await Login(client, manager.UserName!);
        var (set, segment) = await Publish(client, project, section, route);
        await using (var db = sql.CreateDbContext())
        {
            foreach (var actor in new[] { first.Id, second.Id }) db.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = actor,
                RoleCode = UserRoleCode.DroneOperator, ValidFrom = new DateOnly(2026, 1, 1), Status = ProjectMemberStatus.Active });
            await db.SaveChangesAsync();
        }
        return new(factory, client, project, section, route, set, segment, supervisor.UserName!, manager.UserName!, first.UserName!, second.UserName!, first.Id, second.Id);
    }
    private static object Draft(Guid? section = null) => new { sourceKind = "COORDINATES", sourceCrs = 32648, roadSectionId = section, roadCode = section is null ? "TEST" : null,
        stationOriginMeters = 0d, changeReason = "Test alignment", coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
        widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } }, surveyWidthMeters = 12d };
    private static object[] Scope(SetupData s) => [new { routeVersionId = s.Route, segmentSetId = s.Set, segmentIds = new[] { s.Segment }, targetBand = "SURFACE" }];
    private static async Task<HttpResponseMessage> CreateTask(HttpClient c, SetupData s, HttpStatusCode expected = HttpStatusCode.Created)
    {
        await Login(c, s.Manager);
        var response = await Command(c, $"/api/v1/projects/{s.Project}/survey-tasks", new { scope = Scope(s), surveyType = "BASELINE", operatorId = s.FirstId, dueAt = "2026-10-06T08:00:00Z" });
        response.StatusCode.Should().Be(expected);
        return response;
    }
    private static async Task<(Guid Set, Guid Segment)> Publish(HttpClient c, Guid project, Guid section, Guid route)
    {
        var path = $"/api/v1/projects/{project}/road-sections/{section}/versions/{route}/segment-sets";
        var created = await Command(c, path, new { targetLengthMeters = 100d, remainderMode = "KEEP" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await Body(created);
        var id = body.GetProperty("id").GetGuid();
        (await Command(c, path + $"/{id}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "Test publish" }, created.Headers.ETag!.Tag)).StatusCode.Should().Be(HttpStatusCode.OK);
        return (id, body.GetProperty("segments")[0].GetProperty("id").GetGuid());
    }
    private static async Task Login(HttpClient c, string name)
    {
        var response = await c.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(name), password = "Current1!" });
        response.EnsureSuccessStatusCode();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(response)).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Command(HttpClient c, string url, object? body, string? etag = null, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await c.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage r) { using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return d.RootElement.Clone(); }
    private sealed record SetupData(AuthenticationWebApplicationFactory Factory, HttpClient Client, Guid Project, Guid Section, Guid Route, Guid Set, Guid Segment,
        string Supervisor, string Manager, string First, string Second, Guid FirstId, Guid SecondId);
}
