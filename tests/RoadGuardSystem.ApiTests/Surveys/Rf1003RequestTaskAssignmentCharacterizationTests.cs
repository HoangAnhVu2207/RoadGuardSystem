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

[Trait("TaskId", "RF-10-03-C02")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1003RequestTaskAssignmentCharacterizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1003RequestTaskAssignmentCharacterizationTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task OldRequestAndV2TaskUseDistinctRoutesAndV2CreatesAssignment()
    {
        var supervisor = await _sql.CreateUserAsync($"rf1003c02_sup_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await _sql.CreateUserAsync($"rf1003c02_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var operatorUser = await _sql.CreateUserAsync($"rf1003c02_op_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var replacementOperator = await _sql.CreateUserAsync($"rf1003c02_op2_{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var secondManager = await _sql.CreateUserAsync($"rf1003c02_pm2_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var reporter = await _sql.CreateUserAsync($"rf1003c02_reporter_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        await AuthenticateAsync(client, supervisor.UserName!);
        var projectId = await CreateProjectAsync(client, manager.Id);
        var secondProjectId = await CreateProjectAsync(client, secondManager.Id);
        var routeVersionId = await CreateRoadSectionAsync(client, projectId);
        var secondRouteVersionId = await CreateRoadSectionAsync(client, secondProjectId);
        var segmentSetId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        await using (var setup = _sql.CreateDbContext())
        {
            foreach (var membershipProject in new[] { projectId, secondProjectId })
            foreach (var member in new[] { operatorUser.Id, replacementOperator.Id })
                setup.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = membershipProject, UserId = member,
                    RoleCode = UserRoleCode.DroneOperator, ValidFrom = new DateOnly(2026, 1, 1), Status = ProjectMemberStatus.Active });
            setup.RoadSegmentSets.Add(RoadSegmentSet.Create(segmentSetId, routeVersionId));
            setup.RoadSegments.Add(RoadSegment.Create(segmentId, segmentSetId, routeVersionId, 1));
            var secondSegmentSetId = Guid.NewGuid();
            var secondSegmentId = Guid.NewGuid();
            setup.RoadSegmentSets.Add(RoadSegmentSet.Create(secondSegmentSetId, secondRouteVersionId));
            setup.RoadSegments.Add(RoadSegment.Create(secondSegmentId, secondSegmentSetId, secondRouteVersionId, 1));
            await setup.SaveChangesAsync();
        }

        await AuthenticateAsync(client, manager.UserName!);
        var oldOperationId = Guid.NewGuid();
        var oldPayload = new
        {
            roadSectionVersionId = routeVersionId,
            surveyPlanId = (Guid?)null,
            surveyType = 2,
            dueAt = "2026-10-05T10:00:00Z",
            outputRequirements = "{\"formats\":[\"video\"]}",
            operationId = oldOperationId
        };
        var oldResponse = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{await RoadSectionIdAsync(routeVersionId)}/survey-requests", oldPayload);
        oldResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var oldBody = await oldResponse.Content.ReadFromJsonAsync<JsonElement>();
        var oldRequestId = oldBody.GetProperty("requestId").GetGuid();
        var oldBeforeReplay = await ReadRequestEffectsAsync(oldRequestId);
        var oldProjectBeforeReplay = await ReadProjectEffectsAsync(projectId);
        var oldReceiptBeforeReplay = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyRequestCreated", oldOperationId.ToString("N"));
        var oldReplay = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections/{await RoadSectionIdAsync(routeVersionId)}/survey-requests", oldPayload);
        oldReplay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await oldReplay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestId").GetGuid().Should().Be(oldRequestId);
        var oldAfterReplay = await ReadRequestEffectsAsync(oldRequestId);
        oldAfterReplay.Should().BeEquivalentTo(oldBeforeReplay);
        (await ReadProjectEffectsAsync(projectId)).Should().BeEquivalentTo(oldProjectBeforeReplay);
        (await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyRequestCreated", oldOperationId.ToString("N"))).Should().BeEquivalentTo(oldReceiptBeforeReplay);

        var oldAsV2 = await client.GetAsync($"/api/v1/survey-tasks/{oldRequestId}");
        oldAsV2.StatusCode.Should().Be(HttpStatusCode.NotFound, "V2 task lookup requires an assignment-backed task");

        var scope = new[] { new { routeVersionId, segmentSetId, segmentIds = new[] { segmentId }, targetBand = "SURFACE" } };
        var secondSegmentSetIdForRequest = await SegmentSetIdAsync(secondRouteVersionId);
        var secondSegmentIdForRequest = await SegmentIdAsync(secondSegmentSetIdForRequest);
        var secondScope = new[] { new { routeVersionId = secondRouteVersionId, segmentSetId = secondSegmentSetIdForRequest, segmentIds = new[] { secondSegmentIdForRequest }, targetBand = "SURFACE" } };
        using var createTask = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-tasks")
        {
            Content = JsonContent.Create(new { scope, surveyType = "BASELINE", operatorId = operatorUser.Id, dueAt = "2026-10-06T10:00:00Z", accessPoint = (object?)null })
        };
        createTask.Headers.Add("Idempotency-Key", "rf1003-c02-task-001");
        var taskResponse = await client.SendAsync(createTask);
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskBody = await taskResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = taskBody.GetProperty("id").GetGuid();
        taskBody.GetProperty("operatorId").GetGuid().Should().Be(operatorUser.Id);
        taskBody.GetProperty("projectId").GetGuid().Should().Be(projectId);
        var initialVersion = taskBody.GetProperty("version").GetString();
        initialVersion.Should().NotBeNullOrWhiteSpace();

        await AuthenticateAsync(client, operatorUser.UserName!);
        var assignedRead = await client.GetAsync($"/api/v1/survey-tasks/{taskId}");
        assignedRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var assignedBody = await assignedRead.Content.ReadFromJsonAsync<JsonElement>();
        assignedBody.GetProperty("id").GetGuid().Should().Be(taskId);
        assignedBody.GetProperty("operatorId").GetGuid().Should().Be(operatorUser.Id);
        assignedBody.GetProperty("status").GetString().Should().Be("NEW_ASSIGNED");

        await AuthenticateAsync(client, reporter.UserName!);
        var wrongRole = await client.GetAsync($"/api/v1/survey-tasks/{taskId}");
        wrongRole.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await AuthenticateAsync(client, secondManager.UserName!);
        using var positiveProjectB = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{secondProjectId}/survey-tasks")
        {
            Content = JsonContent.Create(new { scope = secondScope, surveyType = "BASELINE", operatorId = replacementOperator.Id, dueAt = "2026-10-06T10:00:00Z", accessPoint = (object?)null })
        };
        positiveProjectB.Headers.Add("Idempotency-Key", "rf1003-c02-project-b-positive");
        var positiveB = await client.SendAsync(positiveProjectB);
        positiveB.StatusCode.Should().Be(HttpStatusCode.Created);
        await AuthenticateAsync(client, manager.UserName!);
        var beforeWrongProject = await ReadProjectEffectsAsync(secondProjectId);
        using var wrongProjectRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{secondProjectId}/survey-tasks")
        {
            Content = JsonContent.Create(new { scope = secondScope, surveyType = "BASELINE", operatorId = replacementOperator.Id, dueAt = "2026-10-06T10:00:00Z", accessPoint = (object?)null })
        };
        wrongProjectRequest.Headers.Add("Idempotency-Key", "rf1003-c02-wrong-project");
        var wrongProject = await client.SendAsync(wrongProjectRequest);
        wrongProject.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var afterWrongProject = await ReadProjectEffectsAsync(secondProjectId);
        afterWrongProject.Should().BeEquivalentTo(beforeWrongProject);

        var beforeReassign = await ReadLifecycleAsync(taskId);
        var reassignReceiptBefore = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2reassign", "rf1003-c02-reassign-001");
        using var reassign = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-tasks/{taskId}/reassign")
        {
            Content = JsonContent.Create(new { operatorId = replacementOperator.Id, reason = "Operator coverage changed", dueAt = "2026-10-07T10:00:00Z" })
        };
        reassign.Headers.Add("Idempotency-Key", "rf1003-c02-reassign-001");
        reassign.Headers.Add("If-Match", $"\"{initialVersion}\"");
        var reassigned = await client.SendAsync(reassign);
        reassigned.StatusCode.Should().Be(HttpStatusCode.OK);
        var reassignedBody = await reassigned.Content.ReadFromJsonAsync<JsonElement>();
        reassignedBody.GetProperty("operatorId").GetGuid().Should().Be(replacementOperator.Id);
        reassignedBody.GetProperty("status").GetString().Should().Be("REASSIGNED");
        var reassignedVersion = reassignedBody.GetProperty("version").GetString();
        reassignedVersion.Should().NotBeNullOrWhiteSpace();

        var afterReassign = await ReadLifecycleAsync(taskId);
        afterReassign.Assignments.Single(item => item.Active).OperatorId.Should().Be(replacementOperator.Id);
        afterReassign.Assignments.Single(item => !item.Active).OperatorId.Should().Be(operatorUser.Id);
        afterReassign.Assignments.Length.Should().Be(beforeReassign.Assignments.Length + 1);
        afterReassign.AuditIds.Length.Should().Be(beforeReassign.AuditIds.Length + 1);
        var reassignReceiptAfter = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2reassign", "rf1003-c02-reassign-001");
        reassignReceiptAfter.Length.Should().Be(reassignReceiptBefore.Length + 1);

        await AuthenticateAsync(client, replacementOperator.UserName!);
        var replacementRead = await client.GetAsync($"/api/v1/survey-tasks/{taskId}");
        replacementRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var replacementBody = await replacementRead.Content.ReadFromJsonAsync<JsonElement>();
        replacementBody.GetProperty("id").GetGuid().Should().Be(taskId);
        replacementBody.GetProperty("operatorId").GetGuid().Should().Be(replacementOperator.Id);
        replacementBody.GetProperty("projectId").GetGuid().Should().Be(projectId);
        replacementBody.GetProperty("status").GetString().Should().Be("REASSIGNED");

        await AuthenticateAsync(client, manager.UserName!);

        using var staleReassign = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-tasks/{taskId}/reassign")
        {
            Content = JsonContent.Create(new { operatorId = operatorUser.Id, reason = "Stale precondition" })
        };
        staleReassign.Headers.Add("Idempotency-Key", "rf1003-c02-reassign-stale");
        staleReassign.Headers.Add("If-Match", $"\"{initialVersion}\"");
        var beforeStale = await ReadLifecycleAsync(taskId);
        var staleReceiptBefore = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2reassign", "rf1003-c02-reassign-stale");
        var stale = await client.SendAsync(staleReassign);
        stale.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var afterStale = await ReadLifecycleAsync(taskId);
        afterStale.Should().BeEquivalentTo(beforeStale);
        var staleReceiptAfter = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2reassign", "rf1003-c02-reassign-stale");
        staleReceiptAfter.Length.Should().Be(staleReceiptBefore.Length + 1);

        using var replayReassign = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/survey-tasks/{taskId}/reassign")
        {
            Content = JsonContent.Create(new { operatorId = replacementOperator.Id, reason = "Operator coverage changed", dueAt = "2026-10-07T10:00:00Z" })
        };
        replayReassign.Headers.Add("Idempotency-Key", "rf1003-c02-reassign-001");
        replayReassign.Headers.Add("If-Match", $"\"{initialVersion}\"");
        var beforeReplayReassign = await ReadLifecycleAsync(taskId);
        var reassignReceiptReplayBefore = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2reassign", "rf1003-c02-reassign-001");
        var replayReassignment = await client.SendAsync(replayReassign);
        replayReassignment.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replayReassignment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("operatorId").GetGuid().Should().Be(replacementOperator.Id);
        var afterReplayReassign = await ReadLifecycleAsync(taskId);
        afterReplayReassign.Should().BeEquivalentTo(beforeReplayReassign);
        (await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2reassign", "rf1003-c02-reassign-001")).Should().BeEquivalentTo(reassignReceiptReplayBefore);
        reassignReceiptAfter.Should().BeEquivalentTo(reassignReceiptReplayBefore);

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-tasks")
        {
            Content = JsonContent.Create(new { scope, surveyType = "BASELINE", operatorId = operatorUser.Id, dueAt = "2026-10-06T10:00:00Z", accessPoint = (object?)null })
        };
        replayRequest.Headers.Add("Idempotency-Key", "rf1003-c02-task-001");
        var beforeCreateReplay = await ReadTaskEffectsAsync(taskId);
        var createReceiptBefore = await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2Created", "rf1003-c02-task-001");
        var replay = await client.SendAsync(replayRequest);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        replayBody.GetProperty("id").GetGuid().Should().Be(taskId);
        replayBody.GetProperty("projectId").GetGuid().Should().Be(projectId);
        replayBody.GetProperty("operatorId").GetGuid().Should().Be(operatorUser.Id);
        replayBody.GetProperty("status").GetString().Should().Be("NEW_ASSIGNED");
        replayBody.GetProperty("version").GetString().Should().Be(initialVersion);
        var afterCreateReplay = await ReadTaskEffectsAsync(taskId);
        afterCreateReplay.Should().Be(beforeCreateReplay);
        (await ReadIdempotencyReceiptAsync(manager.Id, projectId, "SurveyTaskV2Created", "rf1003-c02-task-001")).Should().BeEquivalentTo(createReceiptBefore);
        var missingKey = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/survey-tasks")
        {
            Content = JsonContent.Create(new { scope, surveyType = "BASELINE", operatorId = operatorUser.Id, dueAt = "2026-10-06T10:00:00Z", accessPoint = (object?)null })
        });
        missingKey.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);

        await using var verification = _sql.CreateDbContext();
        var oldRequest = await verification.SurveyRequests.AsNoTracking().SingleAsync(item => item.Id == oldRequestId);
        oldRequest.ProjectId.Should().Be(projectId);
        var task = await verification.SurveyRequests.AsNoTracking().SingleAsync(item => item.Id == taskId);
        task.ProjectId.Should().Be(projectId);
        var assignment = await verification.SurveyAssignments.AsNoTracking().SingleAsync(item => item.SurveyRequestId == taskId && item.EndedAt == null);
        assignment.OperatorUserId.Should().Be(replacementOperator.Id);
        (await verification.SurveyAssignments.AsNoTracking().CountAsync(item => item.SurveyRequestId == oldRequestId)).Should().Be(0);
        task.RowVersion.Should().NotBeNullOrEmpty();
    }

    private async Task<TaskLifecycleSnapshot> ReadLifecycleAsync(Guid taskId)
    {
        await using var context = _sql.CreateDbContext();
        var task = await context.SurveyRequests.AsNoTracking().SingleAsync(item => item.Id == taskId);
        var assignments = await context.SurveyAssignments.AsNoTracking().Where(item => item.SurveyRequestId == taskId).OrderBy(item => item.AssignedAt).ToListAsync();
        var audits = await context.AuditLogs.AsNoTracking().Where(item => item.EntityType == "SurveyRequest" && item.EntityId == taskId).OrderBy(item => item.Id).Select(item => item.Id).ToArrayAsync();
        return new(
            task.Id,
            task.ProjectId,
            task.Status.ToString(),
            Convert.ToBase64String(task.RowVersion),
            assignments.Select(item => new AssignmentSnapshot(item.Id, item.OperatorUserId, item.EndedAt is null)).ToArray(),
            audits);
    }

    private async Task<RequestEffectsSnapshot> ReadRequestEffectsAsync(Guid requestId)
    {
        await using var context = _sql.CreateDbContext();
        var request = await context.SurveyRequests.AsNoTracking().SingleAsync(item => item.Id == requestId);
        return new(
            request.Id,
            request.ProjectId,
            request.RoadSectionId,
            request.RoadSectionVersionId,
            request.Status.ToString(),
            Convert.ToBase64String(request.RowVersion),
            await context.SurveyAssignments.AsNoTracking().Where(item => item.SurveyRequestId == requestId).Select(item => item.Id).OrderBy(item => item).ToArrayAsync(),
            await context.AuditLogs.AsNoTracking().Where(item => item.EntityType == "SurveyRequest" && item.EntityId == requestId).Select(item => item.Id).OrderBy(item => item).ToArrayAsync());
    }

    private async Task<ProjectEffectsSnapshot> ReadProjectEffectsAsync(Guid projectId)
    {
        await using var context = _sql.CreateDbContext();
        var requestIds = await context.SurveyRequests.AsNoTracking().Where(item => item.ProjectId == projectId).Select(item => item.Id).OrderBy(item => item).ToArrayAsync();
        return new(
            requestIds,
            await context.SurveyAssignments.AsNoTracking().CountAsync(item => requestIds.Contains(item.SurveyRequestId)),
            await context.AuditLogs.AsNoTracking().CountAsync(item => item.EntityType == "SurveyRequest" && requestIds.Contains(item.EntityId)));
    }

    private async Task<(Guid TaskId, Guid ProjectId, Guid OperatorId, string Status, string Version, int Assignments, int Audits)> ReadTaskEffectsAsync(Guid taskId)
    {
        await using var context = _sql.CreateDbContext();
        var task = await context.SurveyRequests.AsNoTracking().SingleAsync(item => item.Id == taskId);
        return (
            task.Id,
            task.ProjectId,
            await context.SurveyAssignments.AsNoTracking().Where(item => item.SurveyRequestId == taskId && item.EndedAt == null).Select(item => item.OperatorUserId).SingleAsync(),
            task.Status.ToString(),
            Convert.ToBase64String(task.RowVersion),
            await context.SurveyAssignments.AsNoTracking().CountAsync(item => item.SurveyRequestId == taskId),
            await context.AuditLogs.AsNoTracking().CountAsync(item => item.EntityType == "SurveyRequest" && item.EntityId == taskId));
    }

    private async Task<IdempotencyReceiptSnapshot[]> ReadIdempotencyReceiptAsync(Guid actorId, Guid projectId, string operation, string key)
    {
        await using var context = _sql.CreateDbContext();
        return await context.IdempotencyRecords.AsNoTracking()
            .Where(item => item.ActorUserId == actorId && item.ProjectId == projectId && item.Operation == operation && item.IdempotencyKey == key)
            .OrderBy(item => item.Id)
            .Select(item => new IdempotencyReceiptSnapshot(item.Id, item.OperationId, item.Operation, item.IdempotencyKey, item.RequestFingerprint, item.OutcomeJson))
            .ToArrayAsync();
    }

    private sealed record AssignmentSnapshot(Guid Id, Guid OperatorId, bool Active);
    private sealed record TaskLifecycleSnapshot(Guid TaskId, Guid ProjectId, string Status, string Version, AssignmentSnapshot[] Assignments, Guid[] AuditIds);
    private sealed record RequestEffectsSnapshot(Guid RequestId, Guid ProjectId, Guid RoadSectionId, Guid? RoadSectionVersionId, string Status, string Version, Guid[] AssignmentIds, Guid[] AuditIds);
    private sealed record ProjectEffectsSnapshot(Guid[] RequestIds, int Assignments, int Audits);
    private sealed record IdempotencyReceiptSnapshot(Guid Id, Guid OperationId, string Operation, string Key, string Fingerprint, string OutcomeJson);

    private async Task<Guid> SegmentSetIdAsync(Guid routeVersionId)
    {
        await using var context = _sql.CreateDbContext();
        return await context.RoadSegmentSets.Where(item => item.RoadSectionVersionId == routeVersionId).Select(item => item.Id).SingleAsync();
    }

    private async Task<Guid> SegmentIdAsync(Guid segmentSetId)
    {
        await using var context = _sql.CreateDbContext();
        return await context.RoadSegments.Where(item => item.SegmentSetId == segmentSetId).Select(item => item.Id).SingleAsync();
    }

    private async Task<Guid> CreateProjectAsync(HttpClient client, Guid managerId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = $"RF1003C02-{Guid.NewGuid():N}", name = "RF-10-03-C02", engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = managerId, handover = new { documentNo = $"HD-{Guid.NewGuid():N}", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    private async Task<Guid> CreateRoadSectionAsync(HttpClient client, Guid projectId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/projects/{projectId}/road-sections", new { code = $"RS-{Guid.NewGuid():N}", srid = 32648, coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500100d, y = 1100100d } }, effectiveFrom = "2026-09-21T08:00:00+07:00", changeReason = "C02", operationId = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("roadSectionVersionId").GetGuid();
    }

    private async Task<Guid> RoadSectionIdAsync(Guid versionId)
    {
        await using var context = _sql.CreateDbContext();
        return await context.RoadSectionVersions.Where(value => value.Id == versionId).Select(value => value.RoadSectionId).SingleAsync();
    }

    private static async Task AuthenticateAsync(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }
}
