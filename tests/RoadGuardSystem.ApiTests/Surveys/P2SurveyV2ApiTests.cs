using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Surveys;

[Trait("TaskId", "P2-054/P2-055/P2-011/P2-012")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class P2SurveyV2ApiTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public P2SurveyV2ApiTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task SurveyV2Endpoints_CreateReplayPostponeWithConcurrencyAndReadScope()
    {
        var supervisor = await _sql.CreateUserAsync($"p2_v2_supervisor_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"p2_v2_manager_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var operatorUser = await _sql.CreateUserAsync($"p2_v2_operator_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var otherOperator = await _sql.CreateUserAsync($"p2_v2_other_operator_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString,
            configureTestServices: services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        var routeVersionId = await CreateRoadSectionAsync(client, projectId);
        await AuthenticateAsync(client, manager.UserName!);

        var segmentSetId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        await using (var context = _sql.CreateDbContext())
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ProjectMembers (Id,ProjectId,UserId,RoleCode,IsPrimary,ValidFrom,Status) VALUES ({Guid.NewGuid()},{projectId},{operatorUser.Id},{"DRONE_OPERATOR"},{false},{new DateOnly(2026, 1, 1)},{(byte)ProjectMemberStatus.Active})");
            context.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId, routeVersionId));
            context.RoadSegments.Add(RoadSegment.Create(segmentId, segmentSetId, routeVersionId, 1));
            await context.SaveChangesAsync();
        }
        var scope = new[] { new { routeVersionId, segmentSetId, segmentIds = new[] { segmentId }, targetBand = "SURFACE" } };
        using (var invalidRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans")
        {
            Content = JsonContent.Create(new
            {
                scope = new[] { new { routeVersionId, segmentSetId = Guid.NewGuid(), segmentIds = new[] { Guid.NewGuid() }, targetBand = "SURFACE" } },
                plannedAt = "2026-10-01T08:00:00Z",
                surveyType = "BASELINE"
            })
        })
        {
            invalidRequest.Headers.Add("Idempotency-Key", "p2-plan-invalid-scope-001");
            var invalid = await client.SendAsync(invalidRequest);
            invalid.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        await using (var verification = _sql.CreateDbContext())
        {
            (await verification.SurveyPlans.AsNoTracking().CountAsync(plan => plan.ProjectId == projectId))
                .Should().Be(0);
        }
        var planRequest = new { scope, plannedAt = "2026-10-01T08:00:00Z", surveyType = "BASELINE" };
        using var createPlanRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans") { Content = JsonContent.Create(planRequest) };
        createPlanRequest.Headers.Add("Idempotency-Key", "p2-plan-api-001");
        var createdPlan = await client.SendAsync(createPlanRequest);
        createdPlan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planBody = await createdPlan.Content.ReadFromJsonAsync<JsonElement>();
        var planId = planBody.GetProperty("id").GetGuid();
        var planVersion = planBody.GetProperty("version").GetString()!;
        createdPlan.Headers.ETag!.Tag.Should().Be($"\"{planVersion}\"");

        using var duplicatePlanRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans") { Content = JsonContent.Create(planRequest) };
        duplicatePlanRequest.Headers.Add("Idempotency-Key", "p2-plan-api-duplicate");
        var duplicatePlan = await client.SendAsync(duplicatePlanRequest);
        duplicatePlan.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-plans") { Content = JsonContent.Create(planRequest) };
        replayRequest.Headers.Add("Idempotency-Key", "p2-plan-api-001");
        var replay = await client.SendAsync(replayRequest);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);

        var postponed = await PostponeAsync(client, planId, "p2-postpone-api-001", planVersion);
        postponed.StatusCode.Should().Be(HttpStatusCode.OK);
        var postponedVersion = (await postponed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetString()!;
        postponedVersion.Should().NotBe(planVersion);

        var stale = await PostponeAsync(client, planId, "p2-postpone-api-stale", planVersion);
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);

        using var taskRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-tasks")
        {
            Content = JsonContent.Create(new
            {
                scope,
                surveyType = "BASELINE",
                operatorId = operatorUser.Id,
                dueAt = "2026-10-04T17:00:00Z",
                accessPoint = (object?)null
            })
        };
        taskRequest.Headers.Add("Idempotency-Key", "p2-task-api-001");
        var task = await client.SendAsync(taskRequest);
        task.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskBody = await task.Content.ReadFromJsonAsync<JsonElement>();
        taskBody.GetProperty("status").GetString().Should().Be("NEW_ASSIGNED");
        taskBody.TryGetProperty("dueAt", out _).Should().BeFalse();
        var taskId = taskBody.GetProperty("id").GetGuid();
        var planRow = await CollectionAssertions.Contains(client, $"/api/v1/projects/{projectId}/survey-plans", planId, "id", "projectId", "plannedAt", "status", "version");
        Assert.Equal(postponedVersion, planRow.GetProperty("version").GetString());
        var taskRow = await CollectionAssertions.Contains(client, $"/api/v1/projects/{projectId}/survey-tasks", taskId, "id", "projectId", "operatorId", "status", "version", "supplementTaskId");
        Assert.Equal(operatorUser.Id, taskRow.GetProperty("operatorId").GetGuid());

        var planPath = $"/api/v1/survey-plans/{planId}";
        var taskPath = $"/api/v1/survey-tasks/{taskId}";
        using var webPm = await WebCookieTestClient.Login(factory, manager.Email!);
        using var webSup = await WebCookieTestClient.Login(factory, supervisor.Email!);
        webPm.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        webSup.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var webPlan = await webPm.GetAsync(planPath);
        using var webTask = await webPm.GetAsync(taskPath);
        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.OK }, new[] { webPlan.StatusCode, webTask.StatusCode });
        Assert.Equal(planId, (await webPlan.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        Assert.Equal(taskId, (await webTask.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        Assert.Equal(postponedVersion, webPlan.Headers.ETag!.Tag.Trim('"'));
        Assert.Equal(task.Headers.ETag, webTask.Headers.ETag);

        var outsider = await _sql.CreateUserAsync($"p2_v2_outside_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var reporter = await _sql.CreateUserAsync($"p2_v2_reporter_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        using var webOutside = await WebCookieTestClient.Login(factory, outsider.Email!);
        using var webReporter = await WebCookieTestClient.Login(factory, reporter.Email!);
        using var webOperator = await WebCookieTestClient.Login(factory, operatorUser.Email!);
        using var webOtherOperator = await WebCookieTestClient.Login(factory, otherOperator.Email!);
        foreach (var path in new[] { planPath, taskPath })
        {
            Assert.Equal(HttpStatusCode.OK, (await webSup.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await webOutside.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await webReporter.GetAsync(path)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await webOperator.GetAsync(taskPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await webOperator.GetAsync(planPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await webOtherOperator.GetAsync(taskPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await webPm.GetAsync(taskPath + "/work-package")).StatusCode);
        clock.Now += TimeSpan.FromHours(13);
        Assert.Equal(HttpStatusCode.Unauthorized, (await webPm.GetAsync(planPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await webPm.GetAsync(taskPath)).StatusCode);
        clock.Now -= TimeSpan.FromHours(13);

        var read = await client.GetAsync($"/api/v1/survey-tasks/{taskId}");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        (await read.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("operatorId").GetGuid().Should().Be(operatorUser.Id);

        await AuthenticateAsync(client, otherOperator.UserName!);
        var denied = await client.GetAsync($"/api/v1/survey-tasks/{taskId}");
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await using (var verification = _sql.CreateDbContext())
        {
            (await verification.SurveyPlans.AsNoTracking().CountAsync(plan => plan.ProjectId == projectId))
                .Should().Be(1, "duplicate and replay requests must not create another plan");
            (await verification.SurveyPlanPostponements.AsNoTracking().CountAsync(item => item.SurveyPlanId == planId))
                .Should().Be(1, "stale concurrency requests must not add a postponement");
            (await verification.SurveyRequests.AsNoTracking().CountAsync(item => item.Id == taskId && item.ProjectId == projectId))
                .Should().Be(1);
        }
    }

    private static async Task<HttpResponseMessage> PostponeAsync(HttpClient client, Guid planId, string key, string version)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-plans/{planId}/postpone") { Content = JsonContent.Create(new { reason = "Weather delay" }) };
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return await client.SendAsync(request);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, Guid managerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"P2V2-{Guid.NewGuid():N}",
            name = "P2 V2 API project",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = managerId,
            handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    private static async Task<Guid> CreateRoadSectionAsync(HttpClient client, Guid projectId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new
        {
            code = $"RS-{Guid.NewGuid():N}",
            srid = 32648,
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
            effectiveFrom = "2026-09-21T08:00:00+07:00",
            changeReason = "Initial alignment",
            operationId = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roadSectionVersionId").GetGuid();
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
