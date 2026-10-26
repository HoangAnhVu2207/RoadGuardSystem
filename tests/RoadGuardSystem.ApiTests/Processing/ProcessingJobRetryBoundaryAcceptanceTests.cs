using System.Net;
using System.IO.Compression;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Repositories.Storage;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

using Microsoft.Extensions.Hosting;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.Services.Processing.Anh02;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProcessingJobRetryBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineProcessingRetryPreservesVersionReplayAndAuthority()
    {
        var supervisor = await sql.CreateUserAsync($"anh-super-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"anh-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var first = await sql.CreateUserAsync($"anh-op-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var second = await sql.CreateUserAsync($"anh-op2-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var media = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Anh02Fixtures", "synthetic-road-v1.mp4"));
        var checksum = Convert.ToHexString(SHA256.HashData(media)).ToLowerInvariant();
        var artifacts = new ArtifactFixture();
        var sourceStorage = new SurveyVideoStorage(media, checksum) { Frames = artifacts };
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IHostedService>(); services.Configure<Anh02AiOptions>(o => o.MockEnabled = true);
            services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(artifacts);
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(sourceStorage);
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var created = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"ANH-{Guid.NewGuid():N}",
            name = "DEMO survey evidence",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await Body(created)).GetProperty("projectId").GetGuid();
        await Login(client, manager.UserName!);
        var draft = await Command(client, $"/api/v1/projects/{project}/road-geometry-drafts", new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 0d,
            changeReason = "DEMO alignment",
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } },
            surveyWidthMeters = 12d,
            roadCode = "DEMO"
        });
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var road = await Command(client, draft.Headers.Location!.OriginalString + "/confirm", new
        {
            expectedCurrentVersionId = (Guid?)null,
            effectiveFrom = "2026-10-02T00:00:00Z",
            reason = "Supervisor DEMO approval"
        }, draft.Headers.ETag!.Tag);
        road.StatusCode.Should().Be(HttpStatusCode.Created);
        var route = (await Body(road)).GetProperty("routeVersionId").GetGuid();
        var section = (await Body(road)).GetProperty("roadSectionId").GetGuid();
        await Login(client, manager.UserName!);
        var setsPath = $"/api/v1/projects/{project}/road-sections/{section}/versions/{route}/segment-sets";
        var segmentSet = await Command(client, setsPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" });
        segmentSet.StatusCode.Should().Be(HttpStatusCode.Created);
        var set = (await Body(segmentSet)).GetProperty("id").GetGuid();
        var segment = (await Body(segmentSet)).GetProperty("segments")[0].GetProperty("id").GetGuid();
        var published = await Command(client, setsPath + $"/{set}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "PM DEMO publication" }, segmentSet.Headers.ETag!.Tag);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var device = Guid.NewGuid();
        // Registry and project membership provisioning are fixture setup; there is no assigned device allocation API.
        await using (var db = sql.CreateDbContext())
        {
            db.DroneDevices.Add(DroneDevice.Create(device, $"DEMO-{device:N}", DroneDeviceStatus.Active));
            foreach (var actor in new[] { first.Id, second.Id })
                db.ProjectMembers.Add(new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project,
                    UserId = actor,
                    RoleCode = UserRoleCode.DroneOperator,
                    ValidFrom = new DateOnly(2026, 1, 1),
                    Status = ProjectMemberStatus.Active
                });
            await db.SaveChangesAsync();
        }
        object Scope(string band) => new { routeVersionId = route, segmentSetId = set, segmentIds = new[] { segment }, targetBand = band };
        var scopes = new[] { Scope("SURFACE"), Scope("RIGHT_EDGE") };
        await Login(client, manager.UserName!);
        var plan = await Command(client, $"/api/v1/projects/{project}/survey-plans", new { scope = scopes, plannedAt = "2026-10-02T08:00:00Z", surveyType = "BASELINE" });
        plan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await Body(plan)).GetProperty("id").GetGuid();
        (await client.GetAsync(plan.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var taskResponse = await Command(client, $"/api/v1/projects/{project}/survey-tasks", new
        {
            scope = scopes,
            surveyType = "BASELINE",
            operatorId = first.Id,
            dueAt = "2026-10-03T08:00:00Z",
            accessPoint = (object?)null,
            planId
        });
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await Body(taskResponse)).GetProperty("id").GetGuid();
        await Login(client, first.UserName!);
        var work = await client.GetAsync($"/api/v1/survey-tasks/{task}/work-package");
        work.StatusCode.Should().Be(HttpStatusCode.OK);
        var workBody = await Body(work);
        workBody.GetProperty("schemaVersion").GetString().Should().Be("anh01.survey-work.v1");
        workBody.GetProperty("geometryRefs").GetArrayLength().Should().Be(1);
        workBody.GetProperty("geometryRefs")[0].TryGetProperty("targetBand", out _).Should().BeFalse();
        var accepted = await Command(client, $"/api/v1/survey-tasks/{task}/accept", null, taskResponse.Headers.ETag!.Tag);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadCreated = await Command(client, "/api/v1/uploads", new
        {
            purpose = "SURVEY_VIDEO",
            projectId = project,
            targetId = task,
            fileName = "synthetic.mp4",
            mediaType = "video/mp4",
            sizeBytes = media.Length,
            checksumSha256 = checksum
        });
        uploadCreated.StatusCode.Should().Be(HttpStatusCode.Created);
        var upload = (await Body(uploadCreated)).GetProperty("id").GetGuid();
        var video = (await Body(uploadCreated)).GetProperty("fileId").GetGuid();
        (await Command(client, $"/api/v1/uploads/{upload}/part-urls", new { partNumbers = new[] { 1 } })).StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadRead = await client.GetAsync($"/api/v1/uploads/{upload}");
        (await Command(client, $"/api/v1/uploads/{upload}/complete", new { parts = new[] { new { partNumber = 1, eTag = "fixture-1" } }, checksumSha256 = checksum }, uploadRead.Headers.ETag!.Tag))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var verification = factory.Services.CreateScope())
            await verification.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var fileMetadata = await client.GetAsync($"/api/v1/files/{video}");
        (await Body(fileMetadata)).GetProperty("status").GetString().Should().Be("VERIFIED");
        var submit = new
        {
            videoFileIds = new[] { video },
            telemetryFileIds = Array.Empty<Guid>(),
            recordedAt = "2026-10-02T08:00:00Z",
            deviceId = device,
            scope = scopes,
            pairs = new[] { new { videoFileId = video, telemetryFileId = (Guid?)null, timeOffsetMilliseconds = 0L } }
        };
        var submitted = await Command(client, $"/api/v1/survey-tasks/{task}/datasets", submit, accepted.Headers.ETag!.Tag);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataset = (await Body(submitted)).GetProperty("id").GetGuid();
        var detail = await client.GetAsync(submitted.Headers.Location);
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(detail)).GetProperty("submittedBy").GetGuid().Should().Be(first.Id);
        await Login(client, manager.UserName!);

        var model = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.AIModelVersions.Add(AIModelVersion.Create(model, "Synthetic fixture", $"synthetic-job-retry-{model:N}", "fixture://local", null, null, AIModelVersionStatus.Released, DateTimeOffset.UtcNow, supervisor.Id));
            await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM DefectTypes WHERE Code='CRACK') INSERT INTO DefectTypes(Code,Name,IsActive) VALUES ('CRACK','Crack',1)");
            await db.SaveChangesAsync();
        }


        var createInput = new RoadGuardSystem.DTOs.Processing.CreateProcessingJobRequestDto(dataset, new(route, set, [segment], "SURFACE"), model, "fixture-preprocess.v1", "fixture-config.v1", "MOCK");
        using var admission = await Command(client, "/api/v1/processing-jobs", createInput);
        admission.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var admitted = await Body(admission); var job = admitted.GetProperty("id").GetGuid(); var path = $"/api/v1/processing-jobs/{job}/retry";
        async Task<string> Snapshot(bool receipts = true)
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                job = await db.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == job),
                attempts = await db.ProcessingAttempts.AsNoTracking().Where(a => a.ProcessingJobId == job).OrderBy(a => a.Id).ToArrayAsync(),
                receipts = receipts ? await db.IdempotencyRecords.AsNoTracking().Where(r => r.ProjectId == project && r.Operation == "ProcessingJobRetried").OrderBy(r => r.Id).ToArrayAsync() : [],
                audit = await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == job).OrderBy(a => a.Id).ToArrayAsync(),
                messages = await db.OutboxMessages.AsNoTracking().Where(o => o.CorrelationId!.Value == job).OrderBy(o => o.Id).ToArrayAsync()
            });
        }
        async Task CheckProblem(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            response.StatusCode.Should().Be(status); response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var p = await Body(response); p.GetProperty("status").GetInt32().Should().Be((int)status); p.GetProperty("code").GetString().Should().Be(code);
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue(); response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
        }
        var initialVersion = admitted.GetProperty("version").GetString()!;
        // Current QUEUED job cannot retry. This conflict is a durable stored outcome.
        var before = await Snapshot(false); var conflictKey = Guid.NewGuid().ToString("N");
        using (var blocked = await Command(client, path, new { reason = "not retryable" }, initialVersion, conflictKey)) await CheckProblem(blocked, HttpStatusCode.Conflict, "validation_error");
        (await Snapshot(false)).Should().Be(before); var conflictSnapshot = await Snapshot();
        using (var replay = await Command(client, path, new { reason = "not retryable" }, initialVersion, conflictKey)) await CheckProblem(replay, HttpStatusCode.Conflict, "validation_error");
        (await Snapshot()).Should().Be(conflictSnapshot);
        // No public failure-status callback exists: this controlled SQL failure establishes retry eligibility only.
        await using (var db = sql.CreateDbContext()) await db.ProcessingJobs.Where(j => j.Id == job).ExecuteUpdateAsync(u => u.SetProperty(j => j.Status, ProcessingJobStatus.RetryableFailure));
        string version;
        await using (var db = sql.CreateDbContext()) version = Convert.ToBase64String((await db.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == job)).RowVersion);
        var retryInput = new { reason = "retry owned fixture failure" };
        before = await Snapshot(false);
        using (var stale = await Command(client, path, retryInput, initialVersion)) await CheckProblem(stale, HttpStatusCode.PreconditionFailed, "concurrency_conflict");
        (await Snapshot(false)).Should().Be(before);
        using (var malformed = await Command(client, path, retryInput, "not-base64")) await CheckProblem(malformed, HttpStatusCode.PreconditionFailed, "concurrency_conflict");
        (await Snapshot(false)).Should().Be(before);
        // Two actual HTTP requests race on the same resource version with distinct command keys.
        using var raceClient = factory.CreateClient(); raceClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        var raceKeys = new[] { Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") };
        var results = await Task.WhenAll(Command(client, path, retryInput, version, raceKeys[0]), Command(raceClient, path, retryInput, version, raceKeys[1]));
        results.Select(r => r.StatusCode).Order().Should().Equal(new[] { HttpStatusCode.OK, HttpStatusCode.PreconditionFailed }.Order());
        var winner = Array.FindIndex(results, r => r.StatusCode == HttpStatusCode.OK); using var success = results[winner]; using var lost = results[1 - winner];
        await CheckProblem(lost, HttpStatusCode.PreconditionFailed, "concurrency_conflict");
        var value = await Body(success); success.Content.Headers.ContentType!.MediaType.Should().Be("application/json"); success.Headers.Location.Should().BeNull();
        value.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "id", "status", "resultId", "attemptNumber", "version", "jobType", "error" }.Order());
        value.GetProperty("id").GetGuid().Should().Be(job); value.GetProperty("status").GetString().Should().Be("QUEUED"); value.GetProperty("attemptNumber").GetInt32().Should().Be(2); value.GetProperty("jobType").GetString().Should().Be("AI_ANALYSIS"); value.GetProperty("resultId").ValueKind.Should().Be(JsonValueKind.Null); value.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        success.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"'); Guid.TryParse(success.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        await using (var db = sql.CreateDbContext())
        {
            var current = await db.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == job); current.Status.Should().Be(ProcessingJobStatus.Queued); Convert.ToBase64String(current.RowVersion).Should().Be(value.GetProperty("version").GetString());
            (await db.ProcessingAttempts.CountAsync(a => a.ProcessingJobId == job)).Should().Be(2);
            (await db.AuditLogs.CountAsync(a => a.EntityId == job && a.EventType == "processing_job_retried" && a.ActorUserId == manager.Id)).Should().Be(1);
            (await db.OutboxMessages.CountAsync(o => o.CorrelationId!.Value == job && o.MessageType == "processing_job.dispatch")).Should().Be(2);
        }
        var committed = await Snapshot();
        using (var replay = await Command(client, path, retryInput, version, raceKeys[winner]))
        { replay.StatusCode.Should().Be(HttpStatusCode.OK); (await replay.Content.ReadAsStringAsync()).Should().Be(await success.Content.ReadAsStringAsync()); replay.Headers.ETag.Should().Be(success.Headers.ETag); replay.Headers.Location.Should().BeNull(); }
        (await Snapshot()).Should().Be(committed);
        using (var changed = await Command(client, path, new { reason = "changed payload" }, version, raceKeys[winner])) await CheckProblem(changed, HttpStatusCode.Conflict, "duplicate_request");
        (await Snapshot()).Should().Be(committed);
        foreach (var role in new[] { UserRoleCode.Reporter, UserRoleCode.RepairCrew, UserRoleCode.DroneOperator, UserRoleCode.ProjectManager })
        {
            var stranger = await sql.CreateUserAsync("retry-deny-" + Guid.NewGuid().ToString("N"), "Current1!", role); await Login(client, stranger.UserName!);
            using var denied = await Command(client, path, retryInput, version); await CheckProblem(denied, HttpStatusCode.Forbidden, "access_forbidden"); (await Snapshot()).Should().Be(committed);
        }
        await Login(client, supervisor.UserName!);
        await using (var db = sql.CreateDbContext()) await db.ProcessingJobs.Where(j => j.Id == job).ExecuteUpdateAsync(u => u.SetProperty(j => j.Status, ProcessingJobStatus.RetryableFailure));
        await using (var db = sql.CreateDbContext()) version = Convert.ToBase64String((await db.ProcessingJobs.AsNoTracking().SingleAsync(j => j.Id == job)).RowVersion);
        raceClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        var sameKey = Guid.NewGuid().ToString("N");
        var sameResults = await Task.WhenAll(Command(client, path, retryInput, version, sameKey), Command(raceClient, path, retryInput, version, sameKey));
        using (var firstRetry = sameResults[0]) using (var secondRetry = sameResults[1])
        {
            firstRetry.StatusCode.Should().Be(HttpStatusCode.OK); secondRetry.StatusCode.Should().Be(HttpStatusCode.OK);
            (await firstRetry.Content.ReadAsStringAsync()).Should().Be(await secondRetry.Content.ReadAsStringAsync()); firstRetry.Headers.ETag.Should().Be(secondRetry.Headers.ETag);
            (await Body(firstRetry)).GetProperty("attemptNumber").GetInt32().Should().Be(3);
        }
        await using (var db = sql.CreateDbContext())
        {
            (await db.ProcessingAttempts.CountAsync(a => a.ProcessingJobId == job)).Should().Be(3);
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == supervisor.Id && r.Operation == "ProcessingJobRetried" && r.IdempotencyKey == sameKey)).Should().Be(1);
            (await db.AuditLogs.CountAsync(a => a.EntityId == job && a.EventType == "processing_job_retried" && a.ActorUserId == supervisor.Id)).Should().Be(1);
            (await db.OutboxMessages.CountAsync(o => o.CorrelationId!.Value == job && o.MessageType == "processing_job.dispatch")).Should().Be(3);
        }
        await Login(client, manager.UserName!); committed = await Snapshot(); var bearer = client.DefaultRequestHeaders.Authorization;
        var sid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(bearer!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        client.DefaultRequestHeaders.Authorization = null; using (var denied = await Command(client, path, retryInput, version)) await CheckProblem(denied, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered"); using (var denied = await Command(client, path, retryInput, version)) await CheckProblem(denied, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new RoadGuardSystem.Services.Factories.AccessTokenFactory(factory.Services.GetRequiredService<IOptions<RoadGuardSystem.Services.Options.JwtOptions>>().Value).Create(manager.Id, sid, UserRoleCode.ProjectManager, DateTimeOffset.UtcNow.AddMinutes(-20), 15));
        using (var denied = await Command(client, path, retryInput, version)) await CheckProblem(denied, HttpStatusCode.Unauthorized, "auth_unauthorized"); client.DefaultRequestHeaders.Authorization = bearer;
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == project && m.UserId == manager.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        using (var denied = await Command(client, path, retryInput, version, raceKeys[winner])) await CheckProblem(denied, HttpStatusCode.Forbidden, "access_forbidden");
        using (var denied = await Command(client, path, retryInput, version)) await CheckProblem(denied, HttpStatusCode.Forbidden, "access_forbidden");
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(s => s.Id == sid).ExecuteUpdateAsync(u => u.SetProperty(s => s.RevokedAt, DateTimeOffset.UtcNow));
        using (var denied = await Command(client, path, retryInput, version, raceKeys[winner])) await CheckProblem(denied, HttpStatusCode.Unauthorized, "auth_session_revoked");
        (await Snapshot()).Should().Be(committed);
    }
    private sealed class ArtifactFixture(IAnh02ArtifactStore? real = null) : IAnh02ArtifactStore
    {
        public readonly Dictionary<string, byte[]> Objects = [];
        private readonly Dictionary<string, string> _mediaTypes = [];
        public bool LoseFirstAcknowledgement { get; init; }
        public int Writes { get; private set; }
        public string? CorruptReadKey { get; set; }
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default)
        { using var copy = new MemoryStream(); await content.CopyToAsync(copy, cancellationToken); var bytes = copy.ToArray(); var added = Objects.TryAdd(key, bytes); if (added) { Writes++; _mediaTypes[key] = mediaType; } Assert.Equal(Objects[key], bytes); if (real is not null) { using var put = new MemoryStream(bytes, false); await real.WriteAsync(key, put, bytes.Length, mediaType, cancellationToken); } if (added && LoseFirstAcknowledgement && Writes == 1) throw new IOException("Lost durable acknowledgement fixture"); return new(key, bytes.Length, AiManifestCanonicalizer.Hash(bytes), mediaType); }
        public async Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key == CorruptReadKey)
            {
                if (real is not null) { await using var observed = await real.OpenReadAsync(key, cancellationToken); Assert.True(observed.Metadata.SizeBytes > 0); }
                return new(new MemoryStream([0], false), new(key, Objects[key].Length, AiManifestCanonicalizer.Hash(Objects[key]), _mediaTypes[key]));
            }
            if (real is not null) return await real.OpenReadAsync(key, cancellationToken); if (!Objects.TryGetValue(key, out var bytes)) throw new FileNotFoundException(key); return new Anh02ArtifactRead(new MemoryStream(bytes, false), new(key, bytes.Length, AiManifestCanonicalizer.Hash(bytes), _mediaTypes[key]));
        }
    }
    private static async Task Login(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(login)).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Command(HttpClient client, string url, object? body, string? version = null, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", version);
        return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
    private sealed class SurveyVideoStorage(byte[] media, string checksum) : IUploadObjectStorage
    {
        public bool Corrupt { get; set; }
        public ArtifactFixture? Frames { get; init; }
        public Func<Task>? AfterFrameOpen { get; set; }
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => Task.FromResult("fixture-upload");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.test/{n}", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(media.Length, checksum, "video/mp4"));
        public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (objectKey.StartsWith("anh02/ai/", StringComparison.Ordinal) && Frames is not null)
            {
                var frame = await Frames.OpenReadAsync(objectKey, cancellationToken);
                if (AfterFrameOpen is not null) await AfterFrameOpen();
                return frame.Content;
            }
            return new MemoryStream(Corrupt ? new byte[media.Length] : media, false);
        }
    }
}
