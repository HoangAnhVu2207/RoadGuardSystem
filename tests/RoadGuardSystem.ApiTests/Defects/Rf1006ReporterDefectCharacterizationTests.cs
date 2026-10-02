using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Defects;

[Trait("TaskId", "RF-10-06-C01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class Rf1006ReporterDefectCharacterizationTests
{
    private readonly AuthenticationSqlServerFixture _sql;

    public Rf1006ReporterDefectCharacterizationTests(AuthenticationSqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task ProcessingJobRead_CurrentRolesProjectionAndDurableEffects()
    {
        var supervisor = await _sql.CreateUserAsync($"rf1006_sup_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var reporter = await _sql.CreateUserAsync($"rf1006_reporter_{Guid.NewGuid():N}", "Current1!", UserRoleCode.Reporter);
        var otherManager = await _sql.CreateUserAsync($"rf1006_pm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var scope = await CreateProcessingScopeAsync(supervisor.Id, otherManager.Id);

        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var supervisorClient = CreateClient(factory);
        await AuthenticateAsync(supervisorClient, supervisor.UserName!, "Current1!");
        using var reporterClient = CreateClient(factory);
        await AuthenticateAsync(reporterClient, reporter.UserName!, "Current1!");
        using var otherManagerClient = CreateClient(factory);
        await AuthenticateAsync(otherManagerClient, otherManager.UserName!, "Current1!");

        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/processing-jobs")
        {
            Content = JsonContent.Create(new
            {
                datasetId = scope.DatasetId,
                scope = new
                {
                    routeVersionId = scope.RoadVersionId,
                    segmentSetId = Guid.NewGuid(),
                    segmentIds = new[] { Guid.NewGuid() },
                    targetBand = "SURFACE"
                },
                modelVersionId = scope.ModelId,
                preprocessingVersion = "rf1006-pre-v1",
                configVersion = "rf1006-config-v1",
                mode = "MOCK"
            })
        };
        var createIdempotencyKey = $"rf1006-create-{Guid.NewGuid():N}";
        create.Headers.Add("Idempotency-Key", createIdempotencyKey);
        using var createdResponse = await supervisorClient.SendAsync(create);
        createdResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        createdResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var created = await createdResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jobId = created.GetProperty("id").GetGuid();
        created.GetProperty("status").GetString().Should().Be("QUEUED");
        created.GetProperty("attemptNumber").GetInt32().Should().Be(1);

        var before = await ReadSnapshotAsync(jobId, supervisor.Id, createIdempotencyKey);
        before.Receipts.Should().ContainSingle();
        before.Receipts[0].ProjectId.Should().BeNull();
        before.Receipts[0].ActorUserId.Should().Be(supervisor.Id);
        before.Receipts[0].IdempotencyKey.Should().Be(createIdempotencyKey);
        before.Receipts[0].Operation.Should().Be("ProcessingJobCreated");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/processing-jobs/{jobId}");
        using var response = await supervisorClient.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.ETag.Should().NotBeNull();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["id", "status", "resultId", "attemptNumber", "version", "jobType", "error"]);
        body.GetProperty("id").GetGuid().Should().Be(jobId);
        body.GetProperty("status").GetString().Should().Be("QUEUED");
        body.GetProperty("attemptNumber").GetInt32().Should().Be(1);
        body.GetProperty("jobType").GetString().Should().Be("AI_ANALYSIS");
        body.GetProperty("version").GetString().Should().Be(response.Headers.ETag!.Tag!.Trim('"'));
        body.GetProperty("resultId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        var after = await ReadSnapshotAsync(jobId, supervisor.Id, createIdempotencyKey);
        after.Should().BeEquivalentTo(before);

        using var reporterResponse = await reporterClient.GetAsync($"/api/v1/processing-jobs/{jobId}");
        reporterResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemResponseAsync(reporterResponse, HttpStatusCode.Forbidden);
        (await ReadSnapshotAsync(jobId, supervisor.Id, createIdempotencyKey)).Should().BeEquivalentTo(before);

        using var wrongProjectResponse = await otherManagerClient.GetAsync($"/api/v1/processing-jobs/{jobId}");
        wrongProjectResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemResponseAsync(wrongProjectResponse, HttpStatusCode.Forbidden);
        (await ReadSnapshotAsync(jobId, supervisor.Id, createIdempotencyKey)).Should().BeEquivalentTo(before);
    }

    private async Task<ProcessingScope> CreateProcessingScopeAsync(Guid supervisorId, Guid otherManagerId)
    {
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), $"RF1006-{Guid.NewGuid():N}", "RF-10-06 processing project", null, 32648,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now);
        var otherProject = Project.Create(Guid.NewGuid(), $"RF1006-OTHER-{Guid.NewGuid():N}", "RF-10-06 other project", null, 32648,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now);
        var membership = new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = otherProject.Id, UserId = otherManagerId,
            RoleCode = UserRoleCode.ProjectManager, IsPrimary = true,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1), Status = ProjectMemberStatus.Active
        };
        var section = RoadGuardSystem.BusinessObjects.Projects.RoadSection.Create(Guid.NewGuid(), project.Id, $"RF1006-ROAD-{Guid.NewGuid():N}");
        var geometry = new LineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]) { SRID = 32648 };
        var version = RoadGuardSystem.BusinessObjects.Projects.RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "RF-10-06 fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed,
            SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "rf1006-model", "v1", "file:///rf1006-model", null, null,
            AIModelVersionStatus.Released, now, supervisorId);
        await using var context = _sql.CreateDbContext();
        context.AddRange(project, otherProject, membership, section, version, survey, dataset, model);
        await context.SaveChangesAsync();
        return new ProcessingScope(project.Id, dataset.Id, version.Id, model.Id);
    }

    private async Task<ReadSnapshot> ReadSnapshotAsync(Guid jobId, Guid actorUserId, string idempotencyKey)
    {
        await using var context = _sql.CreateDbContext();
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync(value => value.Id == jobId);
        var attempts = await context.ProcessingAttempts.AsNoTracking()
            .Where(value => value.ProcessingJobId == jobId)
            .OrderBy(value => value.AttemptNo)
            .ThenBy(value => value.Id)
            .Select(value => new AttemptSnapshot(value.Id, value.ProcessingJobId, value.AttemptNo, value.StartedAt, value.EndedAt, value.ErrorType, value.WorkerReference))
            .ToListAsync();
        var detections = await context.AIDetections.AsNoTracking()
            .Where(value => value.ProcessingJobId == jobId)
            .OrderBy(value => value.Id)
            .Select(value => new DetectionSnapshot(value.Id, value.ProcessingJobId, value.ModelVersionId, value.RoadSectionVersionId, value.DefectTypeCode, value.Confidence, value.EstimatedWidth, value.EstimatedLength, value.RawPayload))
            .ToListAsync();
        var audits = await context.AuditLogs.AsNoTracking()
            .Where(value => value.EntityType == "ProcessingJob" && value.EntityId == jobId)
            .OrderBy(value => value.OccurredAtUtc)
            .ThenBy(value => value.Id)
            .Select(value => new AuditSnapshot(value.Id, value.ActorUserId, value.EventType, value.EntityType, value.EntityId, value.BeforeSnapshot, value.AfterSnapshot, value.Reason, value.Source, value.CorrelationId))
            .ToListAsync();
        var outbox = await context.OutboxMessages.AsNoTracking()
            .Where(value => value.CorrelationId == jobId)
            .OrderBy(value => value.Id)
            .Select(value => new OutboxSnapshot(value.Id, value.MessageType, value.CorrelationId, value.PayloadJson, value.DeliveryStatus, value.DeliveryAttemptCount, value.LastErrorCode, value.LastErrorMessage))
            .ToListAsync();
        var receipts = await context.IdempotencyRecords.AsNoTracking()
            .Where(value => value.ActorUserId == actorUserId && value.ProjectId == null && value.Operation == "ProcessingJobCreated" && value.IdempotencyKey == idempotencyKey)
            .OrderBy(value => value.Id)
            .Select(value => new ReceiptSnapshot(value.Id, value.ActorUserId, value.ProjectId, value.Operation, value.IdempotencyKey, value.RequestFingerprint, value.OperationId, value.OutcomeJson))
            .ToListAsync();
        return new ReadSnapshot(
            new JobSnapshot(job.Id, job.ProcessingBlockId, job.ModelVersionId, job.ProjectId, job.ManifestHash, job.ManifestJson, job.Mode, job.Status, Convert.ToBase64String(job.RowVersion), job.StartedAt, job.CompletedAt, job.ErrorCode, job.ErrorMessage),
            attempts, detections, audits, outbox, receipts);
    }

    private static HttpClient CreateClient(AuthenticationWebApplicationFactory factory) => factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }

    private static async Task AssertProblemResponseAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetInt32().Should().Be((int)expected);
        body.GetProperty("code").GetString().Should().Be("access_forbidden");
    }

    private sealed record ProcessingScope(Guid ProjectId, Guid DatasetId, Guid RoadVersionId, Guid ModelId);
    private sealed record ReadSnapshot(JobSnapshot Job, IReadOnlyList<AttemptSnapshot> Attempts, IReadOnlyList<DetectionSnapshot> Detections, IReadOnlyList<AuditSnapshot> Audits, IReadOnlyList<OutboxSnapshot> Outbox, IReadOnlyList<ReceiptSnapshot> Receipts);
    private sealed record JobSnapshot(Guid Id, Guid ProcessingBlockId, Guid ModelVersionId, Guid ProjectId, string ManifestHash, string ManifestJson, string Mode, ProcessingJobStatus Status, string Version, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, string? ErrorCode, string? ErrorMessage);
    private sealed record AttemptSnapshot(Guid Id, Guid ProcessingJobId, int AttemptNo, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, ProcessingAttemptErrorType? ErrorType, string? WorkerReference);
    private sealed record DetectionSnapshot(Guid Id, Guid ProcessingJobId, Guid ModelVersionId, Guid? RoadSectionVersionId, string? DefectTypeCode, decimal Confidence, decimal? EstimatedWidth, decimal? EstimatedLength, string RawPayload);
    private sealed record AuditSnapshot(Guid Id, Guid? ActorUserId, string EventType, string EntityType, Guid EntityId, string? BeforeSnapshot, string? AfterSnapshot, string? Reason, string Source, Guid? CorrelationId);
    private sealed record OutboxSnapshot(Guid Id, string MessageType, Guid? CorrelationId, string PayloadJson, OutboxDeliveryStatus DeliveryStatus, int DeliveryAttemptCount, string? LastErrorCode, string? LastErrorMessage);
    private sealed record ReceiptSnapshot(Guid Id, Guid? ActorUserId, Guid? ProjectId, string Operation, string IdempotencyKey, string RequestFingerprint, Guid OperationId, string OutcomeJson);
}
