using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Exports;

public sealed class Anh02ExportHttpTests : IAsyncLifetime
{
    // Global workers deliberately claim across projects. Each test therefore needs its own durable queue and storage fixture.
    private readonly AuthenticationSqlServerFixture sql = new();
    public Task InitializeAsync() => sql.InitializeAsync();
    public Task DisposeAsync() => sql.DisposeAsync();
    [Fact]
    public async Task Two_reporters_private_evidence_is_never_promoted_by_include_originals()
    {
        var manager = await sql.CreateUserAsync("export-privacy-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var first = await sql.CreateUserAsync("export-reporter1-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var second = await sql.CreateUserAsync("export-reporter2-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Reporter);
        var project = Guid.NewGuid(); var reporterFileIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Private evidence boundary", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, manager.Id, new DateOnly(2000, 1, 1)));
            var owners = new[] { first.Id, second.Id };
            for (var i = 0; i < owners.Length; i++)
            {
                db.Files.Add(StoredFile.Create(reporterFileIds[i], $"private/{reporterFileIds[i]:D}", "private-reporter.jpg", "image/jpeg", 10, new string('b', 64), owners[i], DateTimeOffset.UtcNow, null));
                db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), reporterFileIds[i], owners[i], DateTimeOffset.UtcNow));
            }
            await db.SaveChangesAsync();
        }
        var source = new DenySourceReads(); var storage = new ArtifactFixture();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IHostedService>(); services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(storage);
            services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(source);
            services.Configure<ExportOptions>(o => o.UnicodeFontPath = Environment.GetEnvironmentVariable("ANH02_TEST_FONT_PATH") ?? throw new InvalidOperationException("Set ANH02_TEST_FONT_PATH to an embedding-licensed Unicode TTF font."));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await Login(client, manager.UserName!);
        var path = $"/api/v1/projects/{project}/exports";
        var admitted = await Command(client, path, new { kind = "DOSSIER", format = "ZIP", includeOriginalFiles = true }, Guid.NewGuid().ToString()); Assert.Equal(HttpStatusCode.Accepted, admitted.StatusCode);
        var id = (await Body(admitted)).GetProperty("id").GetGuid(); var manifest = await Body(await client.GetAsync(path + $"/{id}/manifest"));
        Assert.Empty(manifest.GetProperty("files").EnumerateArray());
        Assert.Contains(manifest.GetProperty("sections").EnumerateArray(), s => s.GetProperty("reasonCodes").EnumerateArray().Any(r => r.GetString() == "REPORTER_EVIDENCE_ACCESS_NOT_AVAILABLE"));
        using (var scope = factory.Services.CreateScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        var content = await client.GetAsync(path + $"/{id}/content"); Assert.Equal(HttpStatusCode.OK, content.StatusCode); Assert.Equal(0, source.Reads);
        await Login(client, first.UserName!); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + $"/{id}/content")).StatusCode);
        await Login(client, second.UserName!); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + $"/{id}/content")).StatusCode);
    }
    [Fact]
    public Task Actual_reporting_admission_protected_artifact_replay_expiry_and_revocation() => CheckRecoveryAsync(false);

    [MinioSmokeFact]
    [Trait("Requires", "Minio")]
    public Task Live_MinIO_export_ack_loss_recovery_private_download_replay_expiry_and_revocation() => CheckRecoveryAsync(true);

    private async Task CheckRecoveryAsync(bool live)
    {
        var supervisor = await sql.CreateUserAsync("export-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync("export-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var stranger = await sql.CreateUserAsync("export-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var project = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.Projects.Add(Project.Create(project, project.ToString(), "Actual empty dossier", null, null, null, null, DateTimeOffset.UtcNow));
            db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, manager.Id, new DateOnly(2000, 1, 1))); await db.SaveChangesAsync();
        }
        IAnh02ArtifactStore? liveStore = live ? new MinioAnh02ArtifactStore(Options.Create(new RoadGuardSystem.Repositories.Options.MinioStorageOptions
        {
            Endpoint = Environment.GetEnvironmentVariable("MinioStorage__Endpoint")!,
            BucketName = Environment.GetEnvironmentVariable("MinioStorage__BucketName")!,
            AccessKey = Environment.GetEnvironmentVariable("MinioStorage__AccessKey")!,
            SecretKey = Environment.GetEnvironmentVariable("MinioStorage__SecretKey")!,
            UseSsl = bool.TryParse(Environment.GetEnvironmentVariable("MinioStorage__UseSsl"), out var ssl) && ssl
        })) : null;
        var storage = new ArtifactFixture(liveStore) { LoseFirstWriteAcknowledgement = true };
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IHostedService>(); services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(storage);
            services.Configure<ExportOptions>(o => o.UnicodeFontPath = Environment.GetEnvironmentVariable("ANH02_TEST_FONT_PATH") ?? throw new InvalidOperationException("Set ANH02_TEST_FONT_PATH to an embedding-licensed Unicode TTF font."));
        });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }); await Login(client, manager.UserName!);
        var path = $"/api/v1/projects/{project}/exports"; var key = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.BadRequest, (await Command(client, path, new { kind = "DOSSIER", format = "PDF", unknownFilter = "must-not-be-ignored" }, Guid.NewGuid().ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Command(client, path, new { kind = "DOSSIER", format = "PDF", includeOriginalFiles = true }, Guid.NewGuid().ToString())).StatusCode);
        var unavailable = await Command(client, path, new { kind = "TRAINING", format = "ZIP" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode); Assert.Equal("producer_unavailable", (await Body(unavailable)).GetProperty("code").GetString());
        var admitted = await Command(client, path, new { kind = "DOSSIER", format = "PDF" }, key); Assert.Equal(HttpStatusCode.Accepted, admitted.StatusCode); Assert.NotNull(admitted.Headers.ETag);
        var view = await Body(admitted); var id = view.GetProperty("id").GetGuid(); var hash = view.GetProperty("snapshotHash").GetString();
        var replay = await Command(client, path, new { kind = "DOSSIER", format = "PDF" }, key); Assert.Equal(id, (await Body(replay)).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict, (await Command(client, path, new { kind = "DOSSIER", format = "ZIP" }, key)).StatusCode);
        var manifest = await client.GetAsync(path + $"/{id}/manifest"); Assert.Equal(HttpStatusCode.OK, manifest.StatusCode); Assert.Equal(hash, (await Body(manifest)).GetProperty("snapshotHash").GetString());
        Assert.DoesNotContain("storageUri", await manifest.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        if (live)
        {
            storage.Unavailable = true;
            using (var storageOutage = factory.Services.CreateScope()) Assert.True(await storageOutage.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
            var pending = await Body(await client.GetAsync(path + $"/{id}"));
            Assert.Equal("QUEUED", pending.GetProperty("status").GetString());
            Assert.Equal("export_storage_unavailable", pending.GetProperty("errorCode").GetString());
            Assert.Equal(0, storage.Writes);
            await using var pendingDb = sql.CreateDbContext();
            Assert.Equal(0, await pendingDb.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
            await pendingDb.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02ExportJobs SET NextAttemptAt=NULL WHERE Id={id}");
            storage.Unavailable = false;
        }
        using (var scope = factory.Services.CreateScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        var recoverable = await Body(await client.GetAsync(path + $"/{id}")); Assert.Equal("QUEUED", recoverable.GetProperty("status").GetString());
        await using (var db = sql.CreateDbContext()) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02ExportJobs SET NextAttemptAt=NULL WHERE Id={id}");
        // A new scope models a restarted worker: it must verify/reuse the already durable object.
        using (var restarted = factory.Services.CreateScope()) Assert.True(await restarted.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        var content = await client.GetAsync(path + $"/{id}/content"); Assert.Equal(HttpStatusCode.OK, content.StatusCode); Assert.Equal("application/pdf", content.Content.Headers.ContentType!.MediaType);
        await using (var effects = sql.CreateDbContext())
        {
            Assert.Equal(1, await effects.IdempotencyRecords.CountAsync(r => r.ActorUserId == manager.Id && r.ProjectId == project && r.Operation == "Anh02.Export.Create" && r.IdempotencyKey == key));
            Assert.Equal(1, await effects.Set<ExportSnapshot>().CountAsync(s => s.ProjectId == project));
            Assert.Equal(1, await effects.AuditLogs.CountAsync(a => a.ActorUserId == manager.Id && a.EntityId == id && a.EventType == "Anh02.Export.Admitted"));
            Assert.Equal(1, await effects.AuditLogs.CountAsync(a => a.ActorUserId == manager.Id && a.EntityId == id && a.EventType == "Anh02.Export.Completed"));
            var stored = await (from a in effects.Set<GeneratedArtifact>() join f in effects.Files on a.FileId equals f.Id where a.ExportJobId == id select f).SingleAsync();
            var downloaded = await content.Content.ReadAsByteArrayAsync();
            Assert.Equal(stored.SizeBytes, downloaded.LongLength);
            Assert.Equal(stored.Checksum, Convert.ToHexString(SHA256.HashData(downloaded)).ToLowerInvariant());
        }
        Assert.Equal(1, storage.Writes);
        await Login(client, stranger.UserName!); Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + $"/{id}/content")).StatusCode);
        await Login(client, supervisor.UserName!);
        var hold = await Command(client, "/api/v1/retention/holds", new { scopeType = "PROJECT", scopeId = project, reason = "Preserve export bytes during review" }, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, hold.StatusCode);
        await using (var db = sql.CreateDbContext())
        {
            // Fixture moves both timestamps using one UTC value, preserving exact completedAt +30 days.
            var completed = DateTimeOffset.UtcNow.AddDays(-31); await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02ExportJobs SET CompletedAt={completed}, ExpiresAt={completed.AddDays(30)} WHERE Id={id}");
        }
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path + $"/{id}/content")).StatusCode); Assert.Equal(1, storage.Writes); Assert.Equal(1, storage.Objects);
        await Login(client, manager.UserName!);
        await using (var db = sql.CreateDbContext()) { var membership = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project && x.UserId == manager.Id); membership.Status = ProjectMemberStatus.Ended; await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Forbidden, (await Command(client, path, new { kind = "DOSSIER", format = "PDF" }, key)).StatusCode);
        await using (var db = sql.CreateDbContext()) { Assert.Equal(1, await db.Set<ExportJob>().CountAsync(x => x.ProjectId == project)); Assert.Equal(1, await db.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id)); }
    }
    private static async Task Login(HttpClient client, string user)
    {
        var result = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(user), password = "Current1!" });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(result)).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Command(HttpClient client, string path, object body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) }; request.Headers.Add("Idempotency-Key", key); return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response) => JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    private sealed class ArtifactFixture(IAnh02ArtifactStore? live = null) : IAnh02ArtifactStore
    {
        private readonly Dictionary<string, (byte[] Bytes, string Media)> _objects = new(); public int Writes { get; private set; }
        private readonly HashSet<string> _liveKeys = [];
        public int Objects => live is null ? _objects.Count : _liveKeys.Count;
        public bool LoseFirstWriteAcknowledgement { get; init; }
        public bool Unavailable { get; set; }
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default)
        {
            if (live is not null)
            {
                var actual = await live.WriteAsync(key, content, sizeBytes, mediaType, cancellationToken);
                _liveKeys.Add(key);
                Writes++;
                if (LoseFirstWriteAcknowledgement && Writes == 1) throw new IOException("Injected acknowledgement loss after actual MinIO PUT and durable verification.");
                return actual;
            }
            using var spool = new MemoryStream(); await content.CopyToAsync(spool, cancellationToken); var bytes = spool.ToArray(); _objects[key] = (bytes, mediaType); Writes++;
            if (LoseFirstWriteAcknowledgement && Writes == 1) throw new IOException("Fixture response lost after durable object write.");
            return Metadata(key, bytes, mediaType);
        }
        public Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
        {
            if (Unavailable) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Injected temporary storage unavailability.");
            if (live is not null) return live.OpenReadAsync(key, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested(); if (!_objects.TryGetValue(key, out var value)) throw new FileNotFoundException(); return Task.FromResult(new Anh02ArtifactRead(new MemoryStream(value.Bytes), Metadata(key, value.Bytes, value.Media)));
        }
        private static Anh02ArtifactMetadata Metadata(string key, byte[] bytes, string media) => new(key, bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), media);
    }
    private sealed class DenySourceReads : IUploadObjectStorage
    {
        public int Reads { get; private set; }
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) { Reads++; throw new InvalidOperationException("Private Reporter source bytes must not be read."); }
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
