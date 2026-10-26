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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Data.SqlClient;
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
public sealed class AiCandidateLifecycleAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task GenuineCandidateRejectsProvenanceDriftAndNoncurrentGeometry()
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
            db.AIModelVersions.Add(AIModelVersion.Create(model, "Synthetic fixture", $"synthetic-road-{model:N}", "fixture://local", null, null, AIModelVersionStatus.Released, DateTimeOffset.UtcNow, supervisor.Id));
            await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM DefectTypes WHERE Code='CRACK') INSERT INTO DefectTypes(Code,Name,IsActive) VALUES ('CRACK','Crack',1)");
            await db.SaveChangesAsync();
        }
        string geometryVersion;
        using (var capture = factory.Services.CreateScope())
        {
            var geometry = await capture.ServiceProvider.GetRequiredService<IAnhHuyProducerService>().ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, set);
            Assert.Equal(AnhHuyProducerStatus.Ready, geometry.Status); geometryVersion = geometry.Facts!.Version;
        }
        var input = new RoadGuardSystem.DTOs.Processing.CreateAiMockRunRequest(dataset, new(route, set, [segment], "SURFACE"), model, "fixture-preprocess.v1", "fixture-config.v1", "synthetic-road-v1", "VIDEO_ANALYSIS", geometryVersion);
        var path = $"/api/v1/projects/{project}/ai-mock-runs"; var key = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Command(client, path, input, key: new string('k', 201))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Command(client, path, input with { ModelVersionId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Command(client, path, input with { Scope = input.Scope with { SegmentIds = [Guid.NewGuid()] } })).StatusCode);
        var admitted = await Command(client, path, input, key: key); admitted.StatusCode.Should().Be(HttpStatusCode.Accepted, await admitted.Content.ReadAsStringAsync());
        var run = (await Body(admitted)).GetProperty("id").GetGuid();
        using (var worker = factory.Services.CreateScope()) (await worker.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default)).Should().BeTrue();
        using var resultResponse = await client.GetAsync(path + $"/{run}/result"); resultResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await Body(resultResponse); var detectionId = result.GetProperty("detections")[0].GetProperty("detectionId").GetGuid();
        var candidatePath = $"/api/v1/projects/{project}/ai-candidates/{detectionId}";
        async Task Denied(string url, HttpStatusCode status, string code)
        {
            using var response = await client.GetAsync(url); response.StatusCode.Should().Be(status);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json"); var error = await Body(response);
            error.GetProperty("status").GetInt32().Should().Be((int)status); error.GetProperty("code").GetString().Should().Be(code);
            Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue(); response.Headers.ETag.Should().BeNull(); response.Headers.Location.Should().BeNull();
        }
        async Task<JsonElement> Read()
        {
            using var response = await client.GetAsync(candidatePath); response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json"); response.Headers.Location.Should().BeNull(); Guid.TryParse(response.Headers.GetValues("X-Correlation-ID").Single(), out _).Should().BeTrue();
            var value = await Body(response);
            value.EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "schemaVersion", "detectionId", "projectId", "datasetVersionId", "modelVersionId", "runId", "jobId", "attemptId", "resultId", "manifestHash", "resultHash", "fixtureVersion", "mode", "sourceVideoFileId", "sourceVideoFileVersion", "frameFileId", "frameFileVersion", "frameSha256", "frameSizeBytes", "frameMediaType", "timestampMs", "typeCode", "confidence", "boundingBox", "routeVersionId", "segmentSetId", "segmentId", "geometryVersion", "positionStatus", "sourceVersion", "disposition", "dispositionVersion" }.Order());
            value.GetProperty("boundingBox").EnumerateObject().Select(p => p.Name).Order().Should().Equal(new[] { "x", "y", "width", "height" }.Order());
            value.GetProperty("detectionId").GetGuid().Should().Be(detectionId); value.GetProperty("projectId").GetGuid().Should().Be(project); value.GetProperty("datasetVersionId").GetGuid().Should().Be(dataset); value.GetProperty("modelVersionId").GetGuid().Should().Be(model); value.GetProperty("runId").GetGuid().Should().Be(run);
            foreach (var field in new[] { "jobId", "attemptId", "manifestHash", "resultHash" }) value.GetProperty(field).GetRawText().Should().Be(result.GetProperty(field).GetRawText());
            value.GetProperty("sourceVideoFileId").GetGuid().Should().Be(video); value.GetProperty("mode").GetString().Should().Be("MOCK/SYNTHETIC"); value.GetProperty("routeVersionId").GetGuid().Should().Be(route); value.GetProperty("segmentSetId").GetGuid().Should().Be(set); value.GetProperty("segmentId").GetRawText().Should().Be(result.GetProperty("detections")[0].GetProperty("segmentId").GetRawText()); value.GetProperty("positionStatus").GetString().Should().Be("UNKNOWN"); value.GetProperty("geometryVersion").GetString().Should().Be(geometryVersion);
            response.Headers.ETag!.Tag.Should().Be('"' + value.GetProperty("sourceVersion").GetString() + '"');
            foreach (var forbidden in new[] { "storageUri", "storageKey", "objectKey", "password", "accessToken", "email", "phoneNumber", "fixture://", "https://storage.test" }) value.GetRawText().Should().NotContain(forbidden);
            return value;
        }
        var view = await Read();
        var frameId = view.GetProperty("frameFileId").GetGuid();
        var jobId = view.GetProperty("jobId").GetGuid();
        var attemptId = view.GetProperty("attemptId").GetGuid();
        var resultId = view.GetProperty("resultId").GetGuid();
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                run = await db.Set<AiMockRun>().AsNoTracking().SingleAsync(r => r.Id == run),
                result = await db.Set<AiResultProvenance>().AsNoTracking().SingleAsync(r => r.Id == resultId),
                proof = await db.Set<AiDetectionProvenance>().AsNoTracking().SingleAsync(r => r.DetectionId == detectionId),
                frame = await db.Files.AsNoTracking().SingleAsync(r => r.Id == frameId),
                source = await db.Files.AsNoTracking().SingleAsync(r => r.Id == video),
                job = await db.ProcessingJobs.AsNoTracking().SingleAsync(r => r.Id == jobId),
                attempt = await db.ProcessingAttempts.AsNoTracking().SingleAsync(r => r.Id == attemptId),
                geometry = await db.RoadSectionVersions.AsNoTracking().Where(r => r.Id == route)
                    .Select(r => new { r.Id, r.IsCurrent, wkt = r.Geometry.AsText(), r.VersionNo, r.EffectiveFrom, r.ChangeReason, r.CrsProfileRevisionId }).SingleAsync(),
                receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.ProjectId == project).OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(r => r.ActorUserId == manager.Id).OrderBy(r => r.Id).Select(r => r.Id).ToArrayAsync()
            });
        }
        async Task WriteFault<T>(RoadGuardSystem.Repositories.RoadGuardDbContext db, string property, object? value, Guid id) where T : class
        {
            var entity = db.Model.FindEntityType(typeof(T))!;
            var table = entity.GetTableName()!; var schema = entity.GetSchema() ?? "dbo";
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            var column = entity.FindProperty(property)!.GetColumnName(store)!;
            var keyColumn = Assert.Single(entity.FindPrimaryKey()!.Properties).GetColumnName(store)!;
            // All identifiers come from the installed EF model, values are SQL parameters.
            foreach (var name in new[] { schema, table, column, keyColumn }) Assert.Matches("^[A-Za-z0-9_]+$", name);
            var faultCommand = $"UPDATE [{schema}].[{table}] SET [{column}]={{0}} WHERE [{keyColumn}]={{1}}";
            Assert.Equal(1, await db.Database.ExecuteSqlRawAsync(faultCommand, value!, id));
        }
        // These are deliberate corruptions in the disposable fixture, never production transitions.
        async Task Fault<T>(Guid id, string property, object value, int? sqlDenial = null) where T : class
        {
            if (sqlDenial is { } errorNumber)
            {
                var unchanged = await Snapshot(); var writeCount = artifacts.Writes;
                await using (var guarded = sql.CreateDbContext())
                {
                    var denied = await Assert.ThrowsAsync<SqlException>(() => WriteFault<T>(guarded, property, value, id));
                    Assert.Equal(errorNumber, denied.Number);
                }
                Assert.Equal(unchanged, await Snapshot()); Assert.Equal(writeCount, artifacts.Writes);
                await Read();
                return;
            }
            object? original;
            await using (var db = sql.CreateDbContext())
            {
                var row = await db.Set<T>().FindAsync(id); Assert.NotNull(row);
                var entry = db.Entry(row).Property(property); original = entry.CurrentValue;
                await WriteFault<T>(db, property, value, id);
            }
            try
            {
                var before = await Snapshot(); var writes = artifacts.Writes;
                await Denied(candidatePath, HttpStatusCode.Conflict, "source_not_ready");
                Assert.Equal(before, await Snapshot()); Assert.Equal(writes, artifacts.Writes);
            }
            finally
            {
                await using var db = sql.CreateDbContext(); var row = await db.Set<T>().FindAsync(id);
                Assert.NotNull(row); await WriteFault<T>(db, property, original, id);
            }
            await Read();
        }
        await Fault<AiMockRun>(run, "ManifestHash", new string('0', 64), 51221);
        await Fault<AiResultProvenance>(resultId, "ResultHash", new string('0', 64), 51220);
        await Fault<AiResultProvenance>(resultId, "CanonicalResult", "{}", 51220);
        await Fault<AiDetectionProvenance>(detectionId, "FrameFileVersion", new string('0', 64), 51220);
        await Fault<AiDetectionProvenance>(detectionId, "TimestampMilliseconds", 1L, 51220);
        await Fault<AiDetectionProvenance>(detectionId, "BoxWidth", 0.01m, 51220);
        await Fault<StoredFile>(frameId, "Checksum", new string('0', 64), 51020);
        await Fault<StoredFile>(frameId, "MimeType", "application/octet-stream", 51020);
        await Fault<StoredFile>(video, "SizeBytes", 1L, 51020);
        await Fault<ProcessingJob>(jobId, "Mode", "REAL");
        // Deliberate noncurrent geometry is last: rowversion changes must not be hidden by restoring old hashes.
        await using (var db = sql.CreateDbContext())
        {
            var geometry = await db.RoadSectionVersions.SingleAsync(r => r.Id == route);
            geometry.ClearCurrent(); await db.SaveChangesAsync();
        }
        var stale = await Snapshot(); var priorWrites = artifacts.Writes;
        await Denied(candidatePath, HttpStatusCode.Conflict, "stale_geometry");
        Assert.Equal(stale, await Snapshot()); Assert.Equal(priorWrites, artifacts.Writes);
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
