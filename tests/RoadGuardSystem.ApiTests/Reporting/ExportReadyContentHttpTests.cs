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

// Controlled persisted TRAINING snapshot isolates GET contracts. It does not prove
// label capture/admission, a private-original authority policy, or worker crash recovery.
public sealed class ExportReadyContentHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Fact]
    public async Task ReadyHttpChecksBytesCurrentAuthorityStorageFailureAndRecoveryWithoutSqlMutation()
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var image = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE") ?? throw new InvalidOperationException("Approved disposable MinIO image required.");
        var owner = Guid.NewGuid().ToString("N"); var access = "sbx" + owner[..12]; var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var minio = new ContainerBuilder(image).WithLabel("roadguard.sandbox.owner", owner)
            .WithLabel("roadguard.sandbox.mode", "task22-test").WithEnvironment("MINIO_ROOT_USER", access)
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
        var pm = await sql.CreateUserAsync("ready-pm-" + owner, "Current1!", UserRoleCode.ProjectManager);
        var sup = await sql.CreateUserAsync("ready-sup-" + owner, "Current1!", UserRoleCode.Supervisor);
        var outsider = await sql.CreateUserAsync("ready-other-" + owner, "Current1!", UserRoleCode.ProjectManager);
        var project = Project.Create(Guid.NewGuid(), owner, "TEST_ONLY A", null, null, null, null, now);
        var otherProject = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "TEST_ONLY B", null, null, null, null, now);
        ProjectMember Member(Guid actor, Guid p, UserRoleCode role) => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = p,
            UserId = actor,
            RoleCode = role,
            Status = ProjectMemberStatus.Active,
            ValidFrom = DateOnly.FromDateTime(now.UtcDateTime).AddDays(-1)
        };
        var pmMember = Member(pm.Id, project.Id, UserRoleCode.ProjectManager);
        var snapshotId = Guid.NewGuid();
        var payload = new ExportSnapshotPayloadDto(new("anh02.export.v1", snapshotId, project.Id, "TRAINING", "ZIP", pm.Id, now, now,
            [], new("TRAINING", "ZIP"), [], [], [], [], ""), null);
        var json = JsonSerializer.Serialize(payload, ExportSerialization.Options);
        var snapshot = ExportSnapshot.Create(snapshotId, project.Id, json, ExportSerialization.Hash(json), now);
        var job = ExportJob.Create(Guid.NewGuid(), project.Id, pm.Id, snapshotId, "TRAINING", "ZIP", now);
        var renderer = new ExportRenderer(Options.Create(new ExportOptions()));
        await using var rendered = await renderer.RenderAsync(payload, (_, _) => throw new InvalidOperationException("No source originals in this fixture."), _ => Task.CompletedTask, default);
        using var copy = new MemoryStream(); await rendered.CopyToAsync(copy); var expected = copy.ToArray();
        var key = $"anh02/exports/{job.Id:D}/{snapshot.Hash}.zip";
        await using var input = new MemoryStream(expected); var metadata = await store.WriteAsync(key, input, expected.Length, "application/zip");
        var file = StoredFile.Create(Guid.NewGuid(), key, "training.zip", "application/zip", metadata.SizeBytes, metadata.Sha256, pm.Id, now, null);
        var artifact = GeneratedArtifact.Create(Guid.NewGuid(), job.Id, snapshot.Id, file.Id, now);
        var lease = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(project, otherProject, pmMember, Member(sup.Id, project.Id, UserRoleCode.Supervisor), Member(outsider.Id, otherProject.Id, UserRoleCode.ProjectManager), snapshot);
            await db.SaveChangesAsync();
            // FK cycle is completed exactly as production persistence: job -> file/artifact -> job artifact reference.
            db.Add(job); await db.SaveChangesAsync();
            db.AddRange(file, artifact); await db.SaveChangesAsync();
            Assert.True(job.TryClaim(lease, now, TimeSpan.FromMinutes(1))); job.Complete(lease, artifact.Id, now); await db.SaveChangesAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.AddSingleton<IAnh02ArtifactStore>(store); services.AddSingleton<TimeProvider>(clock); });
        using var client = factory.CreateClient(); using var supervisor = factory.CreateClient(); using var other = factory.CreateClient(); using var anonymous = factory.CreateClient();
        async Task Login(HttpClient c, string email)
        {
            var response = await c.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        }
        await Login(client, pm.Email!); await Login(supervisor, sup.Email!); await Login(other, outsider.Email!);
        var path = $"/api/v1/projects/{project.Id}/exports/{job.Id}";
        await CollectionAssertions.Contains(client, $"/api/v1/projects/{project.Id}/exports", job.Id, "id", "projectId", "kind", "format", "status", "createdAt", "completedAt", "expiresAt", "errorCode", "completeness", "missingSections", "version");
        var supervisorHistory = await supervisor.GetAsync($"/api/v1/projects/{project.Id}/exports");
        Assert.Equal(HttpStatusCode.OK, supervisorHistory.StatusCode);
        Assert.Empty((await supervisorHistory.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
        async Task<byte[]> State()
        {
            await using var db = sql.CreateDbContext(); var row = await db.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == job.Id);
            Assert.Equal("SUCCEEDED", row.Status); Assert.Equal(artifact.Id, row.ArtifactId); Assert.Equal(snapshot.Hash, (await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(s => s.Id == snapshot.Id)).Hash);
            Assert.Single(await db.Set<GeneratedArtifact>().Where(a => a.ExportJobId == job.Id).ToListAsync()); return row.RowVersion;
        }
        var initial = await State();
        foreach (var suffix in new[] { "", "/manifest", "/content" })
        {
            var reads = store.Reads;
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync(path + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/projects/{otherProject.Id}/exports/{job.Id}" + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/projects/{project.Id}/exports/{Guid.NewGuid()}" + suffix)).StatusCode);
            Assert.Equal(reads, store.Reads);
            Assert.Equal(HttpStatusCode.OK, (await supervisor.GetAsync(path + suffix)).StatusCode);
        }
        foreach (var role in new[] { UserRoleCode.DroneOperator, UserRoleCode.RepairCrew, UserRoleCode.Reporter })
        {
            var user = await sql.CreateUserAsync("ready-role-" + Guid.NewGuid().ToString("N"), "Current1!", role);
            await using (var db = sql.CreateDbContext()) { db.Add(Member(user.Id, project.Id, role)); await db.SaveChangesAsync(); }
            using var denied = factory.CreateClient(); await Login(denied, user.Email!); var reads = store.Reads;
            foreach (var suffix in new[] { "", "/manifest", "/content" }) Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync(path + suffix)).StatusCode);
            Assert.Equal(reads, store.Reads);
        }
        var detail = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        var view = (await detail.Content.ReadFromJsonAsync<ExportJobViewDto>())!; Assert.Equal(job.Id, view.Id); Assert.Equal("SUCCEEDED", view.Status);
        Assert.Equal("\"" + view.Version + "\"", detail.Headers.ETag!.ToString());
        var manifest = await client.GetAsync(path + "/manifest"); Assert.Equal(HttpStatusCode.OK, manifest.StatusCode);
        Assert.Equal("\"" + snapshot.Hash + "\"", manifest.Headers.ETag!.ToString()); Assert.DoesNotContain(key, await manifest.Content.ReadAsStringAsync());
        async Task Ready()
        {
            var response = await client.GetAsync(path + "/content"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/zip", response.Content.Headers.ContentType!.MediaType); Assert.Contains("training.zip", response.Content.Headers.ContentDisposition!.ToString());
            Assert.Equal("\"" + metadata.Sha256 + "\"", response.Headers.ETag!.ToString()); Assert.True(response.Headers.CacheControl!.NoStore); Assert.True(response.Headers.CacheControl.Private);
            Assert.Equal(expected, await response.Content.ReadAsByteArrayAsync()); Assert.Equal(initial, await State());
        }
        async Task Restore()
        {
            // Only the exact synthetic object in this disposable bucket is restored.
            await s3.DeleteObjectAsync(bucket, key); await using var bytes = new MemoryStream(expected);
            await store.WriteAsync(key, bytes, expected.Length, "application/zip");
        }
        await Ready(); await Ready();
        // Deterministic read-side transport failure, not a claim of process crash or durable cleanup.
        store.FailRead = true;
        var unavailable = await client.GetAsync(path + "/content"); Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal("export_storage_unavailable", (await unavailable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(initial, await State()); store.FailRead = false; await Ready();
        await s3.DeleteObjectAsync(bucket, key);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path + "/content")).StatusCode); Assert.Equal(initial, await State());
        await Restore(); await Ready();
        var corrupt = new PutObjectRequest { BucketName = bucket, Key = key, InputStream = new MemoryStream([1, 2, 3]), ContentType = "application/zip" };
        corrupt.Metadata["anh02-sha256"] = metadata.Sha256; corrupt.Metadata["anh02-size"] = metadata.SizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await s3.PutObjectAsync(corrupt);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path + "/content")).StatusCode); Assert.Equal(initial, await State());
        await Restore(); await Ready();
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.Id == pmMember.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        var count = store.Reads;
        foreach (var suffix in new[] { "", "/manifest", "/content" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + suffix)).StatusCode);
        Assert.Equal(count, store.Reads); Assert.Equal(initial, await State());
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.Id == pmMember.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Active));
        store.AfterRead = async () =>
        {
            await using var db = sql.CreateDbContext();
            await db.ProjectMembers.Where(m => m.Id == pmMember.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + "/content")).StatusCode); Assert.Equal(initial, await State());
        store.AfterRead = null;
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.Id == pmMember.Id).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Active));
        store.AfterRead = () => { clock.Now = now.AddDays(31); return Task.CompletedTask; };
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path + "/content")).StatusCode); Assert.Equal(initial, await State());
        store.AfterRead = null;
        clock.Now = now.AddDays(31); count = store.Reads;
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path + "/content")).StatusCode); Assert.Equal(count, store.Reads); Assert.Equal(initial, await State());
        var inventory = await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket }); Assert.Single(inventory.S3Objects); Assert.Equal(key, inventory.S3Objects[0].Key);
    }
    private sealed class TestClock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class CountingStore(IAnh02ArtifactStore inner) : IAnh02ArtifactStore
    {
        public int Reads; public bool FailRead; public Func<Task>? AfterRead;
        public Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken ct = default) => inner.WriteAsync(key, content, sizeBytes, mediaType, ct);
        public async Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Reads); if (FailRead) throw new HttpRequestException("SYNTHETIC transport interruption");
            var read = await inner.OpenReadAsync(key, ct);
            try { if (AfterRead is not null) await AfterRead(); return read; }
            catch { await read.DisposeAsync(); throw; }
        }
    }
}
