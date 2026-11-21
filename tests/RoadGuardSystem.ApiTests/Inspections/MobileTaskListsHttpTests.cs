using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;
namespace RoadGuardSystem.ApiTests.Inspections;

public sealed class MobileTaskListsHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task CrewFiltersBeforePagingAndRejectedHistoryDoesNotGrantCurrentTaskAccess()
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        var now = DateTimeOffset.UtcNow; var tasks = new List<FieldInspectionTask>();
        for (var i = 0; i < 7; i++)
        {
            var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), $"MOBILE-{i}", source.Project, source.Defect, null, "REPORTER", source.Route, source.Set, null, null,
                FieldInspectionPurpose.PreMeasurement, 1, "{}", null, now.AddDays(1), source.Pm);
            db.Add(task); tasks.Add(task);
            db.Add(FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, source.Crew, source.Pm, now, null, FieldInspectionAssignmentStatus.Active, null));
        }
        await db.SaveChangesAsync();
        using var logs = new InMemoryLogProvider(); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: logs);
        using var crew = await Login(factory, source.Crew);
        async Task<JsonElement> Read(string path)
        {
            using var response = await crew.GetAsync(path); var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, text + logs.CombinedEntries); return JsonDocument.Parse(text).RootElement.Clone();
        }
        const string root = "/api/v1/me/inspection-tasks";
        var first = await Read(root + "?limit=2"); Assert.Equal(2, first.GetProperty("items").GetArrayLength());
        var cursor = first.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
        var firstTaskIds = first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToHashSet();
        var laterTask = tasks.First(x => !firstTaskIds.Contains(x.Id));
        var found = await Read(root + "?limit=2&mode=MEASURE_ONLY&status=NEWASSIGNED&acceptance=PENDING&search=" + laterTask.TaskCode.ToLowerInvariant());
        Assert.Equal(laterTask.Id, Assert.Single(found.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal("MEASURE_ONLY", found.GetProperty("items")[0].GetProperty("mode").GetString());
        var ids = new HashSet<Guid>(); var page = first;
        while (true)
        {
            foreach (var item in page.GetProperty("items").EnumerateArray()) Assert.True(ids.Add(item.GetProperty("assignmentId").GetGuid()));
            var next = page.GetProperty("nextCursor").GetString(); if (next is null) break;
            page = await Read(root + "?limit=2&cursor=" + Uri.EscapeDataString(next));
        }
        Assert.Equal(7, ids.Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await crew.GetAsync(root + "?limit=2&search=other&cursor=" + Uri.EscapeDataString(cursor!))).StatusCode);
        using var other = await Login(factory, source.Pm);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(root)).StatusCode);
        using var taskDetail = await crew.GetAsync($"/api/v1/projects/{source.Project}/field-inspection-tasks/{tasks[1].Id}");
        Assert.Equal(HttpStatusCode.OK, taskDetail.StatusCode);
        using var accept = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{source.Project}/field-inspection-tasks/{tasks[1].Id}/accept") { Content = JsonContent.Create(new { reason = "Accept current assignment" }) };
        accept.Headers.Add("Idempotency-Key", "mobile-accept"); accept.Headers.Add("If-Match", taskDetail.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.Created, (await crew.SendAsync(accept)).StatusCode);
        var accepted = await Read(root + "?acceptance=ACCEPTED&status=ACCEPTED&mode=MEASURE_ONLY&search=MOBILE-1");
        var acceptedItem = Assert.Single(accepted.GetProperty("items").EnumerateArray());
        Assert.Equal("ACCEPTED", acceptedItem.GetProperty("acceptanceOutcome").GetString());
        Assert.Equal(JsonValueKind.String, acceptedItem.GetProperty("acceptedAt").ValueKind);
        var assignment = await db.FieldInspectionAssignments.SingleAsync(x => x.FieldInspectionTaskId == tasks[0].Id);
        assignment.End(now.AddMinutes(1), "Crew declined", true); await db.SaveChangesAsync();
        var replacementCrew = await sql.CreateUserAsync("mobile-replacement-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.RepairCrew);
        db.Add(new RoadGuardSystem.BusinessObjects.Projects.ProjectMember { Id = Guid.NewGuid(), UserId = replacementCrew.Id, ProjectId = source.Project, RoleCode = UserRoleCode.RepairCrew, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) });
        db.Add(FieldInspectionAssignment.Create(Guid.NewGuid(), tasks[0].Id, replacementCrew.Id, source.Pm, now.AddMinutes(2), null, FieldInspectionAssignmentStatus.Active, null));
        await db.SaveChangesAsync();
        using var replacementClient = await Login(factory, replacementCrew.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await replacementClient.GetAsync(root + "?limit=2&cursor=" + Uri.EscapeDataString(cursor!))).StatusCode);
        var replacementHistory = await replacementClient.GetFromJsonAsync<JsonElement>(root + "?acceptance=REJECTED");
        Assert.Empty(replacementHistory.GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await crew.GetAsync(root + "?mode=FAST_TRACK")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await crew.GetAsync(root + "?search=" + new string('a', 101))).StatusCode);
        var history = await Read(root + "?acceptance=REJECTED"); var old = Assert.Single(history.GetProperty("items").EnumerateArray());
        Assert.Equal(assignment.Id, old.GetProperty("assignmentId").GetGuid()); Assert.Equal("REJECTED", old.GetProperty("acceptanceOutcome").GetString());
        Assert.True(old.GetProperty("isHistory").GetBoolean()); Assert.Equal(JsonValueKind.Null, old.GetProperty("status").ValueKind);
        Assert.Equal(HttpStatusCode.Forbidden, (await crew.GetAsync($"/api/v1/projects/{source.Project}/field-inspection-tasks/{tasks[0].Id}")).StatusCode);
        await db.ProjectMembers.Where(x => x.ProjectId == source.Project && x.UserId == source.Crew).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ProjectMemberStatus.Ended));
        Assert.Empty((await Read(root + "?acceptance=REJECTED")).GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task OperatorFiltersAndSearchApplyBeforePagingAndCursorIsBoundToActorAndQuery()
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        var op = await sql.CreateUserAsync("mobile-op-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var other = await sql.CreateUserAsync("mobile-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        db.Add(new RoadGuardSystem.BusinessObjects.Projects.ProjectMember { Id = Guid.NewGuid(), UserId = op.Id, ProjectId = source.Project, RoleCode = UserRoleCode.DroneOperator, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) });
        var now = DateTimeOffset.UtcNow; var tasks = new List<SurveyRequest>();
        for (var i = 0; i < 7; i++)
        {
            var scope = JsonSerializer.Serialize(new { scope = new[] { new { routeVersionId = source.Route, segmentSetId = source.Set, segmentIds = new[] { await db.RoadSegments.Where(x => x.SegmentSetId == source.Set).Select(x => x.Id).SingleAsync() }, targetBand = "SURFACE" } } });
            var task = SurveyRequest.Create(Guid.NewGuid(), source.Project, source.Road, null, source.Pm, SurveyType.Periodic, i == 6 ? SurveyRequestStatus.Submitted : SurveyRequestStatus.NewAssigned, now, now.AddDays(1), scope, source.Route);
            task.SetBandScope(); tasks.Add(task); db.Add(task); db.Add(SurveyAssignment.Create(Guid.NewGuid(), task.Id, op.Id, source.Pm, now, null, null, null, null, null));
        }
        await db.SaveChangesAsync();
        using var logs = new InMemoryLogProvider(); await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: logs);
        using var client = await Login(factory, op.Id); using var second = await Login(factory, other.Id);
        const string root = "/api/v1/me/survey-tasks";
        async Task<JsonElement> Read(string path)
        {
            using var response = await client.GetAsync(path); var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, text + logs.CombinedEntries); return JsonDocument.Parse(text).RootElement.Clone();
        }
        var page = await Read(root + "?limit=2"); var cursor = page.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
        var firstTaskIds = page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToHashSet();
        var laterTask = tasks.Take(6).First(x => !firstTaskIds.Contains(x.Id));
        var laterMatch = await Read(root + "?limit=2&statusGroup=WAITING&search=" + laterTask.Id);
        Assert.Equal(laterTask.Id, Assert.Single(laterMatch.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + "?statusGroup=APPROVED")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + "?limit=0")).StatusCode);
        var selected = await Read(root + "?limit=2&statusGroup=SUBMITTED&search=" + tasks[6].Id);
        Assert.Equal(tasks[6].Id, Assert.Single(selected.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal("SUBMITTED", selected.GetProperty("items")[0].GetProperty("status").GetString());
        var ids = new HashSet<Guid>();
        while (true)
        {
            foreach (var row in page.GetProperty("items").EnumerateArray()) Assert.True(ids.Add(row.GetProperty("id").GetGuid()));
            var next = page.GetProperty("nextCursor").GetString(); if (next is null) break;
            page = await Read(root + "?limit=2&cursor=" + Uri.EscapeDataString(next));
        }
        Assert.Equal(7, ids.Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await second.GetAsync(root + "?limit=2&cursor=" + Uri.EscapeDataString(cursor!))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(root + "?limit=2&statusGroup=OTHER&cursor=" + Uri.EscapeDataString(cursor!))).StatusCode);
        var survey = Survey.Create(Guid.NewGuid(), tasks[6].Id, source.Project, source.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Pending, null, null, "[]");
        var secondDataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 2, SurveyDataVersionStatus.Draft, SurveyDataIntegrityStatus.Passed, null, null, "[]");
        var block = RoadGuardSystem.BusinessObjects.Processing.ProcessingBlock.Create(Guid.NewGuid(), dataset.Id, 1, "{}");
        var model = RoadGuardSystem.BusinessObjects.Processing.AIModelVersion.Create(Guid.NewGuid(), "fixture", Guid.NewGuid().ToString("N"), "fixture://local", null, null, AIModelVersionStatus.Draft, null, null);
        db.AddRange(survey, dataset, secondDataset, block, model);
        db.Add(RoadGuardSystem.BusinessObjects.Processing.ProcessingJob.Create(Guid.NewGuid(), block.Id, model.Id, ProcessingJobStatus.Queued, null, null, null, null));
        await db.SaveChangesAsync();
        var datasets = await Read($"/api/v1/survey-tasks/{tasks[6].Id}/datasets");
        Assert.Equal(2, datasets.GetProperty("items").GetArrayLength());
        var selectedDataset = datasets.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == dataset.Id);
        Assert.Equal(1, selectedDataset.GetProperty("processingCounts").GetProperty("QUEUED").GetInt32());
        Assert.Equal("PENDING", selectedDataset.GetProperty("integrityStatus").GetString());
        var noJobs = datasets.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == secondDataset.Id);
        Assert.Empty(noJobs.GetProperty("processingCounts").EnumerateObject());
        await db.SurveyAssignments.Where(x => x.SurveyRequestId == tasks[6].Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, now.AddMinutes(1)));
        Assert.Empty((await Read(root + "?status=SUBMITTED")).GetProperty("items").EnumerateArray());
    }
    [Fact]
    public async Task SurveyAssignmentOutboxDeliversOnceAndInboxRechecksAssignment()
    {
        await using var db = sql.CreateDbContext(); var source = await GenuineProtectedSource.Seed(db, sql);
        var op = await sql.CreateUserAsync("mobile-notify-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        db.Add(new RoadGuardSystem.BusinessObjects.Projects.ProjectMember { Id = Guid.NewGuid(), UserId = op.Id, ProjectId = source.Project, RoleCode = UserRoleCode.DroneOperator, Status = ProjectMemberStatus.Active, ValidFrom = new(2000, 1, 1) });
        await db.SaveChangesAsync();
        using var notificationLogs = new InMemoryLogProvider();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, loggerProvider: notificationLogs);
        using var pm = await Login(factory, source.Pm); using var client = await Login(factory, op.Id);
        var segment = await db.RoadSegments.Where(x => x.SegmentSetId == source.Set).Select(x => x.Id).SingleAsync();
        var input = new { scope = new[] { new { routeVersionId = source.Route, segmentSetId = source.Set, segmentIds = new[] { segment }, targetBand = "SURFACE" } }, surveyType = "BASELINE", operatorId = op.Id, dueAt = DateTimeOffset.UtcNow.AddDays(2) };
        var key = Guid.NewGuid().ToString("N");
        async Task<Guid> Create()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{source.Project}/survey-tasks") { Content = JsonContent.Create(input) };
            request.Headers.Add("Idempotency-Key", key);
            using var response = await pm.SendAsync(request); var text = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode); return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
        }
        var task = await Create(); Assert.Equal(task, await Create());
        var messages = await db.OutboxMessages.AsNoTracking().Where(x => x.MessageType == "survey.task.assigned.v1").ToListAsync();
        Assert.Single(messages.Where(x => x.PayloadJson.Contains(task.ToString(), StringComparison.Ordinal)));
        // Drain through the production fenced consumer, including any earlier fixture envelopes.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dispatch = new H6NotificationDispatchService(scope.ServiceProvider.GetRequiredService<IH6NotificationDispatchRepository>());
            for (var i = 0; i < 30; i++) { var result = await dispatch.ProcessOneAsync(); if (result.Status == "IDLE") break; }
        }
        var notifications = await db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == op.Id && x.SourceEntityId == task).ToListAsync();
        var notification = Assert.Single(notifications);
        Assert.Equal("SurveyTask", notification.SourceEntityType);
        var path = $"/api/v1/notifications/{notification.Id}";
        using var detail = await client.GetAsync(path); Assert.True(detail.StatusCode == HttpStatusCode.OK, await detail.Content.ReadAsStringAsync() + notificationLogs.CombinedEntries);
        using var read = new HttpRequestMessage(HttpMethod.Post, path + "/read"); read.Headers.Add("Idempotency-Key", "mobile-read"); read.Headers.Add("If-Match", detail.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(read)).StatusCode);
        using var replayRead = new HttpRequestMessage(HttpMethod.Post, path + "/read"); replayRead.Headers.Add("Idempotency-Key", "mobile-read"); replayRead.Headers.Add("If-Match", detail.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(replayRead)).StatusCode);
        var protectedScope = await client.GetFromJsonAsync<JsonElement>(path + "/scope");
        Assert.Equal("survey.task.assigned.v1", protectedScope.GetProperty("eventCode").GetString());
        Assert.Equal(notification.EventType, protectedScope.GetProperty("eventType").GetString());
        Assert.Equal(task, protectedScope.GetProperty("sourceId").GetGuid());
        Assert.Equal(task, protectedScope.GetProperty("taskId").GetGuid());
        Assert.Equal(source.Project, protectedScope.GetProperty("taskProjectId").GetGuid());
        Assert.Equal(JsonValueKind.Null, protectedScope.GetProperty("datasetId").ValueKind);
        Assert.Equal(JsonValueKind.Null, protectedScope.GetProperty("assessmentId").ValueKind);
        Assert.NotNull(await db.Notifications.Where(x => x.Id == notification.Id).Select(x => x.ReadAt).SingleAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/survey-tasks/{task}")).StatusCode);
        await db.Users.Where(x => x.Id == op.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleCode, UserRoleCode.RepairCrew));
        await db.ProjectMembers.Where(x => x.UserId == op.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleCode, UserRoleCode.RepairCrew));
        try
        {
            using var changedRole = await Login(factory, op.Id);
            Assert.Equal(HttpStatusCode.NotFound, (await changedRole.GetAsync(path)).StatusCode);
        }
        finally
        {
            await db.Users.Where(x => x.Id == op.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleCode, UserRoleCode.DroneOperator));
            await db.ProjectMembers.Where(x => x.UserId == op.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RoleCode, UserRoleCode.DroneOperator));
        }
        await db.SurveyAssignments.Where(x => x.SurveyRequestId == task).ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/v1/survey-tasks/{task}")).StatusCode);
    }

    [Fact]
    public async Task SwaggerPublishesQueryFiltersAndIndependentSummaries()
    {
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        var api = factory.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        var crew = api.Paths["/api/v1/me/inspection-tasks"].Operations[Microsoft.OpenApi.Models.OperationType.Get];
        foreach (var query in new[] { "status", "statusGroup", "mode", "search", "acceptance", "cursor", "limit" }) Assert.Contains(crew.Parameters, p => p.Name == query);
        var op = api.Paths["/api/v1/me/survey-tasks"].Operations[Microsoft.OpenApi.Models.OperationType.Get];
        foreach (var query in new[] { "status", "statusGroup", "search", "cursor", "limit" }) Assert.Contains(op.Parameters, p => p.Name == query);
        Assert.True(api.Components.Schemas["InspectionTaskResponseDto"].Properties["mode"].Nullable);
        Assert.Contains("processingCounts", api.Components.Schemas["DatasetListItemDto"].Properties.Keys);
        var complete = api.Paths["/api/v1/survey-tasks/{taskId}/complete"].Operations[Microsoft.OpenApi.Models.OperationType.Post];
        Assert.Contains(complete.Security, requirement => requirement.Keys.Any(key => key.Reference?.Id == "Bearer"));
        Assert.Contains(complete.Security, requirement => requirement.Keys.Any(key => key.Reference?.Id == "WebSession"));
        Assert.Contains(complete.Parameters, parameter => parameter.Name == "If-Match");
        Assert.Contains(complete.Parameters, parameter => parameter.Name == "Idempotency-Key");
        Assert.Contains("datasetId", api.Components.Schemas["CompleteSurveyTaskRequestDto"].Properties.Keys);
        Assert.Contains("assessmentId", api.Components.Schemas["H6NotificationScopeDto"].Properties.Keys);
    }

    private async Task<HttpClient> Login(AuthenticationWebApplicationFactory factory, Guid actor)
    {
        await using var db = sql.CreateDbContext(); var email = await db.Users.Where(x => x.Id == actor).Select(x => x.Email).SingleAsync();
        var client = factory.CreateClient(new() { BaseAddress = new("https://localhost"), HandleCookies = false });
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode); var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString()); return client;
    }
}
