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
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
using Xunit.Abstractions;

namespace RoadGuardSystem.ApiTests.Processing;

[Trait("Finding", "A08-01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class A0801CallbackIdentityCharacterizationTests
{
    private const string Operation = "ProcessingAiResultReceived";
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private readonly AuthenticationSqlServerFixture _sql;
    private readonly ITestOutputHelper _output;

    public A0801CallbackIdentityCharacterizationTests(AuthenticationSqlServerFixture sql, ITestOutputHelper output)
        => (_sql, _output) = (sql, output);

    [Fact]
    public async Task OldAttemptAfterRetry_Returns409WithoutEffects_CurrentAttemptReturns200AndReplays()
    {
        var scenario = await SetupAsync();
        Guid currentAttempt;
        await using (var context = _sql.CreateDbContext())
        {
            // Controlled retryable-failure fixture; the result DTO has no failure/status field.
            await context.ProcessingJobs.Where(j => j.Id == scenario.Job.Id).ExecuteUpdateAsync(u =>
                u.SetProperty(j => j.Status, ProcessingJobStatus.RetryableFailure));
            var failed = await context.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == scenario.Job.Id);
            var repository = new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context));
            var retry = await repository.RetryAsync(new(scenario.Scope.ActorId, scenario.Job.Id, "fixture retry",
                Convert.ToBase64String(failed.RowVersion), scenario.Key + "-retry", Hash(scenario.Key + "-retry"), null));
            retry.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
            currentAttempt = await context.ProcessingAttempts.Where(a => a.ProcessingJobId == scenario.Job.Id)
                .OrderByDescending(a => a.AttemptNo).Select(a => a.Id).FirstAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var before = await ReadAsync(scenario, "before-stale");
        var stale = await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key);
        stale.Status.Should().Be(HttpStatusCode.Conflict);
        var rejected = await ReadAsync(scenario, "after-stale");
        rejected.EffectsJson.Should().Be(before.EffectsJson);
        rejected.Receipts.Should().ContainSingle();
        var current = scenario.Request with { JobAttemptId = currentAttempt };
        var accepted = await SendAsync(client, scenario.Job.Id, current, scenario.ControlKey);
        accepted.Status.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(accepted.Body);
        body.RootElement.GetProperty("attemptNumber").GetInt32().Should().Be(2);
        body.RootElement.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        var completed = await ReadAsync(scenario, "after-current");
        (await SendAsync(client, scenario.Job.Id, current, scenario.ControlKey)).Should().Be(accepted);
        (await ReadAsync(scenario, "after-current-replay")).Should().BeEquivalentTo(completed);
        (await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key + "-late-again")).Status
            .Should().Be(HttpStatusCode.Conflict);
        (await ReadAsync(scenario, "after-late-again")).EffectsJson.Should().Be(completed.EffectsJson);
        await using var final = _sql.CreateDbContext();
        (await final.AIDetections.CountAsync(d => d.ProcessingJobId == scenario.Job.Id)).Should().Be(1);
        var job = await final.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == scenario.Job.Id);
        job.Status.Should().Be(ProcessingJobStatus.Completed);
        Convert.ToBase64String(job.RowVersion).Should().Be(body.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task ValidCallback_ThenSameKeyReplay_ReturnsStoredOutcomeAndOneEffect()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var first = await AcceptAsync(client, scenario);
        var before = await ReadAsync(scenario, "before-same-replay");
        var replay = await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key);
        replay.Should().Be(first);
        (await ReadAsync(scenario, "after-same-replay")).Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("JobId")]
    [InlineData("AttemptId")]
    [InlineData("ManifestHash")]
    [InlineData("ModelVersionId")]
    [InlineData("Mode")]
    [InlineData("RawResultFileId")]
    public async Task ChangedIdentity_SameKeyConflicts_NewKeyValidatesAndRejects(string field)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        var first = await AcceptAsync(client, scenario);
        var jobId = field == "JobId" ? scenario.OtherJob.Id : scenario.Job.Id;
        var changed = field switch
        {
            "AttemptId" => scenario.Request with { JobAttemptId = scenario.OtherAttemptId },
            "ManifestHash" => scenario.Request with { ManifestHash = new string('b', 64) },
            "ModelVersionId" => scenario.Request with { ModelVersionId = scenario.OtherScope.ModelId },
            "Mode" => scenario.Request with { Mode = "REAL" },
            "RawResultFileId" => scenario.Request with { RawResultFileId = scenario.OtherScope.RawFileId },
            _ => scenario.Request
        };
        Fingerprint(changed, jobId).Should().NotBe(Fingerprint(scenario.Request, scenario.Job.Id));
        var beforeReplay = await ReadAsync(scenario, "before-changed-replay-" + field);
        var conflict = await SendAsync(client, jobId, changed, scenario.Key);
        conflict.Status.Should().Be(HttpStatusCode.Conflict);
        using var problem = JsonDocument.Parse(conflict.Body);
        problem.RootElement.GetProperty("code").GetString().Should().Be("duplicate_request");
        conflict.ETag.Should().BeNull();
        var afterReplay = await ReadAsync(scenario, "after-changed-replay-" + field);
        afterReplay.Should().BeEquivalentTo(beforeReplay);
        // A fresh key distinguishes durable replay from the handler's identity checks.
        var beforeControl = await ReadAsync(scenario, "before-new-key-control-" + field);
        var rejection = await SendAsync(client, jobId, changed, scenario.ControlKey);
        rejection.Status.Should().Be(HttpStatusCode.Conflict);
        var afterControl = await ReadAsync(scenario, "after-new-key-control-" + field);
        afterControl.EffectsJson.Should().Be(beforeControl.EffectsJson);
        afterControl.Receipts.Should().HaveCount(beforeControl.Receipts.Count + 1);
        afterControl.Receipts.Single(value => value.IdempotencyKey == scenario.Key)
            .Should().BeEquivalentTo(beforeControl.Receipts.Single());
        var rejected = afterControl.Receipts.Single(value => value.IdempotencyKey == scenario.ControlKey);
        rejected.RequestFingerprint.Should().Be(Fingerprint(changed, jobId));
        using var stored = JsonDocument.Parse(rejected.OutcomeJson);
        stored.RootElement.GetProperty("Status").GetInt32().Should().Be((int)ProcessingJobPersistenceStatus.Conflict);
        stored.RootElement.GetProperty("Job").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("Detections")]
    [InlineData("Checksum")]
    public async Task ChangedFingerprint_WithValidStructure_ReturnsConflictWithoutSqlChange(string field)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AcceptAsync(client, scenario);
        var changed = field == "Checksum"
            ? scenario.Request with { ChecksumSha256 = new string('c', 64) }
            : scenario.Request with { Detections = [scenario.Request.Detections.Single() with { Confidence = 0.8m }] };
        Fingerprint(changed, scenario.Job.Id).Should().NotBe(Fingerprint(scenario.Request, scenario.Job.Id));
        var before = await ReadAsync(scenario, "before-fingerprint-" + field);
        var conflict = await SendAsync(client, scenario.Job.Id, changed, scenario.Key);
        conflict.Status.Should().Be(HttpStatusCode.Conflict);
        using var problem = JsonDocument.Parse(conflict.Body);
        problem.RootElement.GetProperty("title").GetString()
            .Should().Be("Idempotency key was reused with a different request");
        problem.RootElement.GetProperty("code").GetString().Should().Be("duplicate_request");
        (await ReadAsync(scenario, "after-fingerprint-" + field)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task SameKeyAndFingerprint_DifferentProject_ExecutesIndependentReceipt()
    {
        // Empty detection arrays are accepted by the current DTO/service. They avoid global detection-PK reuse.
        var scenario = await SetupAsync(emptyDetections: true);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AcceptAsync(client, scenario);
        var otherRequest = new ReceiveAiResultRequestDto(scenario.CrossProjectAttemptId,
            scenario.CrossProjectJob.ManifestHash, scenario.OtherScope.ModelId, "MOCK",
            scenario.OtherScope.RawFileId, scenario.OtherScope.Checksum, []);
        Fingerprint(otherRequest, scenario.CrossProjectJob.Id).Should().NotBe(Fingerprint(scenario.Request, scenario.Job.Id));
        var before = await ReadAsync(scenario, "before-cross-project");
        var result = await SendAsync(client, scenario.CrossProjectJob.Id, otherRequest, scenario.Key);
        result.Status.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(result.Body);
        body.RootElement.GetProperty("id").GetGuid().Should().Be(scenario.CrossProjectJob.Id);
        var after = await ReadAsync(scenario, "after-cross-project");
        after.Receipts.Should().HaveCount(2);
        var receipt = after.Receipts.Single(value => value.ProjectId == scenario.OtherScope.ProjectId);
        receipt.OperationId.Should().Be(scenario.CrossProjectJob.Id);
        receipt.RequestFingerprint.Should().NotBe(before.Receipts.Single().RequestFingerprint);
        receipt.Id.Should().NotBe(before.Receipts.Single().Id);
        await using var context = _sql.CreateDbContext();
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync(value => value.Id == scenario.CrossProjectJob.Id);
        job.Status.Should().Be(ProcessingJobStatus.Completed);
        Convert.ToBase64String(job.RowVersion).Should().NotBe(scenario.CrossProjectJob.Version);
        var original = await context.ProcessingJobs.AsNoTracking().SingleAsync(value => value.Id == scenario.Job.Id);
        Convert.ToBase64String(original.RowVersion).Should().Be(scenario.CompletedVersion);
        (await context.AIDetections.AsNoTracking().Where(value => scenario.JobIds.Contains(value.ProcessingJobId)).ToListAsync()).Should().BeEmpty();
        // Verify all other observed effects by replacing only the independently completed job in the snapshot.
        after.StableEffectsJson.Should().Be(before.StableEffectsJson);
        after.Receipts.Single(value => value.ProjectId == scenario.Scope.ProjectId).Should().BeEquivalentTo(before.Receipts.Single());
    }

    [Theory]
    [InlineData("Mode", HttpStatusCode.BadRequest)]
    [InlineData("AttemptId", HttpStatusCode.Forbidden)]
    public async Task InvalidUpstreamIdentity_IsRejectedBeforeReceiptEvenWithReplayKey(string field, HttpStatusCode expected)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = CreateClient(factory);
        await AcceptAsync(client, scenario);
        var changed = field == "Mode" ? scenario.Request with { Mode = "INVALID" }
            : scenario.Request with { JobAttemptId = Guid.Empty };
        var before = await ReadAsync(scenario, "before-upstream-" + field);
        (await SendAsync(client, scenario.Job.Id, changed, scenario.Key)).Status.Should().Be(expected);
        (await ReadAsync(scenario, "after-upstream-" + field)).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task MissingAiCredential_IsUnauthorizedAndHasNoSqlEffect()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var before = await ReadAsync(scenario, "before-unauthorized");
        (await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key)).Status.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadAsync(scenario, "after-unauthorized")).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task CommittedDispatchMintsPerAttemptCredentialAndCompletedJobCannotRedispatch()
    {
        var scenario = await SetupAsync();
        await using var db = _sql.CreateDbContext();
        var outbox = await db.OutboxMessages.AsNoTracking().SingleAsync(x => x.CorrelationId == scenario.Job.Id && x.MessageType == "processing_job.dispatch");
        var adapter = new RoadGuardSystem.Services.Processing.AiDispatchAdapter(new AiDispatchSourceRepository(db),
            TestAiServiceIdentity.Tokens, TestAiServiceIdentity.Options);
        var envelope = await adapter.PrepareAsync(outbox.Id);
        envelope.Should().NotBeNull(); envelope!.JobId.Should().Be(scenario.Job.Id);
        envelope.AttemptId.Should().Be(scenario.Request.JobAttemptId);
        envelope.ToString().Should().NotContain(envelope.Bearer);
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", envelope.Bearer);
        (await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key)).Status.Should().Be(HttpStatusCode.OK);
        (await adapter.PrepareAsync(outbox.Id)).Should().BeNull();
        (await adapter.PrepareAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Theory]
    [InlineData("human", HttpStatusCode.Unauthorized)]
    [InlineData("job", HttpStatusCode.Forbidden)]
    [InlineData("attempt", HttpStatusCode.Forbidden)]
    [InlineData("expired", HttpStatusCode.Unauthorized)]
    public async Task InvalidServiceCredentialHasNoSqlEffect(string kind, HttpStatusCode expected)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        string raw;
        if (kind == "human")
        {
            var jwt = new JwtSecurityToken(AuthenticationWebApplicationFactory.TestIssuer, AuthenticationWebApplicationFactory.TestAudience,
                [new Claim("sub", Guid.NewGuid().ToString())], DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(new SymmetricSecurityKey(AuthenticationWebApplicationFactory.CurrentSigningKey)
                { KeyId = AuthenticationWebApplicationFactory.CurrentKeyId }, SecurityAlgorithms.HmacSha256));
            raw = new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        else
        {
            using var rsa = RSA.Create(); rsa.ImportFromPem(TestAiServiceIdentity.Options.PrivateKeyPem);
            var payload = new JwtSecurityTokenHandler().ReadJwtToken(TestAiServiceIdentity.Tokens.Mint(
                kind == "job" ? Guid.NewGuid() : scenario.Job.Id,
                kind == "attempt" ? Guid.NewGuid() : scenario.Request.JobAttemptId)).Payload;
            if (kind == "expired") { payload["iat"] = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(); payload["nbf"] = payload["iat"]; payload["exp"] = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(); }
            raw = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(new SigningCredentials(
                new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = TestAiServiceIdentity.Options.ActiveKeyId }, SecurityAlgorithms.RsaSha256)), payload));
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", raw);
        var before = await ReadAsync(scenario, "before-invalid-credential");
        (await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key)).Status.Should().Be(expected);
        (await ReadAsync(scenario, "after-invalid-credential")).Should().BeEquivalentTo(before);
    }

    private static HttpClient CreateClient(AuthenticationWebApplicationFactory factory)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-per-attempt");
        return client;
    }

    private async Task<HttpOutcome> SendAsync(HttpClient client, Guid jobId, ReceiveAiResultRequestDto request, string key)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{jobId}/results")
        { Content = JsonContent.Create(request) };
        message.Headers.Add("Idempotency-Key", key);
        if (client.DefaultRequestHeaders.Authorization?.Parameter == "test-per-attempt")
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestAiServiceIdentity.Tokens.Mint(jobId,
                request.JobAttemptId == Guid.Empty ? Guid.NewGuid() : request.JobAttemptId));
        using var response = await client.SendAsync(message);
        var result = new HttpOutcome(response.StatusCode, await response.Content.ReadAsStringAsync(), response.Headers.ETag?.Tag);
        _output.WriteLine("HTTP " + JsonSerializer.Serialize(new { jobId, key, request, result }));
        return result;
    }

    private async Task<HttpOutcome> AcceptAsync(HttpClient client, Scenario scenario)
    {
        var before = await ReadAsync(scenario, "before-first");
        before.Receipts.Should().BeEmpty();
        var result = await SendAsync(client, scenario.Job.Id, scenario.Request, scenario.Key);
        result.Status.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(result.Body);
        body.RootElement.GetProperty("id").GetGuid().Should().Be(scenario.Job.Id);
        body.RootElement.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        body.RootElement.GetProperty("attemptNumber").GetInt32().Should().Be(1);
        scenario.CompletedVersion = body.RootElement.GetProperty("version").GetString()!;
        result.ETag.Should().Be('"' + scenario.CompletedVersion + '"');
        var after = await ReadAsync(scenario, "after-first");
        after.StableEffectsJson.Should().Be(before.StableEffectsJson);
        var receipt = after.Receipts.Should().ContainSingle().Subject;
        receipt.OperationId.Should().Be(scenario.Job.Id);
        receipt.RequestFingerprint.Should().Be(Fingerprint(scenario.Request, scenario.Job.Id));
        using var stored = JsonDocument.Parse(receipt.OutcomeJson);
        stored.RootElement.GetProperty("Job").GetProperty("Id").GetGuid().Should().Be(scenario.Job.Id);
        stored.RootElement.GetProperty("Job").GetProperty("AttemptNumber").GetInt32().Should().Be(1);
        stored.RootElement.GetProperty("Status").GetInt32().Should().Be((int)ProcessingJobPersistenceStatus.Success);
        await using var context = _sql.CreateDbContext();
        var jobs = await context.ProcessingJobs.AsNoTracking().Where(value => scenario.JobIds.Contains(value.Id)).ToListAsync();
        jobs.Single(value => value.Id == scenario.Job.Id).Status.Should().Be(ProcessingJobStatus.Completed);
        jobs.Where(value => value.Id != scenario.Job.Id).Should().OnlyContain(value => value.Status == ProcessingJobStatus.Queued);
        Convert.ToBase64String(jobs.Single(value => value.Id == scenario.Job.Id).RowVersion).Should().Be(scenario.CompletedVersion);
        scenario.CompletedVersion.Should().NotBe(scenario.Job.Version);
        var detections = await context.AIDetections.AsNoTracking().Where(value => scenario.JobIds.Contains(value.ProcessingJobId)).ToListAsync();
        if (scenario.Request.Detections.Count == 0) detections.Should().BeEmpty();
        else
        {
            var detection = detections.Should().ContainSingle().Subject;
            detection.Id.Should().Be(Guid.Parse(scenario.Request.Detections.Single().DetectionId));
            detection.ProcessingJobId.Should().Be(scenario.Job.Id);
            detection.ModelVersionId.Should().Be(scenario.Scope.ModelId);
            detection.Confidence.Should().Be(0.9m);
            body.RootElement.GetProperty("resultId").GetGuid().Should().Be(detection.Id);
        }
        return result;
    }

    private async Task<Snapshot> ReadAsync(Scenario scenario, string label)
    {
        await using var context = _sql.CreateDbContext();
        var jobs = await context.ProcessingJobs.AsNoTracking().Where(value => scenario.JobIds.Contains(value.Id)).OrderBy(value => value.Id).ToListAsync();
        var attempts = await context.ProcessingAttempts.AsNoTracking().Where(value => scenario.JobIds.Contains(value.ProcessingJobId)).OrderBy(value => value.Id).ToListAsync();
        var detections = await context.AIDetections.AsNoTracking().Where(value => scenario.JobIds.Contains(value.ProcessingJobId)).OrderBy(value => value.Id).ToListAsync();
        var files = await context.Files.AsNoTracking().Where(value => scenario.FileIds.Contains(value.Id)).OrderBy(value => value.Id).ToListAsync();
        var scopes = await context.FileScopes.AsNoTracking().Where(value => scenario.FileIds.Contains(value.FileId)).OrderBy(value => value.Id).ToListAsync();
        var uploads = await context.UploadSessions.AsNoTracking().Where(value => scenario.FileIds.Contains(value.FileId)).OrderBy(value => value.Id).ToListAsync();
        var audit = await context.AuditLogs.AsNoTracking().Where(value => value.EntityType == "ProcessingJob" && scenario.JobIds.Contains(value.EntityId)).OrderBy(value => value.Id).ToListAsync();
        var outbox = await context.OutboxMessages.AsNoTracking().Where(value => value.CorrelationId != null && scenario.JobIds.Contains(value.CorrelationId.Value)).OrderBy(value => value.Id).ToListAsync();
        var receipts = await context.IdempotencyRecords.AsNoTracking().Where(value => value.ActorUserId == null &&
            (value.ProjectId == scenario.Scope.ProjectId || value.ProjectId == scenario.OtherScope.ProjectId) && value.Operation == Operation &&
            (value.IdempotencyKey == scenario.Key || value.IdempotencyKey == scenario.ControlKey)).OrderBy(value => value.Id).ToListAsync();
        var stable = new { attempts, files, scopes, uploads, audit, outbox };
        var snapshot = new Snapshot(JsonSerializer.Serialize(new { jobs, detections, stable }), JsonSerializer.Serialize(stable), receipts);
        _output.WriteLine("SQL " + label + " " + JsonSerializer.Serialize(snapshot));
        return snapshot;
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

    // OWNER-A1-REPLAY-RECEIPT-COMPATIBILITY replaces the historical incomplete hash.
    private static string Fingerprint(ReceiveAiResultRequestDto request, Guid jobId)
    {
        var detections = JsonSerializer.Serialize(request.Detections.Select(item => new
        { detectionId = Guid.Parse(item.DetectionId), item.FrameFileId, item.TimestampMs, item.TypeCode, item.Confidence, item.Bbox }), WebJson);
        return Hash(JsonSerializer.Serialize(new
        {
            receiptIdentity = "A1_CALLBACK_V2",
            JobId = jobId,
            AttemptId = request.JobAttemptId,
            request.ManifestHash,
            request.ModelVersionId,
            request.Mode,
            request.RawResultFileId,
            request.ChecksumSha256,
            DetectionsJson = detections
        }));
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
