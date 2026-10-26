using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reporting;

using System.IO.Compression;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Devices;

// Actual reporting capture and worker rendering, with domain-valid persisted survey and Reporter-derived Case/Defect sources.
// Synthetic bytes are not a claim of drone/video semantic validation or Reporter-original export policy.
public sealed class SourceBackedExportHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CapturedDossierOriginalAndCaseFactsRemainFrozenAcrossHttpStatesAndSourceRevision(bool interruptWrite)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var image = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE") ?? throw new InvalidOperationException("Approved disposable MinIO image required.");
        var owner = Guid.NewGuid().ToString("N"); var access = "sbx" + owner[..12]; var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var minio = new ContainerBuilder(image).WithLabel("roadguard.sandbox.owner", owner)
            .WithLabel("roadguard.sandbox.mode", "task23-test").WithEnvironment("MINIO_ROOT_USER", access)
            .WithEnvironment("MINIO_ROOT_PASSWORD", secret).WithCommand("server", "/data").WithPortBinding(9000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/minio/health/ready"),
                m => m.WithTimeout(TimeSpan.FromSeconds(30)))).Build();
        await minio.StartAsync();
        var endpoint = $"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}"; var bucket = "sbx-" + owner;
        using var s3 = new AmazonS3Client(access, secret, new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        var options = Options.Create(new MinioStorageOptions { Endpoint = endpoint, BucketName = bucket, AccessKey = access, SecretKey = secret });
        var store = new CountingStore(new MinioAnh02ArtifactStore(options)); var clock = new TestClock();
        var now = clock.GetUtcNow();
        GenuineProtectedSource.Source source;
        var original = System.Text.Encoding.UTF8.GetBytes("TEST_ONLY private survey original");
        var originalHash = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();
        var fileId = Guid.NewGuid(); var originalKey = "private/survey/" + fileId.ToString("N");
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = originalKey,
            InputStream = new MemoryStream(original),
            ContentType = "video/mp4"
        });
        Guid datasetId;
        await using (var db = sql.CreateDbContext())
        {
            source = await GenuineProtectedSource.Seed(db, sql);
            var defectVersion = Convert.ToBase64String(await db.Defects.Where(x => x.Id == source.Defect).Select(x => EF.Property<byte[]>(x, "RowVersion")).SingleAsync());
            var field = await new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ExecuteAsync(
                new(source.Project, null, "create", new FieldTaskCreateInputFact(source.Defect, defectVersion, null, "REPORTER", source.Route, source.Set, null, null, "PRE_MEASUREMENT", 1, "{}", "task23 FIELD source", source.Crew, now.AddDays(1)),
                    Guid.NewGuid().ToString("N"), null, new(source.Pm, UserRoleCode.ProjectManager, source.Pm, "DIRECT", true)), _ => Task.FromResult(true), default);
            Assert.Equal(201, field.Status);
            var file = StoredFile.Create(fileId, originalKey, "synthetic.mp4", "video/mp4", original.Length, originalHash, source.Pm, now, null);
            db.AddRange(file, FileScope.Create(Guid.NewGuid(), fileId, source.Project, null, source.Pm, "SURVEY_VIDEO", now));
            await db.SaveChangesAsync();
            var upload = UploadSession.Create(Guid.NewGuid(), fileId, source.Pm, originalKey, "SURVEY_VIDEO", "video/mp4", original.Length, originalHash, 8388608, now.AddHours(1));
            upload.StartUploading("owned-fixture", now); db.Add(upload); await db.SaveChangesAsync();
            upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
            var request = SurveyRequest.Create(Guid.NewGuid(), source.Project, source.Road, null, source.Pm, SurveyType.Periodic, SurveyRequestStatus.Accepted, now);
            request.SetBandScope();
            var survey = Survey.Create(Guid.NewGuid(), request.Id, source.Project, source.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
            var device = DroneDevice.Create(Guid.NewGuid(), "TEST-" + owner, DroneDeviceStatus.Active);
            var segment = await db.RoadSegments.Where(s => s.SegmentSetId == source.Set).Select(s => s.Id).FirstAsync();
            var dataset = SurveyDataVersion.CreateSubmitted(Guid.NewGuid(), survey.Id, 1, now, now, device.Id,
                JsonSerializer.Serialize(new[] { new { fileId, checksumSha256 = originalHash, sizeBytes = original.Length, mediaType = "video/mp4", purpose = "SURVEY_VIDEO" } }),
                JsonSerializer.Serialize(new[] { new { routeVersionId = source.Route, segmentSetId = source.Set, segmentIds = new[] { segment }, targetBand = "SURFACE" } }));
            datasetId = dataset.Id; db.AddRange(request, survey, device, dataset); await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.Configure<ExportOptions>(o => o.UnicodeFontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"));
            services.AddSingleton<IAnh02ArtifactStore>(store); services.AddSingleton<TimeProvider>(clock);
            services.AddSingleton<IUploadObjectStorage>(new MinioUploadObjectStorage(options));
        });
        using var client = factory.CreateClient();
        string email; await using (var db = sql.CreateDbContext()) email = (await db.Users.SingleAsync(u => u.Id == source.Pm)).Email!;
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        Guid jobId; string path; string frozenJson; string frozenHash;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            var admitted = await service.CreateAsync(source.Pm, source.Project, new("DOSSIER", "ZIP", IncludeOriginalFiles: true), "task23-" + owner, null, default);
            Assert.Equal("success", admitted.Code); Assert.NotNull(admitted.Value); jobId = admitted.Value.Id;
        }
        path = $"/api/v1/projects/{source.Project}/exports/{jobId}";
        await using (var db = sql.CreateDbContext())
        {
            var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId);
            var snap = await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId);
            frozenJson = snap.PayloadJson; frozenHash = snap.Hash;
        }
        var payload = JsonSerializer.Deserialize<ExportSnapshotPayloadDto>(frozenJson, ExportSerialization.Options)!;
        Assert.NotNull(payload.Dossier); Assert.NotEmpty(payload.Dossier.Items);
        Assert.NotNull(payload.Dossier.CaseDefectFacts); Assert.NotEmpty(payload.Dossier.CaseDefectFacts.Cases);
        var manifestFile = Assert.Single(payload.Manifest.Files); Assert.Equal(fileId, manifestFile.FileId); Assert.True(manifestFile.Included);
        Assert.Equal(originalHash, manifestFile.Sha256); Assert.Contains(payload.Manifest.SourceRevisions, r => r.Id == datasetId);
        async Task State(string status, HttpStatusCode content)
        {
            byte[] version; int audits;
            await using (var db = sql.CreateDbContext()) { version = (await db.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == jobId)).RowVersion; audits = await db.Set<AuditLog>().CountAsync(a => a.EntityId == jobId); }
            var get = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var view = (await get.Content.ReadFromJsonAsync<ExportJobViewDto>())!; Assert.True(status == view.Status, $"Expected {status}; actual {view.Status}; error {view.ErrorCode}");
            var m = await client.GetAsync(path + "/manifest"); Assert.Equal(HttpStatusCode.OK, m.StatusCode);
            var manifest = (await m.Content.ReadFromJsonAsync<ExportManifestV1Dto>())!; Assert.Equal(frozenHash, manifest.SnapshotHash);
            Assert.NotEmpty(manifest.SourceRevisions); Assert.Equal(fileId, Assert.Single(manifest.Files).FileId);
            Assert.DoesNotContain(originalKey, await m.Content.ReadAsStringAsync());
            Assert.Equal(source.Project, view.ProjectId); Assert.Equal("DOSSIER", view.Kind); Assert.Equal("ZIP", view.Format);
            Assert.Equal("\"" + view.Version + "\"", get.Headers.ETag!.ToString());
            Assert.Equal("\"" + frozenHash + "\"", m.Headers.ETag!.ToString());
            Assert.Equal(content, (await client.GetAsync(path + "/content")).StatusCode);
            await using var db2 = sql.CreateDbContext(); Assert.Equal(version, (await db2.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == jobId)).RowVersion);
            Assert.Equal(audits, await db2.Set<AuditLog>().CountAsync(a => a.EntityId == jobId));
        }
        await State("QUEUED", HttpStatusCode.Conflict);
        var lease = Guid.NewGuid();
        await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId); Assert.True(job.TryClaim(lease, now, TimeSpan.FromMinutes(1))); await db.SaveChangesAsync(); }
        await State("RUNNING", HttpStatusCode.Conflict);
        await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId); job.Fail(lease, now, "SYNTHETIC_STATE", true); await db.SaveChangesAsync(); }
        await State("FAILED", HttpStatusCode.Conflict);
        // A second admitted job uses real claim/render/upload/complete rather than manually declaring success.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            var second = await service.CreateAsync(source.Pm, source.Project, new("DOSSIER", "ZIP", IncludeOriginalFiles: true), "ready-" + owner, null, default);
            Assert.Equal("success", second.Code); jobId = second.Value!.Id;
            store.FailWriteOnce = interruptWrite;
            Assert.True(await service.ProcessNextAsync(default));
            if (interruptWrite)
            {
                await using var db = sql.CreateDbContext(); var interrupted = await db.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == jobId);
                Assert.Equal("QUEUED", interrupted.Status); Assert.Equal("export_storage_unavailable", interrupted.ErrorCode); Assert.Null(interrupted.ArtifactId);
                Assert.Empty(await db.Set<GeneratedArtifact>().Where(a => a.ExportJobId == jobId).ToListAsync());
                clock.Now = now.AddMinutes(2);
            }
        }
        if (interruptWrite)
        {
            await using var restartedScope = factory.Services.CreateAsyncScope();
            Assert.True(await restartedScope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        }
        path = $"/api/v1/projects/{source.Project}/exports/{jobId}";
        await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId); var snap = await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId); frozenJson = snap.PayloadJson; frozenHash = snap.Hash; }
        await State("SUCCEEDED", HttpStatusCode.OK);
        var response = await client.GetAsync(path + "/content"); var archiveBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("\"" + Convert.ToHexString(SHA256.HashData(archiveBytes)).ToLowerInvariant() + "\"", response.Headers.ETag!.ToString());
        using (var zip = new ZipArchive(new MemoryStream(archiveBytes), ZipArchiveMode.Read))
        {
            var entry = zip.GetEntry(manifestFile.ArchivePath!); Assert.NotNull(entry); using var bytes = new MemoryStream(); await entry.Open().CopyToAsync(bytes); Assert.Equal(original, bytes.ToArray());
            using var manifestStream = zip.GetEntry("manifest.json")!.Open();
            var embedded = (await JsonSerializer.DeserializeAsync<ExportManifestV1Dto>(manifestStream, ExportSerialization.Options))!;
            Assert.Equal(frozenHash, embedded.SnapshotHash); Assert.Equal(originalHash, Assert.Single(embedded.Files).Sha256);
            using var pdf = zip.GetEntry("dossier.pdf")!.Open(); var signature = new byte[4]; Assert.Equal(4, await pdf.ReadAsync(signature)); Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(signature));
        }
        // A legitimate later request revision must not rewrite historical snapshot bytes/version refs.
        await using (var db = sql.CreateDbContext()) { var request = await db.SurveyRequests.FirstAsync(r => r.ProjectId == source.Project); request.ChangeDueAt(now.AddDays(2)); await db.SaveChangesAsync(); }
        Assert.Equal(archiveBytes, await client.GetByteArrayAsync(path + "/content"));
        await using (var db = sql.CreateDbContext())
        {
            var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId); var snap = await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId); Assert.Equal(frozenJson, snap.PayloadJson); Assert.Equal(frozenHash, snap.Hash); Assert.Single(await db.Set<GeneratedArtifact>().Where(a => a.ExportJobId == jobId).ToListAsync());
            var events = await db.Set<AuditLog>().Where(a => a.EntityId == jobId).Select(a => a.EventType).ToListAsync();
            Assert.Equal(1, events.Count(e => e == "Anh02.Export.Admitted")); Assert.Equal(1, events.Count(e => e == "Anh02.Export.Completed"));
        }
        // Current membership revocation blocks new indirect reads; stored archive is retained.
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        var reads = store.Reads; foreach (var suffix in new[] { "", "/content", "/manifest" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + suffix)).StatusCode);
        Assert.Equal(reads, store.Reads);
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Active));
        clock.Now = now.AddDays(31); await State("SUCCEEDED", HttpStatusCode.Gone);
    }
    private sealed class TestClock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class CountingStore(IAnh02ArtifactStore inner) : IAnh02ArtifactStore
    {
        public int Reads; public bool FailWriteOnce;
        public Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken ct = default)
        {
            if (FailWriteOnce) { FailWriteOnce = false; throw new IOException("SYNTHETIC artifact PUT interrupted after real rendering"); }
            return inner.WriteAsync(key, content, sizeBytes, mediaType, ct);
        }
        public async Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Reads); return await inner.OpenReadAsync(key, ct);
        }
    }
}
