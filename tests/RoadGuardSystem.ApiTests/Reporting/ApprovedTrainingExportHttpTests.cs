using System.IO.Compression;
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
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Labels;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.Services.Cases;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Labels;
using Xunit;

namespace RoadGuardSystem.ApiTests.Reporting;

// Decision 34A: actual PM label approval and immutable training metadata. No Reporter original is exported.
// The injected lost acknowledgement is an exception boundary, not a process-crash claim.
public sealed class ApprovedTrainingExportHttpTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonemptyApprovedTrainingSnapshotSurvivesRevisionAndLostPutAcknowledgement(bool loseAcknowledgement)
    {
        sql.GetOwnedSeedDatabaseIdentity();
        var image = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE") ?? throw new InvalidOperationException("Approved disposable MinIO image required.");
        var owner = Guid.NewGuid().ToString("N"); var access = "sbx" + owner[..12]; var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var minio = new ContainerBuilder(image).WithLabel("roadguard.sandbox.owner", owner)
            .WithLabel("roadguard.sandbox.mode", "task24-test").WithEnvironment("MINIO_ROOT_USER", access)
            .WithEnvironment("MINIO_ROOT_PASSWORD", secret).WithCommand("server", "/data").WithPortBinding(9000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/minio/health/ready"),
                m => m.WithTimeout(TimeSpan.FromSeconds(30)))).Build();
        await minio.StartAsync();
        var endpoint = $"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}"; var bucket = "sbx-" + owner;
        using var s3 = new AmazonS3Client(access, secret, new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        var options = Options.Create(new MinioStorageOptions { Endpoint = endpoint, BucketName = bucket, AccessKey = access, SecretKey = secret });
        var store = new ObservedStore(new MinioAnh02ArtifactStore(options)); var clock = new Clock();
        GenuineProtectedSource.Source source; Guid reportId; string type;
        await using (var db = sql.CreateDbContext())
        {
            source = await GenuineProtectedSource.Seed(db, sql);
            (await db.Projects.SingleAsync(p => p.Id == source.Project)).EngineeringUtmSrid = 32648;
            await db.SaveChangesAsync();
            reportId = await db.Set<HuyCaseReportLink>().Where(l => db.IncidentCases.Any(c => c.Id == l.CaseId && c.ProjectId == source.Project)).Select(l => l.ReportId).SingleAsync();
            type = await db.Defects.Where(d => d.Id == source.Defect).Select(d => d.DefectTypeCode).SingleAsync();
        }
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        {
            services.AddSingleton<IAnh02ArtifactStore>(store); services.AddSingleton<TimeProvider>(clock);
            services.Configure<ExportOptions>(o => o.UnicodeFontPath = Environment.GetEnvironmentVariable("ANH02_TEST_FONT_PATH") ?? Environment.GetEnvironmentVariable("RG_TEST_UNICODE_FONT") ?? throw new InvalidOperationException("Set RG_TEST_UNICODE_FONT to an embedding-licensed Unicode TTF font."));
        });
        using var client = factory.CreateClient();
        string email; await using (var db = sql.CreateDbContext()) email = (await db.Users.SingleAsync(u => u.Id == source.Pm)).Email!;
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        await BindReportGeometry(factory, client, source, reportId);
        Guid jobId; TrainingLabelViewDto label; Guid fileId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var producer = scope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>();
            var facts = await producer.ResolveCandidateSourceAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, CandidateSourceKind.Report, reportId);
            Assert.Equal(AnhHuyProducerStatus.Ready, facts.Status); fileId = Assert.Single(facts.Facts!.Evidence).Reference.FileId;
            var labels = scope.ServiceProvider.GetRequiredService<ITrainingLabelService>();
            var created = await labels.CreateAsync(source.Pm, UserRoleCode.ProjectManager, source.Project,
                new("REPORT", reportId, facts.Facts.DomainFacts.Source.SourceVersion, fileId, new("BBOX", "NORMALIZED", .1m, .2m, .3m, .4m), type, "synthetic source annotation"), owner + "-label", null, default);
            Assert.Equal(201, created.Status); Assert.NotNull(created.Label);
            var exports = scope.ServiceProvider.GetRequiredService<IExportService>();
            Assert.Equal("no_eligible_labels", (await exports.CreateAsync(source.Pm, source.Project, new("TRAINING", "ZIP"), owner + "-unapproved", null, default)).Code);
            var reviewed = await labels.ReviewAsync(source.Pm, UserRoleCode.ProjectManager, created.Label.Id,
                new("APPROVE", "PM approval under decision 34A"), owner + "-review", "\"" + created.Label.Version + "\"", null, default);
            Assert.Equal(200, reviewed.Status); label = reviewed.Label!; Assert.Equal(source.Pm, label.ReviewedBy);
            var admitted = await exports.CreateAsync(source.Pm, source.Project, new("TRAINING", "ZIP", IncludeOriginalFiles: false), owner + "-export", null, default);
            Assert.Equal("success", admitted.Code); jobId = admitted.Value!.Id;
        }
        async Task State(string expected)
        {
            await using var db = sql.CreateDbContext(); var job = await db.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == jobId);
            var version = job.RowVersion.ToArray(); var url = $"/api/v1/projects/{source.Project}/exports/{jobId}";
            var response = await client.GetAsync(url); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var value = (await response.Content.ReadFromJsonAsync<ExportJobViewDto>())!; Assert.Equal(expected, value.Status);
            Assert.Equal("\"" + value.Version + "\"", response.Headers.ETag!.ToString()); Assert.Equal("TRAINING", value.Kind);
            var manifestResponse = await client.GetAsync(url + "/manifest"); Assert.Equal(HttpStatusCode.OK, manifestResponse.StatusCode);
            Assert.Equal(label.Id, Assert.Single((await manifestResponse.Content.ReadFromJsonAsync<ExportManifestV1Dto>())!.Labels!).LabelId);
            Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(url + "/content")).StatusCode);
            await db.Entry(job).ReloadAsync(); Assert.Equal(version, job.RowVersion);
        }
        await State("QUEUED"); var lease = Guid.NewGuid();
        await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId); Assert.True(job.TryClaim(lease, clock.Now, TimeSpan.FromMinutes(1))); await db.SaveChangesAsync(); }
        await State("RUNNING");
        await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId); job.Fail(lease, clock.Now, "SYNTHETIC_STATE", true); await db.SaveChangesAsync(); }
        await State("FAILED");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var admitted = await scope.ServiceProvider.GetRequiredService<IExportService>().CreateAsync(source.Pm, source.Project, new("TRAINING", "ZIP", IncludeOriginalFiles: false), owner + "-ready", null, default);
            Assert.Equal("success", admitted.Code); jobId = admitted.Value!.Id;
        }
        string frozen; string hash;
        await using (var db = sql.CreateDbContext())
        {
            var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == jobId);
            var snap = await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId); frozen = snap.PayloadJson; hash = snap.Hash;
        }
        var snapshot = JsonSerializer.Deserialize<ExportSnapshotPayloadDto>(frozen, ExportSerialization.Options)!;
        var captured = Assert.Single(snapshot.Manifest.Labels!); Assert.Equal(label.Id, captured.LabelId);
        Assert.Equal(label.Revision, captured.Revision); Assert.Equal(source.Pm, captured.ApprovedBy); Assert.NotEqual(Guid.Empty, captured.ApprovalId);
        Assert.Equal(label.SourceVersion, captured.SourceVersion); Assert.Equal(label.FileVersion, captured.FileVersion); Assert.Equal(type, captured.TypeCode);
        Assert.Equal(.3m, captured.Width); Assert.Equal(.4m, captured.Height); Assert.Equal("REPORT", captured.SourceKind);
        var file = Assert.Single(snapshot.Manifest.Files); Assert.Equal(fileId, file.FileId); Assert.False(file.Included); Assert.Null(file.ArchivePath);
        Assert.Equal("ORIGINALS_NOT_REQUESTED", file.ReasonCode);
        var path = $"/api/v1/projects/{source.Project}/exports/{jobId}";
        using (var anonymous = factory.CreateClient()) foreach (var suffix in new[] { "", "/manifest", "/content" }) Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path + suffix)).StatusCode);
        foreach (var actor in new[] { source.Crew, source.Supervisor })
        {
            using var other = factory.CreateClient(); string actorEmail; await using (var db = sql.CreateDbContext()) actorEmail = (await db.Users.SingleAsync(u => u.Id == actor)).Email!;
            var signed = await other.PostAsJsonAsync("/api/v1/auth/login", new { email = actorEmail, password = "Current1!" }); signed.EnsureSuccessStatusCode();
            other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await signed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
            // Supervisor also needs current membership, which the genuine fixture supplies; Crew has no export role.
            foreach (var suffix in new[] { "", "/manifest", "/content" }) Assert.Equal(actor == source.Crew ? HttpStatusCode.Forbidden : suffix == "/content" ? HttpStatusCode.Conflict : HttpStatusCode.OK, (await other.GetAsync(path + suffix)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(path + "/content")).StatusCode);
        store.LoseAcknowledgement = loseAcknowledgement;
        await using (var scope = factory.Services.CreateAsyncScope()) Assert.True(await scope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        if (loseAcknowledgement)
        {
            await using (var db = sql.CreateDbContext())
            {
                var job = await db.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == jobId);
                Assert.Equal("QUEUED", job.Status); Assert.Null(job.ArtifactId); Assert.Equal("export_storage_unavailable", job.ErrorCode);
                Assert.Empty(await db.Set<GeneratedArtifact>().Where(a => a.ExportJobId == jobId).ToListAsync());
            }
            Assert.Equal(1, store.Writes); Assert.NotNull(store.LastKey);
            Assert.True((await s3.GetObjectMetadataAsync(bucket, store.LastKey)).ContentLength > 0);
            clock.Now = clock.Now.AddMinutes(2);
            await using var restartedScope = factory.Services.CreateAsyncScope();
            Assert.True(await restartedScope.ServiceProvider.GetRequiredService<IExportService>().ProcessNextAsync(default));
        }
        Assert.Equal(1, store.Writes); // Existing matching object is reconciled; no duplicate PUT.
        var get = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var view = (await get.Content.ReadFromJsonAsync<ExportJobViewDto>())!; Assert.Equal("SUCCEEDED", view.Status); Assert.Equal("TRAINING", view.Kind);
        Assert.Equal("\"" + view.Version + "\"", get.Headers.ETag!.ToString());
        var manifestResponse = await client.GetAsync(path + "/manifest"); Assert.Equal(HttpStatusCode.OK, manifestResponse.StatusCode);
        Assert.Equal("\"" + hash + "\"", manifestResponse.Headers.ETag!.ToString());
        var manifest = (await manifestResponse.Content.ReadFromJsonAsync<ExportManifestV1Dto>())!; Assert.Equal(captured, Assert.Single(manifest.Labels!));
        Assert.DoesNotContain("private/h4-reporter-", await manifestResponse.Content.ReadAsStringAsync());
        var content = await client.GetAsync(path + "/content"); Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        var bytes = await content.Content.ReadAsByteArrayAsync(); Assert.Equal("application/zip", content.Content.Headers.ContentType!.MediaType);
        Assert.True(content.Headers.CacheControl!.Private); Assert.True(content.Headers.CacheControl.NoStore); Assert.NotNull(content.Content.Headers.ContentDisposition!.FileNameStar);
        Assert.Equal("\"" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() + "\"", content.Headers.ETag!.ToString());
        using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
        {
            Assert.Equal(new[] { "README.txt", "evidence-index.json", "labels.jsonl", "manifest.json" }, archive.Entries.Select(e => e.FullName).OrderBy(n => n, StringComparer.Ordinal).ToArray());
            using var reader = new StreamReader(archive.GetEntry("labels.jsonl")!.Open());
            var lines = (await reader.ReadToEndAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries); Assert.Single(lines);
            var exported = JsonSerializer.Deserialize<ExportLabelDto>(lines[0], ExportSerialization.Options)!; Assert.Equal(captured, exported);
        }
        // A new unapproved revision cannot rewrite the captured approved revision or archive.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var revised = await scope.ServiceProvider.GetRequiredService<ITrainingLabelService>().ReviseAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, label.Id,
                new(fileId, new("BBOX", "NORMALIZED", .2m, .2m, .3m, .3m), type, "later annotation revision"), owner + "-revise", "\"" + label.Version + "\"", null, default);
            Assert.Equal(201, revised.Status); Assert.Equal(label.Revision + 1, revised.Label!.Revision);
        }
        Assert.Equal(bytes, await client.GetByteArrayAsync(path + "/content"));
        // Owned object only: missing and corrupt read paths cannot return bytes or rewrite SQL authority.
        var objectKey = store.LastKey!;
        await s3.DeleteObjectAsync(bucket, objectKey); Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path + "/content")).StatusCode);
        await s3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = objectKey, InputStream = new MemoryStream(new byte[] { 1, 2, 3 }), ContentType = "application/zip" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path + "/content")).StatusCode);
        await s3.DeleteObjectAsync(bucket, objectKey); // Only the corrupt object created by this test; preserve adapter metadata on restoration.
        await new MinioAnh02ArtifactStore(options).WriteAsync(objectKey, new MemoryStream(bytes), bytes.Length, "application/zip");
        Assert.Equal(bytes, await client.GetByteArrayAsync(path + "/content"));
        await using (var db = sql.CreateDbContext())
        {
            var job = await db.Set<ExportJob>().AsNoTracking().SingleAsync(j => j.Id == jobId); var snap = await db.Set<ExportSnapshot>().SingleAsync(s => s.Id == job.SnapshotId);
            Assert.Equal(frozen, snap.PayloadJson); Assert.Equal(hash, snap.Hash); Assert.Single(await db.Set<GeneratedArtifact>().Where(a => a.ExportJobId == jobId).ToListAsync());
        }
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        var reads = store.Reads; foreach (var suffix in new[] { "", "/manifest", "/content" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path + suffix)).StatusCode);
        Assert.Equal(reads, store.Reads);
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Active));
        store.FailRead = true; Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(path + "/content")).StatusCode); store.FailRead = false;
        Assert.Equal(bytes, await client.GetByteArrayAsync(path + "/content"));
        // The independently callable DOSSIER/PDF configuration must use its own captured source and artifact.
        Guid pdfId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var exports = scope.ServiceProvider.GetRequiredService<IExportService>();
            var admitted = await exports.CreateAsync(source.Pm, source.Project, new("DOSSIER", "PDF", IncludeOriginalFiles: false), owner + "-pdf", null, default);
            Assert.Equal("success", admitted.Code); pdfId = admitted.Value!.Id;
            async Task PdfState(string state)
            {
                var statePath = $"/api/v1/projects/{source.Project}/exports/{pdfId}";
                var metadata = (await client.GetFromJsonAsync<ExportJobViewDto>(statePath))!; Assert.Equal(state, metadata.Status); Assert.Equal("PDF", metadata.Format);
                Assert.Equal("DOSSIER", (await client.GetFromJsonAsync<ExportManifestV1Dto>(statePath + "/manifest"))!.Kind);
                Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync(statePath + "/content")).StatusCode);
            }
            await PdfState("QUEUED"); var pdfLease = Guid.NewGuid();
            await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == pdfId); Assert.True(job.TryClaim(pdfLease, clock.Now, TimeSpan.FromMinutes(1))); await db.SaveChangesAsync(); }
            await PdfState("RUNNING");
            await using (var db = sql.CreateDbContext()) { var job = await db.Set<ExportJob>().SingleAsync(j => j.Id == pdfId); job.Fail(pdfLease, clock.Now, "SYNTHETIC_PDF_STATE", true); await db.SaveChangesAsync(); }
            await PdfState("FAILED");
            var readyPdf = await exports.CreateAsync(source.Pm, source.Project, new("DOSSIER", "PDF", IncludeOriginalFiles: false), owner + "-pdf-ready", null, default);
            Assert.Equal("success", readyPdf.Code); pdfId = readyPdf.Value!.Id; Assert.True(await exports.ProcessNextAsync(default));
        }
        var pdfPath = $"/api/v1/projects/{source.Project}/exports/{pdfId}";
        var pdfMeta = (await client.GetFromJsonAsync<ExportJobViewDto>(pdfPath))!; Assert.Equal("SUCCEEDED", pdfMeta.Status); Assert.Equal("PDF", pdfMeta.Format);
        var pdfManifest = (await client.GetFromJsonAsync<ExportManifestV1Dto>(pdfPath + "/manifest"))!; Assert.Equal("DOSSIER", pdfManifest.Kind); Assert.NotEmpty(pdfManifest.SourceRevisions);
        var pdfResponse = await client.GetAsync(pdfPath + "/content"); Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync(); Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 4)));
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType!.MediaType); Assert.True(pdfResponse.Headers.CacheControl!.Private); Assert.True(pdfResponse.Headers.CacheControl.NoStore);
        Assert.Equal("\"" + Convert.ToHexString(SHA256.HashData(pdfBytes)).ToLowerInvariant() + "\"", pdfResponse.Headers.ETag!.ToString());
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Ended));
        var pdfReads = store.Reads; foreach (var suffix in new[] { "", "/manifest", "/content" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(pdfPath + suffix)).StatusCode); Assert.Equal(pdfReads, store.Reads);
        await using (var db = sql.CreateDbContext()) await db.ProjectMembers.Where(m => m.ProjectId == source.Project && m.UserId == source.Pm).ExecuteUpdateAsync(u => u.SetProperty(m => m.Status, ProjectMemberStatus.Active));
        clock.Now = clock.Now.AddDays(31); Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(path + "/content")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path + "/manifest")).StatusCode);
    }
    private async Task BindReportGeometry(AuthenticationWebApplicationFactory factory, HttpClient pm, GenuineProtectedSource.Source source, Guid reportId)
    {
        // Actual coordinate draft/confirmation/publication and case command; not an official CRS accuracy claim.
        async Task<HttpResponseMessage> Send(HttpClient client, string path, object input, string? version = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(input) };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", version);
            var response = await client.SendAsync(request); response.EnsureSuccessStatusCode(); return response;
        }
        var prefix = $"/api/v1/projects/{source.Project}";
        var draft = await Send(pm, prefix + "/road-geometry-drafts", new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 0d,
            changeReason = "synthetic training geometry",
            coordinates = new[] { new { x = 500000d, y = 1200000d }, new { x = 500250d, y = 1200000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 250d, widthMeters = 7d } },
            surveyWidthMeters = 9d,
            roadCode = "TRAINING-" + Guid.NewGuid().ToString("N")
        });
        var draftId = (await draft.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var supervisor = factory.CreateClient(); string email;
        await using (var db = sql.CreateDbContext()) email = (await db.Users.SingleAsync(u => u.Id == source.Supervisor)).Email!;
        var login = await supervisor.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Current1!" }); login.EnsureSuccessStatusCode();
        supervisor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var confirmed = await Send(supervisor, prefix + $"/road-geometry-drafts/{draftId}/confirm", new { expectedCurrentVersionId = (Guid?)null, effectiveFrom = DateTimeOffset.UtcNow.AddMinutes(-1), reason = "synthetic fixture alignment" }, draft.Headers.ETag!.ToString());
        var route = await confirmed.Content.ReadFromJsonAsync<JsonElement>(); var routeId = route.GetProperty("routeVersionId").GetGuid(); var road = route.GetProperty("roadSectionId").GetGuid();
        var setPath = prefix + $"/road-sections/{road}/versions/{routeId}/segment-sets";
        var set = await Send(pm, setPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" });
        var setId = (await set.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Send(pm, setPath + $"/{setId}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "synthetic fixture segmentation" }, set.Headers.ETag!.ToString());
        await using var scope = factory.Services.CreateAsyncScope();
        var geometry = await scope.ServiceProvider.GetRequiredService<IAnhHuyProducerService>().ResolveGeometryAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, routeId, setId);
        Assert.Equal(AnhHuyProducerStatus.Ready, geometry.Status);
        Guid caseId; await using (var db = sql.CreateDbContext()) caseId = (await db.Set<HuyCaseReportLink>().SingleAsync(l => l.ReportId == reportId)).CaseId;
        var cases = scope.ServiceProvider.GetRequiredService<ICaseWorkflowService>();
        var current = await cases.ReadAsync(source.Pm, UserRoleCode.ProjectManager, caseId, default);
        var bound = await cases.CommandAsync(source.Pm, UserRoleCode.ProjectManager, new(caseId, current.Case!.Version, "triage", "bind verified fixture geometry", ProjectId: source.Project, Method: CaseVerificationMethod.ExistingEvidence, RouteVersionId: routeId, SegmentSetId: setId, GeometryVersion: geometry.Facts!.Version), Guid.NewGuid().ToString(), null, default);
        Assert.Equal(200, bound.Status);
    }
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class ObservedStore(IAnh02ArtifactStore inner) : IAnh02ArtifactStore
    {
        public int Writes; public int Reads; public bool LoseAcknowledgement; public bool FailRead; public string? LastKey;
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Writes); LastKey = key; var result = await inner.WriteAsync(key, content, sizeBytes, mediaType, ct);
            if (LoseAcknowledgement) { LoseAcknowledgement = false; throw new IOException("SYNTHETIC lost acknowledgement after successful PUT"); }
            return result;
        }
        public async Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken ct = default)
        {
            Interlocked.Increment(ref Reads); if (FailRead) throw new IOException("SYNTHETIC bounded read transport failure"); return await inner.OpenReadAsync(key, ct);
        }
    }
}
