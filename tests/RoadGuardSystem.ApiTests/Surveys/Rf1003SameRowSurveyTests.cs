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

[Trait("TaskId", "RF-10-03-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1003SameRowSurveyTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1003SameRowSurveyTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task OldPlan_V2PostponeAndReplay_RejectIncompatibleScopeBeforeWriting()
    {
        await using var scenario = await Scenario.CreateAsync(_sql);
        var operationId = Guid.NewGuid();
        var oldBody = new
        {
            plannedStartAt = "2026-10-01T08:00:00Z",
            plannedEndAt = "2026-10-01T10:00:00Z",
            roadSectionVersionId = scenario.VersionId,
            surveyType = 2,
            outputRequirements = "{\"formats\":[\"video\"]}",
            operationId
        };
        var oldRoute = $"/api/v1/projects/{scenario.ProjectId}/road-sections/{scenario.RoadSectionId}/survey-plans";
        var created = await scenario.Client.PostAsJsonAsync(oldRoute, oldBody);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdJson = await created.Content.ReadFromJsonAsync<JsonElement>();
        var planId = createdJson.GetProperty("planId").GetGuid();
        var before = await ReadAsync(scenario, planId);
        before.PlanCount.Should().Be(1);
        before.PlanId.Should().Be(planId);
        before.ProjectId.Should().Be(scenario.ProjectId);
        before.Start.Should().Be(DateTimeOffset.Parse("2026-10-01T08:00:00Z"));
        before.End.Should().Be(DateTimeOffset.Parse("2026-10-01T10:00:00Z"));
        before.ScopeCount.Should().Be(0);
        before.RequestCount.Should().Be(0);
        before.Postponements.Should().BeEmpty();
        before.Audits.Should().ContainSingle(value => value.Contains("survey_plan_created"));
        before.Receipts.Should().ContainSingle(value => value.Contains($"SurveyPlanCreated|{operationId:N}"));

        await scenario.AuthenticateOtherManagerAsync();
        var denied = await PostponeV2Async(scenario.Client, planId, "wrong-actor", before.Version);
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadAsync(scenario, planId)).Should().BeEquivalentTo(before);
        await scenario.AuthenticateManagerAsync();

        var sharedKey = Guid.NewGuid();
        var current = await PostponeV2Async(scenario.Client, planId, sharedKey.ToString("N"), before.Version);
        current.StatusCode.Should().Be(HttpStatusCode.Conflict);
        current.Headers.ETag.Should().BeNull();
        (await current.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()
            .Should().Be("survey_scope_incompatible");
        var after = await ReadAsync(scenario, planId);
        after.Should().BeEquivalentTo(before, "incompatible scopes fail closed before mutation, audit or receipt");

        var replayOld = await scenario.Client.PostAsJsonAsync(oldRoute, oldBody);
        replayOld.StatusCode.Should().Be(HttpStatusCode.Created);
        (await replayOld.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("planId").GetGuid().Should().Be(planId);
        (await ReadAsync(scenario, planId)).Should().BeEquivalentTo(after);

        var replay = await PostponeV2Async(scenario.Client, planId, sharedKey.ToString("N"), before.Version);
        replay.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadAsync(scenario, planId)).Should().BeEquivalentTo(after);

        var stale = await PostponeV2Async(scenario.Client, planId, "stale-after-old", before.Version);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var staleState = await ReadAsync(scenario, planId);
        staleState.PlanId.Should().Be(after.PlanId);
        staleState.Version.Should().Be(after.Version);
        staleState.Postponements.Should().Equal(after.Postponements);
        staleState.Audits.Should().Equal(after.Audits);
        staleState.OutboxIds.Should().Equal(after.OutboxIds);
        staleState.Receipts.Should().Equal(after.Receipts, "scope compatibility is checked before idempotency or version mutation");
    }

    [Fact]
    public async Task V2Plan_OldPostponeAndReplay_ChangeTheSameRowAndInvalidateOldVersion()
    {
        await using var scenario = await Scenario.CreateAsync(_sql);
        var sharedKey = Guid.NewGuid();
        var v2Body = new
        {
            scope = new[] { new { routeVersionId = scenario.VersionId, segmentSetId = scenario.SegmentSetId, segmentIds = new[] { scenario.SegmentId }, targetBand = "SURFACE" } },
            plannedAt = "2026-10-01T08:00:00Z",
            surveyType = "BASELINE"
        };
        var createRoute = $"/api/v1/projects/{scenario.ProjectId}/survey-plans";
        var created = await PostV2Async(scenario.Client, createRoute, sharedKey.ToString("N"), v2Body);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdJson = await created.Content.ReadFromJsonAsync<JsonElement>();
        var planId = createdJson.GetProperty("id").GetGuid();
        var before = await ReadAsync(scenario, planId);
        created.Headers.ETag!.Tag.Should().Be($"\"{before.Version}\"");
        before.PlanCount.Should().Be(1);
        before.PlanId.Should().Be(planId);
        before.ProjectId.Should().Be(scenario.ProjectId);
        before.Start.Should().Be(DateTimeOffset.Parse("2026-10-01T08:00:00Z"));
        before.End.Should().Be(DateTimeOffset.Parse("2026-10-01T09:00:00Z"));
        before.ScopeCount.Should().Be(1);
        before.RequestCount.Should().Be(0);
        before.Postponements.Should().BeEmpty();
        before.Receipts.Should().ContainSingle(value => value.Contains($"SurveyPlanV2Created|{sharedKey:N}"));

        var oldRoute = $"/api/v1/projects/{scenario.ProjectId}/survey-plans/{planId}/postpone";
        var oldBody = new { newPlannedStartAt = "2026-10-01T08:30:00Z", reason = "Weather delay", operationId = sharedKey };
        var postponed = await scenario.Client.PostAsJsonAsync(oldRoute, oldBody);
        postponed.StatusCode.Should().Be(HttpStatusCode.OK);
        postponed.Headers.ETag.Should().BeNull();
        (await postponed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("planId").GetGuid().Should().Be(planId);
        var after = await ReadAsync(scenario, planId);
        after.PlanCount.Should().Be(1);
        after.PlanId.Should().Be(planId);
        after.Start.Should().Be(DateTimeOffset.Parse("2026-10-01T08:30:00Z"));
        after.End.Should().Be(before.End);
        after.Status.Should().Be(SurveyPlanStatus.Postponed);
        after.Version.Should().NotBe(before.Version);
        after.ScopeCount.Should().Be(1);
        after.Postponements.Should().ContainSingle(value => value.Contains("Weather delay") && value.EndsWith("|2026-10-01T08:30:00.0000000+00:00"));
        after.Audits.Should().HaveCount(before.Audits.Length + 1);
        after.Receipts.Should().Contain(value => value.Contains($"SurveyPlanPostponed|{sharedKey:N}"));
        after.Receipts.Should().Contain(value => value.Contains($"SurveyPlanV2Created|{sharedKey:N}"));
        after.OutboxIds.Should().Equal(before.OutboxIds);

        var replayOld = await scenario.Client.PostAsJsonAsync(oldRoute, oldBody);
        replayOld.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(scenario, planId)).Should().BeEquivalentTo(after);

        var replayV2 = await PostV2Async(scenario.Client, createRoute, sharedKey.ToString("N"), v2Body);
        replayV2.StatusCode.Should().Be(HttpStatusCode.Created);
        replayV2.Headers.ETag!.Tag.Should().Be(created.Headers.ETag!.Tag, "the create replay returns the stored response version");
        (await ReadAsync(scenario, planId)).Should().BeEquivalentTo(after);

        var stale = await PostponeV2Async(scenario.Client, planId, "v2-stale-after-old", before.Version);
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var staleState = await ReadAsync(scenario, planId);
        staleState.Version.Should().Be(after.Version);
        staleState.Postponements.Should().Equal(after.Postponements);
        staleState.Audits.Should().Equal(after.Audits);
        staleState.OutboxIds.Should().Equal(after.OutboxIds);
        staleState.Receipts.Should().HaveCount(after.Receipts.Length + 1);

        var current = await PostponeV2Async(scenario.Client, planId, sharedKey.ToString("N"), after.Version);
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        var final = await ReadAsync(scenario, planId);
        final.PlanId.Should().Be(planId);
        final.Start.Should().Be(after.Start, "V2 postpone passes null for the new start");
        final.End.Should().Be(after.End);
        final.Version.Should().Be(after.Version, "V2 postpones an already-postponed plan without changing its plan fields");
        final.Postponements.Should().HaveCount(after.Postponements.Length + 1);
        final.Audits.Should().HaveCount(after.Audits.Length + 1);
        final.Receipts.Should().Contain(value => value.Contains($"SurveyPlanV2Postponed|{sharedKey:N}"));
        final.Receipts.Count(value => value.Contains($"|{sharedKey:N}|")).Should().Be(3,
            "V2 create, old postpone and V2 postpone keep separate operation receipts for the same key text");
        final.OutboxIds.Should().Equal(after.OutboxIds);
    }

    private async Task<SurveyState> ReadAsync(Scenario scenario, Guid planId)
    {
        var projectId = scenario.ProjectId;
        await using var context = _sql.CreateDbContext();
        var plan = await context.SurveyPlans.AsNoTracking().SingleAsync(value => value.Id == planId);
        var postponements = await context.SurveyPlanPostponements.AsNoTracking()
            .Where(value => value.SurveyPlanId == planId).OrderBy(value => value.Id)
            .Select(value => new { value.Id, value.PostponedAt, value.Reason, value.NewPlannedStartAt }).ToListAsync();
        var scopes = await context.SurveyPlanScopes.AsNoTracking()
            .Where(value => value.SurveyPlanId == planId).OrderBy(value => value.Id)
            .Select(value => new { value.Id, value.RouteSectionVersionId, value.SegmentSetId, value.SegmentIdsJson, value.TargetBand })
            .ToListAsync();
        var audits = await context.AuditLogs.AsNoTracking()
            .Where(value => value.EntityType == "SurveyPlan" && value.EntityId == planId)
            .OrderBy(value => value.Id).Select(value => new { value.Id, value.EventType, value.Source }).ToListAsync();
        var receipts = await context.IdempotencyRecords.AsNoTracking()
            .Where(value => value.ActorUserId == scenario.ManagerId &&
                ((value.ProjectId == projectId && (value.Operation == "SurveyPlanCreated" || value.Operation == "SurveyPlanV2Created" || value.Operation == "SurveyPlanPostponed")) ||
                 (value.ProjectId == null && value.Operation == "SurveyPlanV2Postponed")))
            .OrderBy(value => value.Id).Select(value => new { value.Id, value.Operation, value.IdempotencyKey, value.ProjectId }).ToListAsync();
        var outboxIds = await context.OutboxMessages.AsNoTracking().OrderBy(value => value.Id).Select(value => value.Id).ToArrayAsync();
        return new SurveyState(
            await context.SurveyPlans.AsNoTracking().CountAsync(value => value.ProjectId == projectId),
            plan.Id, plan.ProjectId, plan.RoadSectionId, plan.RoadSectionVersionId,
            plan.PlannedStartAt, plan.PlannedEndAt, plan.SurveyType, plan.Status,
            plan.OutputRequirements, Convert.ToBase64String(plan.RowVersion),
            scopes.Count,
            scopes.Select(value => $"{value.Id:N}|{value.RouteSectionVersionId:N}|{value.SegmentSetId:N}|{value.SegmentIdsJson}|{value.TargetBand}").ToArray(),
            await context.SurveyRequests.AsNoTracking().CountAsync(value => value.ProjectId == projectId),
            postponements.Select(value => $"{value.Id:N}|{value.PostponedAt:O}|{value.Reason}|{value.NewPlannedStartAt?.ToString("O") ?? "<null>"}").ToArray(),
            audits.Select(value => $"{value.Id:N}|{value.EventType}|{value.Source}").ToArray(),
            receipts.Select(value => $"{value.Id:N}|{value.Operation}|{value.IdempotencyKey}|{value.ProjectId}").ToArray(),
            outboxIds);
    }

    private static async Task<HttpResponseMessage> PostponeV2Async(HttpClient client, Guid planId, string key, string version)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-plans/{planId}/postpone")
        {
            Content = JsonContent.Create(new { reason = "Weather delay" })
        };
        request.Headers.Add("Idempotency-Key", key);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostV2Async(HttpClient client, string route, string key, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private sealed record SurveyState(
        int PlanCount, Guid PlanId, Guid ProjectId, Guid RoadSectionId, Guid? RoadSectionVersionId,
        DateTimeOffset Start, DateTimeOffset End, SurveyType Type, SurveyPlanStatus Status,
        string OutputRequirements, string Version, int ScopeCount, string[] ScopeRows, int RequestCount,
        string[] Postponements, string[] Audits, string[] Receipts, Guid[] OutboxIds);

    private sealed class Scenario : IAsyncDisposable
    {
        private readonly AuthenticationWebApplicationFactory _factory;
        private readonly string _managerName;
        private readonly string _otherManagerName;

        private Scenario(AuthenticationWebApplicationFactory factory, HttpClient client, string managerName, string otherManagerName)
        {
            _factory = factory;
            Client = client;
            _managerName = managerName;
            _otherManagerName = otherManagerName;
        }

        public HttpClient Client { get; }
        public Guid ManagerId { get; private set; }
        public Guid ProjectId { get; private set; }
        public Guid RoadSectionId { get; private set; }
        public Guid VersionId { get; private set; }
        public Guid SegmentSetId { get; private set; }
        public Guid SegmentId { get; private set; }

        public static async Task<Scenario> CreateAsync(AuthenticationSqlServerFixture sql)
        {
            var supervisor = await sql.CreateUserAsync($"rf1003_sup_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
            var manager = await sql.CreateUserAsync($"rf1003_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
            var other = await sql.CreateUserAsync($"rf1003_other_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
            var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
            var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            var scenario = new Scenario(factory, client, manager.UserName!, other.UserName!);
            scenario.ManagerId = manager.Id;
            await scenario.AuthenticateAsync(supervisor.UserName!);
            var project = await client.PostAsJsonAsync("/api/v1/projects", new
            {
                projectCode = $"RF1003-{Guid.NewGuid():N}",
                name = "RF-10-03 same-row test",
                engineeringUtmSrid = 32648,
                startDate = "2026-09-01",
                endDate = "2027-09-01",
                primaryProjectManagerUserId = manager.Id,
                handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" },
                operationId = Guid.NewGuid()
            });
            project.StatusCode.Should().Be(HttpStatusCode.Created);
            scenario.ProjectId = (await project.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
            var road = await client.PostAsJsonAsync($"/api/v1/projects/{scenario.ProjectId}/road-sections", new
            {
                code = $"RS-{Guid.NewGuid():N}",
                srid = 32648,
                coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } },
                effectiveFrom = "2026-09-21T08:00:00+07:00",
                changeReason = "Initial alignment",
                operationId = Guid.NewGuid()
            });
            road.StatusCode.Should().Be(HttpStatusCode.Created);
            var roadJson = await road.Content.ReadFromJsonAsync<JsonElement>();
            scenario.RoadSectionId = roadJson.GetProperty("roadSectionId").GetGuid();
            scenario.VersionId = roadJson.GetProperty("roadSectionVersionId").GetGuid();
            scenario.SegmentSetId = Guid.NewGuid();
            scenario.SegmentId = Guid.NewGuid();
            await using (var context = sql.CreateDbContext())
            {
                context.RoadSegmentSets.Add(RoadSegmentSet.Create(scenario.SegmentSetId, scenario.VersionId));
                context.RoadSegments.Add(RoadSegment.Create(scenario.SegmentId, scenario.SegmentSetId, scenario.VersionId, 1));
                await context.SaveChangesAsync();
            }
            await scenario.AuthenticateManagerAsync();
            return scenario;
        }

        public Task AuthenticateManagerAsync() => AuthenticateAsync(_managerName);
        public Task AuthenticateOtherManagerAsync() => AuthenticateAsync(_otherManagerName);

        private async Task AuthenticateAsync(string username)
        {
            var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                email = AuthenticationSqlServerFixture.EmailFor(username),
                password = "Current1!"
            });
            login.EnsureSuccessStatusCode();
            var json = await login.Content.ReadFromJsonAsync<JsonElement>();
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }
    }
}
