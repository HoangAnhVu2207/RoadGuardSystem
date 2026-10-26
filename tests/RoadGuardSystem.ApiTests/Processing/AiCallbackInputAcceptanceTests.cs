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
public sealed class AiCallbackInputAcceptanceTests(AuthenticationSqlServerFixture sql)
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
    [InlineData("missing-manifest", 400)]
    [InlineData("null-manifest", 400)]
    [InlineData("uppercase-manifest", 400)]
    [InlineData("short-manifest", 400)]
    [InlineData("missing-checksum", 400)]
    [InlineData("null-checksum", 400)]
    [InlineData("invalid-checksum", 400)]
    [InlineData("missing-mode", 400)]
    [InlineData("invalid-mode", 400)]
    [InlineData("null-mode", 400)]
    [InlineData("missing-detections", 400)]
    [InlineData("null-detections", 400)]
    [InlineData("invalid-guid", 400)]
    [InlineData("zero-model", 422)]
    [InlineData("zero-file", 422)]
    [InlineData("null-detection", 400)]
    [InlineData("empty-detection-id", 400)]
    [InlineData("invalid-detection-id", 422)]
    [InlineData("zero-frame", 422)]
    [InlineData("negative-timestamp", 422)]
    [InlineData("null-type", 400)]
    [InlineData("empty-type", 400)]
    [InlineData("negative-confidence", 422)]
    [InlineData("high-confidence", 422)]
    [InlineData("null-bbox", 400)]
    [InlineData("short-bbox", 400)]
    [InlineData("long-bbox", 400)]
    [InlineData("negative-bbox", 422)]
    [InlineData("overflow-bbox", 422)]
    [InlineData("wrong-root", 400)]
    [InlineData("malformed-json", 400)]
    [InlineData("missing-header", 428)]
    [InlineData("unsupported-media", 415)]
    public async Task InvalidCallbackInputPreservesDurableState(string boundary, int expected)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(scenario.Request, WebJson))!;
        if (boundary.StartsWith("missing-", StringComparison.Ordinal) && boundary != "missing-header")
            body.AsObject().Remove(boundary[8..] switch { "manifest" => "manifestHash", "checksum" => "checksumSha256", var key => key });
        if (boundary == "null-manifest") body["manifestHash"] = null;
        if (boundary == "uppercase-manifest") body["manifestHash"] = new string('A', 64);
        if (boundary == "short-manifest") body["manifestHash"] = "abc";
        if (boundary == "null-checksum") body["checksumSha256"] = null;
        if (boundary == "invalid-checksum") body["checksumSha256"] = new string('z', 64);
        if (boundary == "invalid-mode") body["mode"] = "invalid";
        if (boundary == "null-mode") body["mode"] = null;
        if (boundary == "null-detections") body["detections"] = null;
        if (boundary == "invalid-guid") body["modelVersionId"] = "invalid";
        if (boundary == "zero-model") body["modelVersionId"] = Guid.Empty.ToString();
        if (boundary == "zero-file") body["rawResultFileId"] = Guid.Empty.ToString();
        if (boundary == "empty-detection-id") body["detections"]![0]!["detectionId"] = "";
        if (boundary == "invalid-detection-id") body["detections"]![0]!["detectionId"] = "invalid";
        if (boundary == "zero-frame") body["detections"]![0]!["frameFileId"] = Guid.Empty.ToString();
        if (boundary == "negative-timestamp") body["detections"]![0]!["timestampMs"] = -1;
        if (boundary == "null-type") body["detections"]![0]!["typeCode"] = null;
        if (boundary == "empty-type") body["detections"]![0]!["typeCode"] = "";
        if (boundary == "negative-confidence") body["detections"]![0]!["confidence"] = -0.1m;
        if (boundary == "high-confidence") body["detections"]![0]!["confidence"] = 1.1m;
        if (boundary == "null-bbox") body["detections"]![0]!["bbox"] = null;
        if (boundary == "short-bbox") body["detections"]![0]!["bbox"] = System.Text.Json.Nodes.JsonNode.Parse("[0,0,0]");
        if (boundary == "long-bbox") body["detections"]![0]!["bbox"] = System.Text.Json.Nodes.JsonNode.Parse("[0,0,0,0,0]");
        if (boundary == "negative-bbox") body["detections"]![0]!["bbox"] = System.Text.Json.Nodes.JsonNode.Parse("[-0.1,0,0.1,0.1]");
        if (boundary == "overflow-bbox") body["detections"]![0]!["bbox"] = System.Text.Json.Nodes.JsonNode.Parse("[0.9,0.9,0.2,0.2]");
        if (boundary == "null-detection") body["detections"]![0] = null;
        var text = boundary switch { "wrong-root" => "[]", "malformed-json" => "{", _ => body.ToJsonString() };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
        { Content = new StringContent(text, Encoding.UTF8, boundary == "unsupported-media" ? "text/plain" : "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId));
        if (boundary != "missing-header") request.Headers.Add("Idempotency-Key", scenario.Key);
        var before = await SnapshotAsync(scenario);
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be((HttpStatusCode)expected);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(expected);
        json.RootElement.GetProperty("code").GetString().Should().Be(expected == 415 ? "unsupported_media_type" : "validation_error");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
        Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
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
