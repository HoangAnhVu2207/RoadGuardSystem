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

[Trait("Finding", "A08-01")]
[Collection(AuthenticationApiFixture.Name)]
public sealed class AiCallbackWireBoundaryAcceptanceTests
{
    private const string Operation = "ProcessingAiResultReceived";
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private readonly AuthenticationSqlServerFixture _sql;
    private readonly ITestOutputHelper _output;

    public AiCallbackWireBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql, ITestOutputHelper output)
        => (_sql, _output) = (sql, output);


    [Fact]
    public async Task ExactAiCallbackWireAndCredentialBoundaries()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        string Token(string kind)
        {
            if (kind == "missing") return "";
            if (kind == "malformed") return "invalid";
            var encoded = TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId);
            if (kind == "valid") return encoded;
            var payload = new JwtSecurityTokenHandler().ReadJwtToken(encoded).Payload;
            if (kind == "issuer") payload["iss"] = "untrusted";
            if (kind == "audience") payload["aud"] = "human-frontend";
            if (kind == "subject") payload["sub"] = "wrong-service";
            if (kind == "jti") payload.Remove("jti");
            if (kind == "expired") { payload["iat"] = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(); payload["nbf"] = payload["iat"]; payload["exp"] = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(); }
            if (kind == "job") payload["jobId"] = scenario.CrossProjectJob.Id.ToString();
            if (kind == "attempt") payload["attemptId"] = scenario.OtherAttemptId.ToString();
            using var rsa = RSA.Create(); rsa.ImportFromPem(TestAiServiceIdentity.Options.PrivateKeyPem);
            var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = kind == "kid" ? "unknown-kid" : TestAiServiceIdentity.Options.ActiveKeyId };
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.RsaSha256)), payload));
        }
        async Task<HttpResponseMessage> Send(string token, ReceiveAiResultRequestDto request)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results") { Content = JsonContent.Create(request) };
            message.Headers.Add("Idempotency-Key", scenario.Key);
            if (token.Length > 0) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(message);
        }
        foreach (var kind in new[] { "missing", "malformed", "issuer", "audience", "subject", "jti", "expired", "kid", "job", "attempt" })
        {
            var before = await ReadAsync(scenario, "before-credential");
            using var response = await Send(Token(kind), scenario.Request);
            var expected = kind is "job" or "attempt" ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
            response.StatusCode.Should().Be(expected);
            using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            error.RootElement.GetProperty("status").GetInt32().Should().Be((int)expected);
            error.RootElement.GetProperty("code").GetString().Should().Be(expected == HttpStatusCode.Forbidden ? "access_forbidden" : "auth_unauthorized");
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            (await ReadAsync(scenario, "after-credential")).Should().BeEquivalentTo(before);
        }
        using var success = await Send(Token("valid"), scenario.Request);
        success.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await success.Content.ReadAsStringAsync(); using var json = JsonDocument.Parse(body); var value = json.RootElement;
        value.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("id", "status", "resultId", "attemptNumber", "version", "jobType", "error");
        value.GetProperty("id").GetGuid().Should().Be(scenario.Job.Id); value.GetProperty("status").GetString().Should().Be("SUCCEEDED");
        value.GetProperty("resultId").GetGuid().Should().Be(Guid.Parse(scenario.Request.Detections.Single().DetectionId));
        value.GetProperty("attemptNumber").GetInt32().Should().Be(1); value.GetProperty("jobType").GetString().Should().Be("AI_ANALYSIS"); value.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        success.Content.Headers.ContentType!.MediaType.Should().Be("application/json"); success.Headers.Location.Should().BeNull();
        success.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"'); Guid.TryParse(success.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        await using (var db = _sql.CreateDbContext())
        {
            var job = await db.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == scenario.Job.Id);
            Convert.ToBase64String(job.RowVersion).Should().Be(value.GetProperty("version").GetString()); job.Status.Should().Be(ProcessingJobStatus.Completed);
            (await db.AIDetections.CountAsync(d => d.ProcessingJobId == job.Id)).Should().Be(1);
        }
        var committed = await ReadAsync(scenario, "committed");
        using var replay = await Send(Token("valid"), scenario.Request); replay.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replay.Content.ReadAsStringAsync()).Should().Be(body); replay.Headers.ETag.Should().Be(success.Headers.ETag);
        (await ReadAsync(scenario, "replayed")).Should().BeEquivalentTo(committed);
        using var changed = await Send(Token("valid"), scenario.Request with { ChecksumSha256 = new string('c', 64) }); changed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var conflict = JsonDocument.Parse(await changed.Content.ReadAsStringAsync()); conflict.RootElement.GetProperty("code").GetString().Should().Be("duplicate_request");
        (await ReadAsync(scenario, "conflict")).Should().BeEquivalentTo(committed);
    }
    [Theory]
    [InlineData("root")]
    [InlineData("detection")]
    [InlineData("header")]
    public async Task ExactCallbackRejectsUnknownMembersAndOversizedKey(string boundary)
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var body = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(scenario.Request, WebJson))!;
        if (boundary == "root") body["undeclared"] = "deny";
        if (boundary == "detection") body["detections"]![0]!["undeclared"] = "deny";
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
        { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId));
        request.Headers.Add("Idempotency-Key", boundary == "header" ? new string('k', 201) : scenario.Key);
        var before = await ReadAsync(scenario, "before-schema");
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        json.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
        Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        (await ReadAsync(scenario, "after-schema")).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task RetiredAiSigningKeyIsDeniedWhileRotatedKeyRemainsValid()
    {
        var scenario = await SetupAsync();
        var oldToken = TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId);
        using var nextRsa = RSA.Create(2048);
        var nextOptions = new AiServiceTokenOptions
        {
            Enabled = true,
            Subject = TestAiServiceIdentity.Options.Subject,
            ActiveKeyId = "rotated-private-test-key",
            PrivateKeyPem = nextRsa.ExportPkcs8PrivateKeyPem(),
            PublicKeys = new() { ["rotated-private-test-key"] = nextRsa.ExportSubjectPublicKeyInfoPem() }
        };
        var nextTokens = new AiCallbackTokens(nextOptions, TimeProvider.System);
        var nextToken = nextTokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId);
        async Task<HttpResponseMessage> Send(HttpClient client, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results")
            { Content = JsonContent.Create(scenario.Request) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Idempotency-Key", scenario.Key);
            return await client.SendAsync(request);
        }
        async Task<AuthenticationWebApplicationFactory> Host(bool retainOldKey)
        {
            var verifierOptions = new AiServiceTokenOptions
            {
                Enabled = true,
                Subject = nextOptions.Subject,
                ActiveKeyId = nextOptions.ActiveKeyId,
                PrivateKeyPem = nextOptions.PrivateKeyPem,
                PublicKeys = new(nextOptions.PublicKeys)
            };
            if (retainOldKey)
                verifierOptions.PublicKeys.Add(TestAiServiceIdentity.Options.ActiveKeyId,
                    TestAiServiceIdentity.Options.PublicKeys[TestAiServiceIdentity.Options.ActiveKeyId]);
            var host = new AuthenticationWebApplicationFactory(_sql.ConnectionString, configureTestServices: services =>
            {
                services.RemoveAll<AiCallbackTokens>();
                services.AddSingleton(new AiCallbackTokens(verifierOptions, TimeProvider.System));
            });
            await Task.CompletedTask;
            return host;
        }
        await using (var overlap = await Host(true))
        {
            using var client = overlap.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            using var first = await Send(client, oldToken);
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            using var rotated = await Send(client, nextToken);
            rotated.StatusCode.Should().Be(HttpStatusCode.OK);
            (await rotated.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());
            rotated.Headers.ETag.Should().Be(first.Headers.ETag);
        }
        var committed = await ReadAsync(scenario, "before-retired-key");
        await using (var removed = await Host(false))
        {
            using var client = removed.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            using var denied = await Send(client, oldToken);
            denied.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            using var json = JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("status").GetInt32().Should().Be(401);
            json.RootElement.GetProperty("code").GetString().Should().Be("auth_unauthorized");
            denied.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            denied.Headers.ETag.Should().BeNull(); denied.Headers.Location.Should().BeNull();
            Guid.TryParse(denied.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            (await ReadAsync(scenario, "after-retired-key")).Should().BeEquivalentTo(committed);
            using var retained = await Send(client, nextToken);
            retained.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadAsync(scenario, "after-retained-key")).Should().BeEquivalentTo(committed);
        }
        TestAiServiceIdentity.Options.PublicKeys.ContainsKey(TestAiServiceIdentity.Options.ActiveKeyId).Should().BeTrue();
    }

    [Fact]
    public async Task HumanCredentialAndInvalidAiClaimBoundariesHaveNoCallbackEffects()
    {
        var scenario = await SetupAsync();
        await using var factory = new AuthenticationWebApplicationFactory(_sql.ConnectionString);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var human = await _sql.CreateUserAsync("callback-human-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(human.UserName!), password = "Current1!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var humanToken = loginBody.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", humanToken);
        using var me = await client.GetAsync("/api/v1/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var validToken = TestAiServiceIdentity.Tokens.Mint(scenario.Job.Id, scenario.Request.JobAttemptId);
        var original = new JwtSecurityTokenHandler().ReadJwtToken(validToken);
        original.ValidTo.Should().Be(original.IssuedAt.AddSeconds(300));
        foreach (var kind in new[] { "human", "signature", "algorithm", "futureIat", "overTtl", "sub", "iss", "aud", "exp", "iat", "jti", "jobId", "attemptId" })
        {
            var payload = new JwtSecurityTokenHandler().ReadJwtToken(validToken).Payload;
            if (kind == "futureIat") payload["iat"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds();
            else if (kind == "overTtl") payload["exp"] = (long)payload["iat"] + 301;
            else if (kind is not ("human" or "signature" or "algorithm")) payload.Remove(kind);
            SigningCredentials credentials;
            using var rsa = RSA.Create(2048);
            if (kind != "signature") rsa.ImportFromPem(TestAiServiceIdentity.Options.PrivateKeyPem);
            if (kind == "algorithm")
                credentials = new SigningCredentials(new SymmetricSecurityKey(Enumerable.Repeat((byte)61, 32).ToArray()) { KeyId = TestAiServiceIdentity.Options.ActiveKeyId }, SecurityAlgorithms.HmacSha256);
            else credentials = new SigningCredentials(new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = TestAiServiceIdentity.Options.ActiveKeyId }, SecurityAlgorithms.RsaSha256);
            var token = kind == "human" ? humanToken : new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/internal/processing-jobs/{scenario.Job.Id}/results") { Content = JsonContent.Create(scenario.Request) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("Idempotency-Key", scenario.Key);
            var before = await ReadAsync(scenario, "before-claim");
            using var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, kind);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("status").GetInt32().Should().Be(401);
            json.RootElement.GetProperty("code").GetString().Should().Be("auth_unauthorized");
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            (await ReadAsync(scenario, "after-claim")).Should().BeEquivalentTo(before);
        }
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
        receipt.RequestFingerprint.Should().Be(Fingerprint(scenario.Request));
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

    private static string Fingerprint(ReceiveAiResultRequestDto request) => Hash(JsonSerializer.Serialize(request.Detections.Select(item => new
    { detectionId = Guid.Parse(item.DetectionId), item.FrameFileId, item.TimestampMs, item.TypeCode, item.Confidence, item.Bbox }), WebJson) + request.ChecksumSha256);
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
