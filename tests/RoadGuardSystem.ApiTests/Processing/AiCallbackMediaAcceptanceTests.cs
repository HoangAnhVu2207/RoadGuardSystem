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
public sealed class AiCallbackMediaAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    private readonly AuthenticationSqlServerFixture _sql = sql;
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);



    [Theory]
    [InlineData("application/json", false)]
    [InlineData("application/json", true)]
    [InlineData("text/json", false)]
    [InlineData("text/json", true)]
    [InlineData("application/vnd.roadguard+json", false)]
    [InlineData("application/vnd.roadguard+json", true)]
    public async Task DeclaredJsonMediaAndEmptyResultsCompleteExactlyOnce(string media, bool empty)
    {
        var scenario = await SetupAsync(empty);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        async Task<HttpResponseMessage> Send()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
            { Content = new StringContent(JsonSerializer.Serialize(scenario.Request, WebJson), Encoding.UTF8, media) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId));
            request.Headers.Add("Idempotency-Key", scenario.Key);
            return await client.SendAsync(request);
        }
        using var response = await Send();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body); var value = json.RootElement;
        value.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status", "resultId", "attemptNumber", "version", "jobType", "error");
        value.GetProperty("id").GetGuid().Should().Be(scenario.Job.Id);
        value.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        value.GetProperty("attemptNumber").GetInt32().Should().Be(1);
        value.GetProperty("jobType").GetString().Should().Be("AI_ANALYSIS");
        value.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        if (empty) value.GetProperty("resultId").ValueKind.Should().Be(JsonValueKind.Null);
        else value.GetProperty("resultId").GetGuid().Should().Be(Guid.Parse(scenario.Request.Detections.Single().DetectionId));
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
        response.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"');
        Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        await using (var db = _sql.CreateDbContext())
        {
            var job = await db.ProcessingJobs.AsNoTracking().SingleAsync(x => x.Id == scenario.Job.Id);
            job.Status.Should().Be(ProcessingJobStatus.Completed);
            Convert.ToBase64String(job.RowVersion).Should().Be(value.GetProperty("version").GetString());
            (await db.AIDetections.CountAsync(x => x.ProcessingJobId == job.Id)).Should().Be(empty ? 0 : 1);
            (await db.IdempotencyRecords.CountAsync(x => x.ProjectId == job.ProjectId && x.IdempotencyKey == scenario.Key)).Should().Be(1);
        }
        var committed = await SnapshotAsync(scenario);
        using var replay = await Send(); replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replay.Content.ReadAsStringAsync()).Should().Be(body);
        replay.Headers.ETag.Should().Be(response.Headers.ETag);
        (await SnapshotAsync(scenario)).Should().Be(committed);
    }

    [Theory]
    [InlineData("missing-attempt", 403)]
    [InlineData("zero-attempt", 403)]
    [InlineData("other-attempt", 403)]
    [InlineData("invalid-attempt", 400)]
    [InlineData("missing-model", 422)]
    [InlineData("missing-file", 422)]
    [InlineData("zero-job", 403)]
    [InlineData("invalid-route", 404)]
    [InlineData("missing-job", 404)]
    public async Task RouteAndAttemptInputBoundariesPreserveDurableState(string boundary, int expected)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(scenario.Request, WebJson))!;
        if (boundary == "missing-attempt") body.AsObject().Remove("jobAttemptId");
        if (boundary == "zero-attempt") body["jobAttemptId"] = Guid.Empty.ToString();
        if (boundary == "other-attempt") body["jobAttemptId"] = scenario.OtherAttemptId.ToString();
        if (boundary == "invalid-attempt") body["jobAttemptId"] = "invalid";
        if (boundary == "missing-model") body.AsObject().Remove("modelVersionId");
        if (boundary == "missing-file") body.AsObject().Remove("rawResultFileId");
        var route = boundary switch { "zero-job" => Guid.Empty.ToString(), "invalid-route" => "invalid", "missing-job" => Guid.NewGuid().ToString(), _ => scenario.Job.Id.ToString() };
        var tokenJob = boundary == "missing-job" ? Guid.Parse(route) : scenario.Job.Id;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{route}/results")
        { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAiServiceIdentity.Tokens.Mint(tokenJob, scenario.Request.JobAttemptId));
        request.Headers.Add("Idempotency-Key", scenario.Key);
        var before = await SnapshotAsync(scenario);
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be((HttpStatusCode)expected);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(expected);
        json.RootElement.GetProperty("code").GetString().Should().Be(expected switch { 403 => "access_forbidden", 404 => "not_found", _ => "validation_error" });
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
        Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        (await SnapshotAsync(scenario)).Should().Be(before);
    }

    [Theory]
    [InlineData("missing-detectionId", 400)]
    [InlineData("missing-typeCode", 400)]
    [InlineData("missing-bbox", 400)]
    [InlineData("missing-frameFileId", 422)]
    [InlineData("invalid-frameFileId", 400)]
    [InlineData("null-frameFileId", 400)]
    [InlineData("null-confidence", 400)]
    [InlineData("invalid-confidence", 400)]
    [InlineData("null-timestampMs", 400)]
    [InlineData("overflow-timestampMs", 400)]
    [InlineData("null-bbox-item", 400)]
    [InlineData("string-bbox-item", 400)]
    [InlineData("null-body", 400)]
    [InlineData("empty-body", 400)]
    [InlineData("blank-key", 428)]
    [InlineData("whitespace-key", 428)]
    [InlineData("space-typeCode", 400)]
    [InlineData("invalid-file", 400)]
    [InlineData("null-file", 400)]
    [InlineData("null-model", 400)]
    public async Task NestedRequiredAndScalarInputsHaveExactDeniedWire(string boundary, int expected)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(scenario.Request, WebJson))!;
        var detection = body["detections"]![0]!;
        if (boundary.StartsWith("missing-", StringComparison.Ordinal)) detection.AsObject().Remove(boundary[8..]);
        if (boundary == "invalid-frameFileId") detection["frameFileId"] = "invalid";
        if (boundary == "null-frameFileId") detection["frameFileId"] = null;
        if (boundary == "null-confidence") detection["confidence"] = null;
        if (boundary == "invalid-confidence") detection["confidence"] = "invalid";
        if (boundary == "null-timestampMs") detection["timestampMs"] = null;
        if (boundary == "overflow-timestampMs") detection["timestampMs"] = 2147483648L;
        if (boundary == "null-bbox-item") detection["bbox"]![0] = null;
        if (boundary == "string-bbox-item") detection["bbox"]![0] = "invalid";
        if (boundary == "space-typeCode") detection["typeCode"] = " ";
        if (boundary == "invalid-file") body["rawResultFileId"] = "invalid";
        if (boundary == "null-file") body["rawResultFileId"] = null;
        if (boundary == "null-model") body["modelVersionId"] = null;
        var text = boundary switch { "null-body" => "null", "empty-body" => "", _ => body.ToJsonString() };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
        { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId));
        request.Headers.TryAddWithoutValidation("Idempotency-Key", boundary switch { "blank-key" => "", "whitespace-key" => "   ", _ => scenario.Key }).Should().BeTrue();
        var before = await SnapshotAsync(scenario);
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be((HttpStatusCode)expected);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(expected);
        json.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
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
