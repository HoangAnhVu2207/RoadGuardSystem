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
public sealed class AiCallbackScalarAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);




    [Theory]
    [InlineData("confidence-zero", 200)]
    [InlineData("confidence-one", 200)]
    [InlineData("missing-confidence", 200)]
    [InlineData("missing-timestamp", 200)]
    [InlineData("timestamp-max", 200)]
    [InlineData("bbox-origin-zero", 200)]
    [InlineData("bbox-full", 200)]
    [InlineData("bbox-edge", 200)]
    [InlineData("key-max", 200)]
    [InlineData("bbox-high-x", 422)]
    [InlineData("bbox-high-y", 422)]
    [InlineData("bbox-high-width", 422)]
    [InlineData("bbox-high-height", 422)]
    [InlineData("bbox-negative-y", 422)]
    [InlineData("bbox-negative-width", 422)]
    [InlineData("bbox-negative-height", 422)]
    [InlineData("bbox-overflow-x", 422)]
    [InlineData("bbox-overflow-y", 422)]
    [InlineData("null-attempt", 400)]
    [InlineData("null-detection-id", 400)]
    [InlineData("zero-detection-id", 422)]
    [InlineData("wrong-detections-type", 400)]
    [InlineData("wrong-bbox-type", 400)]
    [InlineData("short-checksum", 400)]
    [InlineData("long-manifest", 400)]
    [InlineData("lower-mode", 400)]
    public async Task ExactScalarBoundariesHaveApprovedWireAndDurableEffects(string boundary, int expected)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(scenario.Request, WebJson))!;
        var detection = body["detections"]![0]!;
        if (boundary == "confidence-zero") detection["confidence"] = 0m;
        if (boundary == "confidence-one") detection["confidence"] = 1m;
        if (boundary == "missing-confidence") detection.AsObject().Remove("confidence");
        if (boundary == "missing-timestamp") detection.AsObject().Remove("timestampMs");
        if (boundary == "timestamp-max") detection["timestampMs"] = int.MaxValue;
        string? box = boundary switch
        {
            "bbox-origin-zero" => "[0,0,0,0]",
            "bbox-full" => "[0,0,1,1]",
            "bbox-edge" => "[1,1,0,0]",
            "bbox-high-x" => "[1.1,0,0,0]",
            "bbox-high-y" => "[0,1.1,0,0]",
            "bbox-high-width" => "[0,0,1.1,0]",
            "bbox-high-height" => "[0,0,0,1.1]",
            "bbox-negative-y" => "[0,-0.1,0,0]",
            "bbox-negative-width" => "[0,0,-0.1,0]",
            "bbox-negative-height" => "[0,0,0,-0.1]",
            "bbox-overflow-x" => "[0.9,0,0.2,0]",
            "bbox-overflow-y" => "[0,0.9,0,0.2]",
            _ => null
        };
        if (box is not null) detection["bbox"] = System.Text.Json.Nodes.JsonNode.Parse(box);
        if (boundary == "null-attempt") body["jobAttemptId"] = null;
        if (boundary == "null-detection-id") detection["detectionId"] = null;
        if (boundary == "zero-detection-id") detection["detectionId"] = Guid.Empty.ToString();
        if (boundary == "wrong-bbox-type") detection["bbox"] = "invalid";
        if (boundary == "wrong-detections-type") body["detections"] = "invalid";
        if (boundary == "short-checksum") body["checksumSha256"] = "abc";
        if (boundary == "long-manifest") body["manifestHash"] = new string('a', 65);
        if (boundary == "lower-mode") body["mode"] = "mock";
        var key = boundary == "key-max" ? new string('k', 200) : scenario.Key;
        var before = await SnapshotAsync(scenario);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
        { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId));
        request.Headers.Add("Idempotency-Key", key);
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be((HttpStatusCode)expected);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        response.Headers.Location.Should().BeNull();
        if (expected != 200)
        {
            json.RootElement.GetProperty("status").GetInt32().Should().Be(expected);
            json.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            response.Headers.ETag.Should().BeNull();
            (await SnapshotAsync(scenario)).Should().Be(before);
            return;
        }
        var value = json.RootElement;
        value.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("id", "status", "resultId", "attemptNumber", "version", "jobType", "error");
        value.GetProperty("id").GetGuid().Should().Be(scenario.Job.Id);
        value.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        value.GetProperty("resultId").GetGuid().Should().Be(Guid.Parse(detection["detectionId"]!.GetValue<string>()));
        value.GetProperty("attemptNumber").GetInt32().Should().Be(1);
        value.GetProperty("jobType").GetString().Should().Be("AI_ANALYSIS");
        value.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        await using var db = _sql.CreateDbContext();
        var job = await db.ProcessingJobs.AsNoTracking().SingleAsync(x => x.Id == scenario.Job.Id);
        job.Status.Should().Be(ProcessingJobStatus.Completed);
        Convert.ToBase64String(job.RowVersion).Should().Be(value.GetProperty("version").GetString());
        response.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"');
        (await db.AIDetections.CountAsync(x => x.ProcessingJobId == job.Id)).Should().Be(1);
        (await db.IdempotencyRecords.CountAsync(x => x.ProjectId == job.ProjectId && x.IdempotencyKey == key)).Should().Be(1);
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
