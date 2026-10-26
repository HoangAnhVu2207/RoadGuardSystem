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
public sealed class ProcessingJobCommandBoundaryAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineProcessingJobCommandPreservesWireReplayAndAuthority()
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
            db.AIModelVersions.Add(AIModelVersion.Create(model, "Synthetic fixture", $"synthetic-job-command-{model:N}", "fixture://local", null, null, AIModelVersionStatus.Released, DateTimeOffset.UtcNow, supervisor.Id));
            await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM DefectTypes WHERE Code='CRACK') INSERT INTO DefectTypes(Code,Name,IsActive) VALUES ('CRACK','Crack',1)");
            await db.SaveChangesAsync();
        }

        var path = "/api/v1/processing-jobs";
        var input = new RoadGuardSystem.DTOs.Processing.CreateProcessingJobRequestDto(dataset, new(route, set, [segment], "SURFACE"), model, "fixture-preprocess.v1", "fixture-config.v1", "MOCK");
        async Task<string> Snapshot(bool includeReceipts = true)
        {
            await using var db = sql.CreateDbContext();
            var jobs = db.ProcessingJobs.Where(j => j.ProjectId == project);
            return JsonSerializer.Serialize(new
            {
                jobs = await jobs.AsNoTracking().OrderBy(j => j.Id).ToArrayAsync(),
                attempts = await db.ProcessingAttempts.Where(a => jobs.Select(j => j.Id).Contains(a.ProcessingJobId)).OrderBy(a => a.Id).ToArrayAsync(),
                receipts = includeReceipts ? await db.IdempotencyRecords.Where(r => r.ActorUserId == manager.Id && r.Operation == "ProcessingJobCreated").OrderBy(r => r.Id).ToArrayAsync() : [],
                audits = await db.AuditLogs.Where(a => jobs.Select(j => j.Id).Contains(a.EntityId)).OrderBy(a => a.Id).ToArrayAsync(),
                messages = await db.OutboxMessages.Where(o => jobs.Select(j => j.Id).Contains(o.CorrelationId!.Value)).OrderBy(o => o.Id).ToArrayAsync()
            });
        }
        async Task Denied(object request, HttpStatusCode status, string code, string? key = null)
        {
            var before = await Snapshot(); using var response = await Command(client, path, request, key: key); response.StatusCode.Should().Be(status);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json"); var error = await Body(response); error.GetProperty("status").GetInt32().Should().Be((int)status); error.GetProperty("code").GetString().Should().Be(code);
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue(); response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull(); (await Snapshot()).Should().Be(before);
        }
        await Denied(input with { DatasetId = Guid.NewGuid() }, HttpStatusCode.NotFound, "not_found");
        // A model-release conflict is a stored idempotent outcome, not a forbidden job write.
        var rejectedInput = input with { ModelVersionId = Guid.NewGuid() }; var rejectedKey = Guid.NewGuid().ToString(); var beforeConflict = await Snapshot(false);
        using (var conflict = await Command(client, path, rejectedInput, key: rejectedKey))
        { conflict.StatusCode.Should().Be(HttpStatusCode.Conflict); (await Body(conflict)).GetProperty("code").GetString().Should().Be("validation_error"); }
        (await Snapshot(false)).Should().Be(beforeConflict);
        await using (var db = sql.CreateDbContext())
            (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == manager.Id && r.Operation == "ProcessingJobCreated" && r.IdempotencyKey == rejectedKey)).Should().Be(1);
        var afterConflict = await Snapshot(); using (var replayConflict = await Command(client, path, rejectedInput, key: rejectedKey)) replayConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Snapshot()).Should().Be(afterConflict);
        var key = Guid.NewGuid().ToString();
        using var createdJob = await Command(client, path, input, key: key); createdJob.StatusCode.Should().Be(HttpStatusCode.Accepted, await createdJob.Content.ReadAsStringAsync());
        createdJob.Content.Headers.ContentType!.MediaType.Should().Be("application/json"); var value = await Body(createdJob); var job = value.GetProperty("id").GetGuid();
        value.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "id", "status", "resultId", "attemptNumber", "version", "jobType", "error" }.Order());
        value.GetProperty("status").GetString().Should().Be("QUEUED"); value.GetProperty("attemptNumber").GetInt32().Should().Be(1); value.GetProperty("resultId").ValueKind.Should().Be(JsonValueKind.Null); value.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null); value.GetProperty("jobType").GetString().Should().Be("AI_ANALYSIS");
        createdJob.Headers.Location!.OriginalString.Should().Be($"/api/v1/processing-jobs/{job}"); createdJob.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("version").GetString() + '"'); Guid.TryParse(createdJob.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
        await using (var db = sql.CreateDbContext())
        {
            var stored = await db.ProcessingJobs.SingleAsync(j => j.Id == job); stored.ProjectId.Should().Be(project); stored.ModelVersionId.Should().Be(model); stored.Mode.Should().Be("MOCK"); Convert.ToBase64String(stored.RowVersion).Should().Be(value.GetProperty("version").GetString());
            (await db.ProcessingAttempts.CountAsync(a => a.ProcessingJobId == job)).Should().Be(1);
            (await db.OutboxMessages.CountAsync(o => o.CorrelationId!.Value == job && o.MessageType == "processing_job.dispatch")).Should().Be(1);
            (await db.AuditLogs.CountAsync(a => a.EntityId == job && a.ActorUserId == manager.Id && a.EventType == "processing_job_created")).Should().Be(1);
        }
        await Login(client, supervisor.UserName!);
        using (var supervisorJob = await Command(client, path, input))
        { supervisorJob.StatusCode.Should().Be(HttpStatusCode.Accepted); (await Body(supervisorJob)).GetProperty("status").GetString().Should().Be("QUEUED"); }
        await Login(client, manager.UserName!);
        var committed = await Snapshot(); using var replay = await Command(client, path, input, key: key); replay.StatusCode.Should().Be(HttpStatusCode.Accepted); (await replay.Content.ReadAsStringAsync()).Should().Be(await createdJob.Content.ReadAsStringAsync()); replay.Headers.Location.Should().Be(createdJob.Headers.Location); replay.Headers.ETag.Should().Be(createdJob.Headers.ETag); (await Snapshot()).Should().Be(committed);
        await Denied(input with { ConfigVersion = "changed" }, HttpStatusCode.Conflict, "duplicate_request", key);
        foreach (var role in new[] { UserRoleCode.Reporter, UserRoleCode.RepairCrew, UserRoleCode.DroneOperator })
        {
            var outsider = await sql.CreateUserAsync("job-command-" + Guid.NewGuid().ToString("N"), "Current1!", role); await Login(client, outsider.UserName!); await Denied(input, HttpStatusCode.Forbidden, "access_forbidden");
            var before = await Snapshot(); using var retry = await Command(client, $"/api/v1/processing-jobs/{job}/retry", new { reason = "retry forbidden" }, value.GetProperty("version").GetString());
            retry.StatusCode.Should().Be(HttpStatusCode.Forbidden); (await Body(retry)).GetProperty("code").GetString().Should().Be("access_forbidden"); (await Snapshot()).Should().Be(before);
        }
        var pmOther = await sql.CreateUserAsync("job-command-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager); await Login(client, pmOther.UserName!); await Denied(input, HttpStatusCode.Forbidden, "access_forbidden");
        await Login(client, manager.UserName!); var credential = client.DefaultRequestHeaders.Authorization; var sid = Guid.Parse(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(credential!.Parameter).Claims.Single(c => c.Type == "sid").Value);
        client.DefaultRequestHeaders.Authorization = null; await Denied(input, HttpStatusCode.Unauthorized, "auth_unauthorized"); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "tampered"); await Denied(input, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new RoadGuardSystem.Services.Factories.AccessTokenFactory(factory.Services.GetRequiredService<IOptions<RoadGuardSystem.Services.Options.JwtOptions>>().Value).Create(manager.Id, sid, UserRoleCode.ProjectManager, DateTimeOffset.UtcNow.AddMinutes(-20), 15)); await Denied(input, HttpStatusCode.Unauthorized, "auth_unauthorized");
        client.DefaultRequestHeaders.Authorization = credential;
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == project && m.UserId == manager.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        await Denied(input, HttpStatusCode.Forbidden, "access_forbidden", key); await Denied(input, HttpStatusCode.Forbidden, "access_forbidden");
        await using (var db = sql.CreateDbContext()) await db.Sessions.Where(r => r.Id == sid).ExecuteUpdateAsync(u => u.SetProperty(r => r.RevokedAt, DateTimeOffset.UtcNow));
        await Denied(input, HttpStatusCode.Unauthorized, "auth_session_revoked", key); (await Snapshot()).Should().Be(committed);
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
