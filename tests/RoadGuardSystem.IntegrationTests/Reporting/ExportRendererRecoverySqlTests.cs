using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Exports;
using Xunit;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RoadGuardSystem.ExportTestWorker")]

namespace RoadGuardSystem.IntegrationTests.Reporting;

// Real service, renderer, SQL and MinIO: six exception boundaries plus twelve abrupt-exit scenarios (one exits twice).
// Controlled frozen DOSSIER ZIP/PDF and TRAINING ZIP snapshots exercise processing.
// The two format recovery cases do not establish reporting capture/admission or AI authority.
// Reporting capture/admission, internal PDF pagination and every source format remain outside this fixture.
public sealed class ExportRendererRecoverySqlTests : IAsyncLifetime
{
    // Each abrupt-exit case leaves a durable queue item intentionally. Keep that
    // recovery proof in its own database so the next case cannot claim it.
    private readonly IdentitySqlServerFixture sql = new(SqlServerTestFixture.CreateForSandboxSeed());
    public Task InitializeAsync() => sql.InitializeAsync();
    public Task DisposeAsync() => sql.DisposeAsync();
    [Theory]
    [InlineData("before-render")]
    [InlineData("after-render")]
    [InlineData("before-put")]
    [InlineData("after-put")]
    [InlineData("before-complete")]
    [InlineData("after-complete")]
    [InlineData("process-before-render")]
    [InlineData("process-after-render")]
    [InlineData("process-during-spool")]
    [InlineData("process-before-sql-commit")]
    [InlineData("process-after-sql-commit")]
    [InlineData("process-after-put")]
    [InlineData("process-after-complete")]
    [InlineData("process-original-open")]
    [InlineData("process-original-read")]
    [InlineData("process-recovery-original-read")]
    [InlineData("process-pdf-after-put")]
    [InlineData("process-training-after-put")]
    public async Task RealRendererAndServiceRecoverDurableBoundaryWithoutDuplicatePublication(string boundary)
    {
        var image = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE")
            ?? throw new InvalidOperationException("Approved disposable MinIO image required.");
        var owner = Guid.NewGuid().ToString("N"); var access = "sbx" + owner[..12];
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var minio = new ContainerBuilder(image)
            .WithLabel("roadguard.sandbox.owner", owner).WithLabel("roadguard.sandbox.mode", "task18-test")
            .WithEnvironment("MINIO_ROOT_USER", access).WithEnvironment("MINIO_ROOT_PASSWORD", secret)
            .WithCommand("server", "/data").WithPortBinding(9000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                r => r.ForPort(9000).ForPath("/minio/health/ready"), s => s.WithTimeout(TimeSpan.FromSeconds(30)))).Build();
        await minio.StartAsync();
        var endpoint = $"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}";
        var bucket = "sbx-" + owner;
        using var s3 = new AmazonS3Client(access, secret, new AmazonS3Config
        { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        var store = new MinioAnh02ArtifactStore(Options.Create(new MinioStorageOptions
        { Endpoint = endpoint, BucketName = bucket, AccessKey = access, SecretKey = secret }));
        var pdfOnly = boundary.StartsWith("process-pdf-", StringComparison.Ordinal);
        var training = boundary.StartsWith("process-training-", StringComparison.Ordinal);
        var kind = training ? "TRAINING" : "DOSSIER";
        var format = pdfOnly ? "PDF" : "ZIP";
        var mimeType = pdfOnly ? "application/pdf" : "application/zip";
        // Normalize only harness scenario names; no public worker or API fields change.
        if (pdfOnly) boundary = "process-" + boundary[12..];
        if (training) boundary = "process-" + boundary[17..];
        var withOriginal = boundary.Contains("original", StringComparison.Ordinal);
        var originalBytes = Enumerable.Range(0, 131072).Select(i => (byte)(i % 251)).ToArray();
        var originalId = Guid.NewGuid(); var originalKey = "private/" + originalId.ToString("N");
        var originalHash = Convert.ToHexString(SHA256.HashData(originalBytes)).ToLowerInvariant();
        var storageOptions = Options.Create(new MinioStorageOptions
        { Endpoint = endpoint, BucketName = bucket, AccessKey = access, SecretKey = secret });
        var sourceStorage = new MinioUploadObjectStorage(storageOptions);
        if (withOriginal)
            using (var input = new MemoryStream(originalBytes))
                await s3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = originalKey,
                    InputStream = input,
                    ContentType = "video/mp4"
                });
        var clock = new Clock();
        Guid id, actor, project; ExportSnapshot frozen;
        await using (var db = sql.CreateDbContext())
        {
            var source = await H4GenuineRepairSource.Seed(db, sql); actor = source.Pm; project = source.Project;
            if (withOriginal)
            {
                var file = StoredFile.Create(originalId, originalKey, "synthetic.mp4", "video/mp4",
                    originalBytes.Length, originalHash, actor, clock.GetUtcNow(), null);
                db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project, null, actor,
                    "SURVEY_VIDEO", clock.GetUtcNow()));
                await db.SaveChangesAsync();
                var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor, file.StorageUri,
                    "SURVEY_VIDEO", file.MimeType, file.SizeBytes, file.Checksum, 8388608, clock.GetUtcNow().AddHours(1));
                upload.StartUploading("owned-fixture", clock.GetUtcNow()); db.Add(upload); await db.SaveChangesAsync();
                upload.StartVerification(Convert.ToBase64String(upload.RowVersion), clock.GetUtcNow());
                await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
            }
            var snapshotId = Guid.NewGuid();
            var payload = new ExportSnapshotPayloadDto(new("anh02.export.v1", snapshotId, project, kind, format,
                actor, clock.GetUtcNow(), clock.GetUtcNow(), [], new(kind, format), [],
                withOriginal ? [new ExportFileDto(originalId, "controlled-upload", originalHash, originalBytes.Length,
                    "video/mp4", ExportArchive.SurveyFilePath(originalId, "video/mp4"), true, null)] : [], [], null, ""), null);
            var json = JsonSerializer.Serialize(payload, ExportSerialization.Options);
            frozen = ExportSnapshot.Create(snapshotId, project, json, ExportSerialization.Hash(json), clock.GetUtcNow());
            var job = ExportJob.Create(Guid.NewGuid(), project, actor, snapshotId, kind, format, clock.GetUtcNow());
            id = job.Id; db.AddRange(frozen, job); await db.SaveChangesAsync();
        }
        var key = $"anh02/exports/{id:D}/{frozen.Hash}.{format.ToLowerInvariant()}";
        var fired = false; var renders = 0; var puts = 0;
        void Fault(string point) { if (!fired && boundary == point) { fired = true; throw new BoundaryInterruption(); } }
        var realRenderer = new ExportRenderer(Options.Create(new ExportOptions
        { UnicodeFontPath = TestFontPath }));
        var renderer = Proxy<IExportRenderer>((_, args) => Render(args!));
        async Task<Stream> Render(object?[] args)
        {
            Fault("before-render"); renders++;
            var stream = await realRenderer.RenderAsync((ExportSnapshotPayloadDto)args[0]!,
                (Func<ExportFileDto, CancellationToken, Task<Stream>>)args[1]!,
                (Func<CancellationToken, Task>)args[2]!, (CancellationToken)args[3]!);
            if (!fired && boundary == "after-render") { await stream.DisposeAsync(); Fault("after-render"); }
            return stream;
        }
        var artifacts = Proxy<IAnh02ArtifactStore>((method, args) => method.Name == nameof(IAnh02ArtifactStore.WriteAsync)
            ? Put(args!) : store.OpenReadAsync((string)args![0]!, (CancellationToken)args[1]!));
        async Task<Anh02ArtifactMetadata> Put(object?[] args)
        {
            Fault("before-put"); puts++;
            var metadata = await store.WriteAsync((string)args[0]!, (Stream)args[1]!, (long?)args[2],
                (string)args[3]!, (CancellationToken)args[4]!);
            Fault("after-put"); return metadata;
        }
        async Task Run()
        {
            await using var db = sql.CreateDbContext();
            var real = new ExportRepository(db, new IdempotencyOperationService(db), clock,
                Options.Create(new ExportRepositoryOptions { LeaseDurationSeconds = 60, RetryBackoffSeconds = 10 }));
            var repository = Proxy<IExportRepository>((method, args) => method.Name == nameof(IExportRepository.CompleteAsync)
                ? Complete(args!) : Invoke(real, method, args));
            async Task<bool> Complete(object?[] args)
            {
                Fault("before-complete");
                var result = await real.CompleteAsync((ExportClaim)args[0]!, (Anh02ArtifactMetadata)args[1]!,
                    (Func<CancellationToken, Task<bool>>)args[2]!, (CancellationToken)args[3]!);
                Fault("after-complete"); return result;
            }
            using var services = new ServiceCollection().AddScoped<IExportRepository>(_ => repository).BuildServiceProvider();
            var service = new ExportService(repository, new IdentityRepository(db, clock),
                new ProjectScopeGuard(new ProjectMembershipReadModel(db), clock), null!, [], [], artifacts, sourceStorage, renderer,
                clock, services.GetRequiredService<IServiceScopeFactory>());
            await service.ProcessNextAsync(default);
        }
        if (boundary.StartsWith("process-", StringComparison.Ordinal))
        {
            var repeats = boundary == "process-recovery-original-read" ? 2 : 1;
            var workerBoundary = boundary == "process-recovery-original-read" ? "original-read" : boundary[8..];
            for (var attempt = 0; attempt < repeats; attempt++)
            {
                if (attempt > 0) clock.Advance(TimeSpan.FromSeconds(61));
                var executable = Environment.GetEnvironmentVariable("RG_EXPORT_TEST_WORKER")
                    ?? throw new InvalidOperationException("Build the dedicated test worker and provide its assembly path.");
                var start = new ProcessStartInfo("dotnet")
                { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(Path.GetFullPath(executable));
                start.Environment.Remove("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING");
                using var process = Process.Start(start)!;
                // Only this already ownership-verified fixture connection is delegated.
                await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new WorkerInput(sql.ConnectionString,
                    endpoint, bucket, access, secret, workerBoundary, clock.GetUtcNow())));
                process.StandardInput.Close();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
                Assert.True(process.ExitCode == 73,
                    $"Expected crash boundary exit 73; actual {process.ExitCode}. {await process.StandardError.ReadToEndAsync()}");
            }
            boundary = boundary[8..]; fired = true;
        }
        else await Assert.ThrowsAsync<BoundaryInterruption>(Run);
        Assert.True(fired);
        var published = boundary is "after-complete" or "after-sql-commit";
        var durableObject = boundary is "after-put" or "before-complete" or "after-complete" or "before-sql-commit" or "after-sql-commit";
        await using (var post = sql.CreateDbContext())
        {
            var job = await post.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal(published ? "SUCCEEDED" : "RUNNING", job.Status);
            Assert.Equal(published ? 1 : 0, await post.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
        }
        Assert.Equal((durableObject ? 1 : 0) + (withOriginal ? 1 : 0), ((await s3.ListObjectsV2Async(new() { BucketName = bucket })).S3Objects?.Count ?? 0));
        var rendersBefore = renders; var putsBefore = puts;
        clock.Advance(TimeSpan.FromSeconds(61));
        if (withOriginal) await Task.WhenAll(Run(), Run()); else await Run();
        await Run();
        await using (var post = sql.CreateDbContext())
        {
            Assert.Equal("SUCCEEDED", (await post.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).Status);
            Assert.Equal(1, await post.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
            var snapshot = await post.Set<ExportSnapshot>().AsNoTracking().SingleAsync(x => x.Id == frozen.Id);
            Assert.Equal(frozen.PayloadJson, snapshot.PayloadJson); Assert.Equal(frozen.Hash, snapshot.Hash);
        }
        if (durableObject) { Assert.Equal(rendersBefore, renders); Assert.Equal(putsBefore, puts); }
        await using var read = await store.OpenReadAsync(key);
        using var bytes = new MemoryStream(); await read.Content.CopyToAsync(bytes);
        Assert.Equal(read.Metadata.SizeBytes, bytes.Length);
        Assert.Equal(read.Metadata.Sha256, Convert.ToHexString(SHA256.HashData(bytes.ToArray())).ToLowerInvariant());
        if (pdfOnly)
        {
            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes.ToArray(), 0, 5));
            Assert.Equal("application/pdf", read.Metadata.MediaType);
        }
        else
        {
            bytes.Position = 0; using var zip = new ZipArchive(bytes, ZipArchiveMode.Read);
            var expectedEntries = new List<string> { "evidence-index.json", "manifest.json" };
            if (training) { expectedEntries.Add("labels.jsonl"); expectedEntries.Add("README.txt"); }
            else expectedEntries.Add("dossier.pdf");
            if (withOriginal)
            {
                var entryPath = ExportArchive.SurveyFilePath(originalId, "video/mp4"); expectedEntries.Add(entryPath);
                using var originalContent = zip.GetEntry(entryPath)!.Open(); using var extracted = new MemoryStream();
                await originalContent.CopyToAsync(extracted); Assert.Equal(originalBytes, extracted.ToArray());
                Assert.DoesNotContain(zip.Entries, e => e.FullName.Contains(originalKey, StringComparison.Ordinal));
            }
            Assert.Equal(expectedEntries.Order().ToArray(), zip.Entries.Select(x => x.FullName).Order().ToArray());
            using var manifest = JsonDocument.Parse(zip.GetEntry("manifest.json")!.Open());
            Assert.Equal(frozen.Hash, manifest.RootElement.GetProperty("snapshotHash").GetString());
            Assert.Equal(project, manifest.RootElement.GetProperty("projectId").GetGuid());
            if (training)
            {
                using var labels = new StreamReader(zip.GetEntry("labels.jsonl")!.Open());
                Assert.Equal("\n", await labels.ReadToEndAsync()); // Controlled empty capture, not an admitted dataset.
                Assert.True(zip.GetEntry("README.txt")!.Length > 0);
            }
            else
            {
                using var pdf = zip.GetEntry("dossier.pdf")!.Open(); var header = new byte[5];
                Assert.Equal(5, pdf.Read(header)); Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(header));
            }
        }
        Assert.Equal(withOriginal ? 2 : 1, (await s3.ListObjectsV2Async(new() { BucketName = bucket })).S3Objects.Count);
        // The published artifact is an indirect private read. Exercise the actual current
        // SQL membership guard and MinIO read, rather than trusting a captured export grant.
        await using var accessDb = sql.CreateDbContext();
        var accessRepo = new ExportRepository(accessDb, new IdempotencyOperationService(accessDb), clock,
            Options.Create(new ExportRepositoryOptions { LeaseDurationSeconds = 60 }));
        var objectReads = 0;
        var accessStore = Proxy<IAnh02ArtifactStore>((method, args) => method.Name == nameof(IAnh02ArtifactStore.OpenReadAsync)
            ? ReadArtifact((string)args![0]!, (CancellationToken)args[1]!) : throw new InvalidOperationException());
        async Task<Anh02ArtifactRead> ReadArtifact(string objectKey, CancellationToken ct)
        { objectReads++; return await store.OpenReadAsync(objectKey, ct); }
        using var accessServices = new ServiceCollection().BuildServiceProvider();
        var accessService = new ExportService(accessRepo, new IdentityRepository(accessDb, clock),
            new ProjectScopeGuard(new ProjectMembershipReadModel(accessDb), clock), null!, [], [], accessStore,
            null!, realRenderer, clock, accessServices.GetRequiredService<IServiceScopeFactory>());
        Assert.Equal("success", (await accessService.GetAsync(actor, project, id, default)).Code);
        Assert.Equal(frozen.Hash, (await accessService.ManifestAsync(actor, project, id, default)).Value!.SnapshotHash);
        var allowed = await accessService.ContentAsync(actor, project, id, default);
        Assert.Equal("success", allowed.Code);
        using (var returned = new MemoryStream())
        {
            await using var content = allowed.Value!.Content;
            await content.CopyToAsync(returned);
            Assert.Equal(bytes.ToArray(), returned.ToArray());
        }
        Assert.Equal("forbidden", (await accessService.ContentAsync(Guid.NewGuid(), project, id, default)).Code);
        Assert.Equal("forbidden", (await accessService.ContentAsync(actor, Guid.NewGuid(), id, default)).Code);
        Assert.Equal(1, objectReads);
        // Fault only the uniquely owned object's bytes. Ready SQL metadata must never
        // make a corrupt/missing object readable; restore it before revocation checks.
        // Bypass the immutable adapter only for fault injection into this owned object.
        var corruptBytes = "synthetic corruption"u8.ToArray();
        using (var corrupt = new MemoryStream(corruptBytes))
        {
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = corrupt,
                ContentType = mimeType
            };
            request.Metadata["anh02-sha256"] = Convert.ToHexString(SHA256.HashData(corruptBytes)).ToLowerInvariant();
            request.Metadata["anh02-size"] = corruptBytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await s3.PutObjectAsync(request);
        }
        Assert.Equal("export_storage_unavailable", (await accessService.ContentAsync(actor, project, id, default)).Code);
        await s3.DeleteObjectAsync(bucket, key);
        Assert.Equal("export_storage_unavailable", (await accessService.ContentAsync(actor, project, id, default)).Code);
        await using (var restore = new MemoryStream(bytes.ToArray()))
            await store.WriteAsync(key, restore, restore.Length, mimeType, default);
        var restored = await accessService.ContentAsync(actor, project, id, default);
        Assert.Equal("success", restored.Code);
        await restored.Value!.Content.DisposeAsync();
        Assert.Equal(4, objectReads);
        var membership = await accessDb.ProjectMembers.SingleAsync(x => x.ProjectId == project && x.UserId == actor);
        membership.EndPrimaryAssignment(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
        await accessDb.SaveChangesAsync();
        var version = (await accessDb.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).RowVersion;
        Assert.Equal("forbidden", (await accessService.GetAsync(actor, project, id, default)).Code);
        Assert.Equal("forbidden", (await accessService.ManifestAsync(actor, project, id, default)).Code);
        Assert.Equal("forbidden", (await accessService.ContentAsync(actor, project, id, default)).Code);
        Assert.Equal(4, objectReads);
        Assert.Equal(version, (await accessDb.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).RowVersion);
        Assert.Equal(1, await accessDb.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
        // Revocation denies a new Backend read; it does not delete the private object.
        await using var retained = await store.OpenReadAsync(key);
        Assert.Equal(read.Metadata.Sha256, retained.Metadata.Sha256);
    }
    private sealed class BoundaryInterruption : Exception;
    public sealed record WorkerInput(string Connection, string Endpoint, string Bucket, string Access,
        string Secret, string Boundary, DateTimeOffset Now);

    // This method never creates/migrates/drops a database. The parent exclusively
    // delegates its owned test fixture. Defensive endpoint/name checks reject shared
    // SQL, including accidental inheritance of a developer's configured connection.
    internal static async Task RunOwnedWorker(WorkerInput input)
    {
        var connection = new SqlConnectionStringBuilder(input.Connection);
        if (!connection.InitialCatalog.StartsWith("RoadGuard_Test_", StringComparison.Ordinal)
            || !System.Text.RegularExpressions.Regex.IsMatch(connection.InitialCatalog, "^RoadGuard_Test_[a-f0-9]{32}$")
            || !(connection.DataSource.StartsWith("127.0.0.1,", StringComparison.Ordinal)
                || connection.DataSource.StartsWith("localhost,", StringComparison.Ordinal))
            || !Uri.TryCreate(input.Endpoint, UriKind.Absolute, out var endpoint) || !endpoint.IsLoopback
            || !System.Text.RegularExpressions.Regex.IsMatch(input.Bucket, "^sbx-[a-f0-9]{32}$")
            || input.Boundary is not ("before-render" or "after-render" or "during-spool" or "before-sql-commit" or "after-sql-commit" or "after-put" or "after-complete" or "original-open" or "original-read"))
            throw new InvalidOperationException($"Not an owned loopback test worker request (database={System.Text.RegularExpressions.Regex.IsMatch(connection.InitialCatalog, "^RoadGuard_Test_[a-f0-9]{32}$")}, sqlLoopback={connection.DataSource.StartsWith("127.0.0.1,", StringComparison.Ordinal) || connection.DataSource.StartsWith("localhost,", StringComparison.Ordinal)}, objectLoopback={Uri.TryCreate(input.Endpoint, UriKind.Absolute, out var checkedEndpoint) && checkedEndpoint.IsLoopback}, bucket={System.Text.RegularExpressions.Regex.IsMatch(input.Bucket, "^sbx-[a-f0-9]{32}$")}).");
        var commitCrash = new ExitAtCompletionCommit(input.Boundary);
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(input.Connection,
            provider => provider.UseNetTopologySuite()).AddInterceptors(commitCrash).Options;
        await using var db = new RoadGuardDbContext(options);
        var clock = new Clock(input.Now);
        var real = new ExportRepository(db, new IdempotencyOperationService(db), clock,
            Options.Create(new ExportRepositoryOptions { LeaseDurationSeconds = 60 }));
        var repository = Proxy<IExportRepository>((method, args) => method.Name == nameof(IExportRepository.CompleteAsync)
            ? Complete(args!) : Invoke(real, method, args));
        async Task<bool> Complete(object?[] args)
        {
            // Arm only after renderer/object work, never the earlier claim/renew commits.
            commitCrash.Armed = true;
            var result = await real.CompleteAsync((ExportClaim)args[0]!, (Anh02ArtifactMetadata)args[1]!,
                (Func<CancellationToken, Task<bool>>)args[2]!, (CancellationToken)args[3]!);
            if (input.Boundary == "after-complete") Environment.Exit(73);
            return result;
        }
        var store = new MinioAnh02ArtifactStore(Options.Create(new MinioStorageOptions
        { Endpoint = input.Endpoint, BucketName = input.Bucket, AccessKey = input.Access, SecretKey = input.Secret }));
        var artifacts = Proxy<IAnh02ArtifactStore>((method, args) => method.Name == nameof(IAnh02ArtifactStore.WriteAsync)
            ? Put(args!) : store.OpenReadAsync((string)args![0]!, (CancellationToken)args[1]!));
        async Task<Anh02ArtifactMetadata> Put(object?[] args)
        {
            var content = (Stream)args[1]!;
            if (input.Boundary == "during-spool") content = new ExitDuringSpool(content);
            var metadata = await store.WriteAsync((string)args[0]!, content, (long?)args[2],
                (string)args[3]!, (CancellationToken)args[4]!);
            if (input.Boundary == "after-put") Environment.Exit(73);
            return metadata;
        }
        using var services = new ServiceCollection().AddScoped<IExportRepository>(_ => repository).BuildServiceProvider();
        var realRenderer = new ExportRenderer(Options.Create(new ExportOptions
        { UnicodeFontPath = TestFontPath }));
        var renderer = Proxy<IExportRenderer>((_, args) => Render(args!));
        async Task<Stream> Render(object?[] args)
        {
            if (input.Boundary == "before-render") Environment.Exit(73);
            var output = await realRenderer.RenderAsync((ExportSnapshotPayloadDto)args[0]!,
                async (file, token) =>
                {
                    // This callback runs inside the real ZIP renderer, after manifest/PDF entries.
                    if (input.Boundary == "original-open") Environment.Exit(73);
                    var opened = await ((Func<ExportFileDto, CancellationToken, Task<Stream>>)args[1]!)(file, token);
                    return input.Boundary == "original-read" ? new ExitDuringSpool(opened) : opened;
                },
                (Func<CancellationToken, Task>)args[2]!, (CancellationToken)args[3]!);
            if (input.Boundary == "after-render") Environment.Exit(73);
            return output;
        }
        var service = new ExportService(repository, new IdentityRepository(db, clock),
            new ProjectScopeGuard(new ProjectMembershipReadModel(db), clock), null!, [], [], artifacts,
            new MinioUploadObjectStorage(Options.Create(new MinioStorageOptions
            { Endpoint = input.Endpoint, BucketName = input.Bucket, AccessKey = input.Access, SecretKey = input.Secret })),
            renderer, clock, services.GetRequiredService<IServiceScopeFactory>());
        await service.ProcessNextAsync(default);
    }
    private sealed class ExitAtCompletionCommit(string boundary) : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Armed && boundary == "before-sql-commit") Environment.Exit(73);
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction,
            TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Armed && boundary == "after-sql-commit") Environment.Exit(73);
            return Task.CompletedTask;
        }
    }
    // The first chunk reaches the real adapter's temporary spool. The next read
    // exits the owned child, before any PUT, rather than throwing in the parent.
    private sealed class ExitDuringSpool(Stream input) : Stream
    {
        private bool copied;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (copied) Environment.Exit(73);
            var count = await input.ReadAsync(buffer[..Math.Min(buffer.Length, 32)], ct);
            if (count == 0) throw new InvalidOperationException("Nonempty rendered bytes required.");
            copied = true; return count;
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => input.Length;
        public override long Position { get => input.Position; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now;
        public Clock(DateTimeOffset? initial = null) => now = initial ?? DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan delta) => now += delta;
    }
    private static string TestFontPath => Environment.GetEnvironmentVariable("RG_TEST_UNICODE_FONT")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
    private static object? Invoke(object target, MethodInfo method, object?[]? args)
    {
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }
    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    { var proxy = DispatchProxy.Create<T, Forwarder>(); ((Forwarder)(object)proxy).Call = invoke; return proxy; }
    public class Forwarder : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!, args);
    }
}
