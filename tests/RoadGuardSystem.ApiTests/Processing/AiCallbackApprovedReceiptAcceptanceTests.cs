using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
using Xunit.Abstractions;

namespace RoadGuardSystem.ApiTests.Processing;


[Collection(AuthenticationApiFixture.Name)]
public sealed class AiCallbackApprovedReceiptAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;


    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private static async Task<(HttpStatusCode Status, string Body, string? ETag)> SendAsync(
        HttpClient client, Scenario scenario, bool credential = true, string? key = null)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
        { Content = JsonContent.Create(scenario.Request) };
        if (credential) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId));
        message.Headers.Add("Idempotency-Key", key ?? scenario.Key);
        using var response = await client.SendAsync(message);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(), response.Headers.ETag?.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyReceiptReturnsDuplicateRequestWithoutEffectsOrRewrite(bool completed)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        if (completed) (await SendAsync(client, scenario, key: scenario.ControlKey)).Status.Should().Be(HttpStatusCode.OK);
        // Create an actual legacy-hash receipt in the owned database using the public factory.
        // It is never transformed into a v2 receipt, even when its old payload hash matches.
        var detections = JsonSerializer.Serialize(scenario.Request.Detections.Select(item => new
        { detectionId = Guid.Parse(item.DetectionId), item.FrameFileId, item.TimestampMs, item.TypeCode, item.Confidence, item.Bbox }), WebJson);
        await using (var db = _sql.CreateDbContext())
        {
            var outcome = completed
                ? (await db.IdempotencyRecords.AsNoTracking().SingleAsync(x => x.IdempotencyKey == scenario.ControlKey &&
                    x.Operation == "ProcessingAiResultReceived" && x.ProjectId == scenario.Scope.ProjectId)).OutcomeJson
                : "{\"Status\":0,\"Job\":null}";
            db.IdempotencyRecords.Add(IdempotencyRecord.Create(null, scenario.Scope.ProjectId,
                "ProcessingAiResultReceived", scenario.Key, Hash(detections + scenario.Request.ChecksumSha256),
                scenario.Job.Id, outcome, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        var before = await SnapshotAsync(scenario);
        for (var replay = 0; replay < 2; replay++)
        {
            var denied = await SendAsync(client, scenario);
            denied.Status.Should().Be(HttpStatusCode.Conflict);
            using var json = JsonDocument.Parse(denied.Body);
            json.RootElement.GetProperty("code").GetString().Should().Be("duplicate_request");
            denied.ETag.Should().BeNull();
            (await SnapshotAsync(scenario)).Should().Be(before);
        }
    }

    [Fact]
    public async Task ConcurrentExactCallbacksReturnOneStoredOutcomeAndOneEffect()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => SendAsync(client, scenario)));
        responses.Should().OnlyContain(x => x.Status == HttpStatusCode.OK);
        responses.Should().OnlyContain(x => x.Body == responses[0].Body && x.ETag == responses[0].ETag);
        var before = await SnapshotAsync(scenario);
        (await SendAsync(client, scenario)).Should().Be(responses[0]);
        (await SnapshotAsync(scenario)).Should().Be(before);
        await using var db = _sql.CreateDbContext();
        (await db.AIDetections.CountAsync(x => x.ProcessingJobId == scenario.Job.Id)).Should().Be(1);
        (await db.IdempotencyRecords.CountAsync(x => x.ProjectId == scenario.Scope.ProjectId &&
            x.Operation == "ProcessingAiResultReceived" && x.IdempotencyKey == scenario.Key)).Should().Be(1);
    }

    [Fact]
    public async Task ReceiptReplayStillRequiresAuthentication()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        (await SendAsync(client, scenario)).Status.Should().Be(HttpStatusCode.OK);
        var before = await SnapshotAsync(scenario);
        (await SendAsync(client, scenario, credential: false)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await SnapshotAsync(scenario)).Should().Be(before);
    }

    [Fact]
    public async Task ExistingReceiptCannotReplayAfterGenuineRetrySupersedesAttempt()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        // A failed callback command may have a receipt; retry must also fence that replay.
        scenario = scenario with { Request = scenario.Request with { RawResultFileId = scenario.OtherScope.RawFileId } };
        (await SendAsync(client, scenario)).Status.Should().Be(HttpStatusCode.Conflict);
        await using (var db = _sql.CreateDbContext())
        {
            // Controlled failure-state prerequisite: the real repository produces attempt 2.
            await db.ProcessingJobs.Where(x => x.Id == scenario.Job.Id).ExecuteUpdateAsync(u =>
                u.SetProperty(x => x.Status, ProcessingJobStatus.RetryableFailure));
            var job = await db.ProcessingJobs.AsNoTracking().SingleAsync(x => x.Id == scenario.Job.Id);
            var repository = new ProcessingV2PersistenceService(db, new IdempotencyOperationService(db));
            (await repository.RetryAsync(new(scenario.Scope.ActorId, scenario.Job.Id, "approved replay fencing",
                Convert.ToBase64String(job.RowVersion), scenario.Key + "-retry", Hash(scenario.Key + "-retry"), null)))
                .Status.Should().Be(ProcessingJobPersistenceStatus.Success);
            (await db.ProcessingAttempts.CountAsync(x => x.ProcessingJobId == job.Id)).Should().Be(2);
        }
        var before = await SnapshotAsync(scenario);
        var denied = await SendAsync(client, scenario);
        denied.Status.Should().Be(HttpStatusCode.Conflict);
        using var json = JsonDocument.Parse(denied.Body);
        json.RootElement.GetProperty("code").GetString().Should().Be("duplicate_request");
        (await SnapshotAsync(scenario)).Should().Be(before);
    }

    private async Task<string> SnapshotAsync(Scenario scenario)
    {
        await using var db = _sql.CreateDbContext();
        var jobs = await db.ProcessingJobs.AsNoTracking().Where(x => scenario.JobIds.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync();
        var attempts = await db.ProcessingAttempts.AsNoTracking().Where(x => scenario.JobIds.Contains(x.ProcessingJobId)).OrderBy(x => x.Id).ToListAsync();
        var detections = await db.AIDetections.AsNoTracking().Where(x => scenario.JobIds.Contains(x.ProcessingJobId)).OrderBy(x => x.Id).ToListAsync();
        var receipts = await db.IdempotencyRecords.AsNoTracking().Where(x => x.Operation == "ProcessingAiResultReceived" &&
            x.IdempotencyKey == scenario.Key && x.ProjectId == scenario.Scope.ProjectId).OrderBy(x => x.Id).ToListAsync();
        return JsonSerializer.Serialize(new { jobs, attempts, detections, receipts, outbox = await db.OutboxMessages.AsNoTracking().Where(x => x.CorrelationId.HasValue && scenario.JobIds.Contains(x.CorrelationId.Value)).OrderBy(x => x.Id).ToListAsync(), audit = await db.AuditLogs.AsNoTracking().Where(x => scenario.JobIds.Contains(x.EntityId)).OrderBy(x => x.Id).ToListAsync() });
    }
    private async Task<Scenario> SetupAsync(bool emptyDetections = false)
    {
        var scope = await SeedScopeAsync();
        var otherScope = await SeedScopeAsync();
        var job = await CreateJobAsync(scope);
        var otherJob = await CreateJobAsync(scope);
        var crossProject = await CreateJobAsync(otherScope);
        await using var context = _sql.CreateDbContext();
        var attempt = await context.ProcessingAttempts.AsNoTracking().SingleAsync(value => value.ProcessingJobId == job.Id);
        var otherAttempt = await context.ProcessingAttempts.AsNoTracking().SingleAsync(value => value.ProcessingJobId == otherJob.Id);
        var crossAttempt = await context.ProcessingAttempts.AsNoTracking().SingleAsync(value => value.ProcessingJobId == crossProject.Id);
        job.ProjectId.Should().Be(scope.ProjectId);
        otherJob.ProjectId.Should().Be(scope.ProjectId);
        crossProject.ProjectId.Should().Be(otherScope.ProjectId);
        var detections = emptyDetections ? Array.Empty<AiDetectionDto>() : new[] { new AiDetectionDto(Guid.NewGuid().ToString(), scope.RawFileId, 0, "POTHOLE", 0.9m, [0m, 0m, 0.1m, 0.1m]) };
        return new Scenario(scope, otherScope, job, otherJob, crossProject, otherAttempt.Id, crossAttempt.Id,
            new ReceiveAiResultRequestDto(attempt.Id, job.ManifestHash, scope.ModelId, "MOCK", scope.RawFileId, scope.Checksum, detections));
    }

    private async Task<SeededJob> CreateJobAsync(Scope scope)
    {
        await using var context = _sql.CreateDbContext();
        var repository = new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context));
        var key = Guid.NewGuid().ToString("N");
        var result = await repository.CreateAsync(new(scope.ActorId, scope.DatasetId, "{}", scope.ModelId, "pre-v1", "config-v1", "MOCK", key, Hash(key), null));
        result.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync(value => value.Id == result.Job!.Id);
        return new SeededJob(job.Id, job.ProjectId, job.ManifestHash, Convert.ToBase64String(job.RowVersion));
    }

    private async Task<Scope> SeedScopeAsync()
    {
        // Reuse RF-10-05's entity setup; jobs/attempts are created by the production repository.
        await using var context = _sql.CreateDbContext();
        var actor = await _sql.CreateUserAsync("a0801_" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var now = DateTimeOffset.UtcNow;
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = "A0801-" + Guid.NewGuid().ToString("N"), Name = "Isolated callback characterization", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, "ROAD-" + Guid.NewGuid().ToString("N"));
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "fixture-model", Guid.NewGuid().ToString("N"), "file:///fixture-model", null, null, AIModelVersionStatus.Released, now, actor.Id);
        var checksum = new string('a', 64);
        var file = StoredFile.Create(Guid.NewGuid(), "objects/a0801/" + Guid.NewGuid().ToString("N"), "result.json", "application/json", 32, checksum, actor.Id, now, null);
        var fileScope = FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, actor.Id, "AI_RESULT", now);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor.Id, file.StorageUri, "AI_RESULT", "application/json", 32, checksum, 32, now.AddHours(1));
        context.AddRange(project, section, version, survey, dataset, model, file, fileScope, upload);
        if (!await context.DefectTypes.AnyAsync(value => value.Code == "POTHOLE")) context.Add(DefectType.Create("POTHOLE", "Pothole"));
        await context.SaveChangesAsync();
        upload.StartUploading("fixture-upload", now);
        await context.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await context.SaveChangesAsync();
        upload.MarkVerified();
        await context.SaveChangesAsync();
        return new Scope(project.Id, actor.Id, dataset.Id, model.Id, file.Id, checksum);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private sealed record Scope(Guid ProjectId, Guid ActorId, Guid DatasetId, Guid ModelId, Guid RawFileId, string Checksum);
    private sealed record HttpOutcome(HttpStatusCode Status, string Body, string? ETag);
    private sealed record Snapshot(string EffectsJson, string StableEffectsJson, IReadOnlyList<IdempotencyRecord> Receipts);
    private sealed record SeededJob(Guid Id, Guid ProjectId, string ManifestHash, string Version);
    private sealed record Scenario(Scope Scope, Scope OtherScope, SeededJob Job, SeededJob OtherJob,
        SeededJob CrossProjectJob, Guid OtherAttemptId, Guid CrossProjectAttemptId, ReceiveAiResultRequestDto Request)
    {
        public string Key { get; } = "a0801-" + Guid.NewGuid().ToString("N");
        public string ControlKey => Key + "-control";
        public Guid[] JobIds => [Job.Id, OtherJob.Id, CrossProjectJob.Id];
        public Guid[] FileIds => [Scope.RawFileId, OtherScope.RawFileId];
        public string CompletedVersion { get; set; } = string.Empty;
    }
}
