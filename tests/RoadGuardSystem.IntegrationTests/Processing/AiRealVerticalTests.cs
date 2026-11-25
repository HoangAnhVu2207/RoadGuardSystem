using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using Xunit;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Processing.Worker;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class RealAiFactAttribute : FactAttribute
{
    public RealAiFactAttribute() { if (Environment.GetEnvironmentVariable("ROADGUARD_RUN_REAL_AI") != "1") Skip = "Opt-in actual FT95/SQL/MinIO test: ROADGUARD_RUN_REAL_AI=1"; }
}

// Never uses configured SQL: both services and all rows belong exclusively to this test.
public sealed class AiRealVerticalTests
{
    private readonly IdentitySqlServerFixture _fixture = new(new SqlServerTestFixture(_ => null, createSpatialProbeSchema: false));
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly string[] Labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
    private readonly Guid _route = Guid.NewGuid(), _set = Guid.NewGuid(), _segment = Guid.NewGuid();
    private const string Video = "C:/Users/HoangAnhVu/Downloads/DJI_0060_01m00s-02m00s.MP4";
    private const string VideoHash = "8f8b0c2dc1bf5fb11579323c64281a8897cb8268b26667687ecffb4f25211932";
    private const string ModelHash = "bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0";

    [RealAiFact]
    public async Task Coordinator_RealFt95_Http_MinIo_Sql_IngestsRegisteredProposalsExactlyOnce()
    {
        var pilotCount = Environment.GetEnvironmentVariable("ROADGUARD_REAL_PILOT_COUNT") == "5" ? 5 : 1;
        var repoRoot = Environment.GetEnvironmentVariable("ROADGUARD_REAL_REPO_ROOT") ?? throw new InvalidOperationException("ROADGUARD_REAL_REPO_ROOT required");
        var root = Path.Combine("D:/RoadGuard-AI-LocalTest", "be-vertical-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        new DriveInfo("D:/").AvailableFreeSpace.Should().BeGreaterThan(32L * 1024 * 1024 * 1024);
        var existingBytes = new DirectoryInfo("D:/RoadGuard-AI-LocalTest").EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        existingBytes.Should().BeLessThan(38L * 1024 * 1024 * 1024);
        await using (var source = File.OpenRead(Video)) Convert.ToHexString(await SHA256.HashDataAsync(source)).ToLowerInvariant().Should().Be(VideoHash);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenFile = Path.Combine(root, "worker.token"); await File.WriteAllTextAsync(tokenFile, token);
        var tools = "C:/Users/HoangAnhVu/AppData/Local/Temp/roadguard-media-tools/ffmpeg-9.0.2-essentials_build/bin";
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var psi = new ProcessStartInfo("D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe") { WorkingDirectory = Path.Combine(repoRoot, "AI/roadguard-ai-engine"), UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-m", "uvicorn", "src.api.be_worker_runtime:create_app_from_env", "--factory", "--host", "127.0.0.1", "--port", port.ToString(), "--no-access-log" }) psi.ArgumentList.Add(arg);
        psi.Environment["ROADGUARD_WORKER_DATA_ROOT"] = Path.Combine(root, "worker"); psi.Environment["ROADGUARD_WORKER_TOKEN_FILE"] = tokenFile;
        psi.Environment["ROADGUARD_WORKER_CHECKPOINT"] = Path.Combine(repoRoot, "AI/roadguard-ai-engine/weights/trained/RoadGuard_YOLO11_UAVPDD2023_FT95_v1.pt");
        psi.Environment["ROADGUARD_WORKER_FFMPEG"] = Path.Combine(tools, "ffmpeg.exe"); psi.Environment["ROADGUARD_WORKER_FFPROBE"] = Path.Combine(tools, "ffprobe.exe");
        psi.Environment["TEMP"] = root; psi.Environment["TMP"] = root;
        using var worker = new Process { StartInfo = psi };
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        await using var minio = new ContainerBuilder("rg-low-resource-20261030-minio:latest").WithEnvironment("MINIO_ROOT_USER", "verticalfixture").WithEnvironment("MINIO_ROOT_PASSWORD", secret)
            .WithCommand("server", "/data").WithPortBinding(9000, true).WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/minio/health/ready"))).Build();
        Task<string>? stdout = null; Task<string>? stderr = null; var workerStarted = false;
        try
        {
            await minio.StartAsync(); await _fixture.InitializeAsync();
            var endpoint = $"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}";
            var storage = Options.Create(new MinioStorageOptions { Endpoint = endpoint, BucketName = "real-vertical", AccessKey = "verticalfixture", SecretKey = secret });
            using var s3 = new AmazonS3Client("verticalfixture", secret, new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
            await s3.PutBucketAsync("real-vertical");
            var seed = await SeedAsync(pilotCount);
            foreach (var source in seed.Sources)
                await using (var input = File.OpenRead(Video)) await s3.PutObjectAsync(new PutObjectRequest { BucketName = "real-vertical", Key = source.ObjectKey, InputStream = input, ContentType = "video/mp4" });
            workerStarted = worker.Start(); workerStarted.Should().BeTrue(); stdout = worker.StandardOutput.ReadToEndAsync(); stderr = worker.StandardError.ReadToEndAsync();
            using var http = AiWorkerClient.CreateHttpClient(); http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token); var origin = new Uri($"http://127.0.0.1:{port}/");
            var ready = false;
            for (var i = 0; i < 120; i++)
            {
                if (worker.HasExited) throw new InvalidOperationException("Worker startup failed: " + await stderr);
                try { using var response = await http.GetAsync(new Uri(origin, "internal/v1/health")); if (response.IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { }
                await Task.Delay(500);
            }
            ready.Should().BeTrue();
            var client = new AiWorkerClient(http, origin, token);
            var options = new AiWorkerRuntimeOptions
            {
                Enabled = true,
                ReleasedModelVersionId = seed.ModelId,
                CheckpointSha256 = ModelHash,
                ClassMap = seed.Map,
                TempRoot = Path.Combine(root, "be-spool"),
                FfprobePath = Path.Combine(tools, "ffprobe.exe"),
                MaxFrames = 60,
                PollInterval = TimeSpan.FromSeconds(1)
            };
            var artifacts = new MinioAnh02ArtifactStore(storage); var uploads = new MinioUploadObjectStorage(storage);
            var timer = Stopwatch.StartNew(); AiVideoWorkItem? state = null;
            while (timer.Elapsed < TimeSpan.FromMinutes(10 * pilotCount))
            {
                await using var db = _fixture.CreateDbContext();
                using var coordinator = new AiVideoCoordinator(new AiVideoWorkRepository(db), uploads, artifacts, client, new AiWorkerMediaProbe(options.FfprobePath), options, geometry: new FixtureGeometry(_route, _set, _segment));
                await coordinator.TickAsync("real-vertical");
                var works = await db.AiVideoWorkItems.AsNoTracking().ToListAsync();
                state = works.FirstOrDefault(x => x.State is AiVideoWorkState.TerminalFailed or AiVideoWorkState.RetryableFailed) ?? works.LastOrDefault();
                (await db.OutboxMessages.AnyAsync(x => x.MessageType == "ai.real.source_rejected.v1")).Should().BeFalse("actual media must not be quarantined before real admission");
                if (works.Any(x => x.State is AiVideoWorkState.TerminalFailed or AiVideoWorkState.RetryableFailed) || works.Count == pilotCount && works.All(x => x.State == AiVideoWorkState.Ingested)) break;
                await Task.Delay(1000);
            }
            state.Should().NotBeNull(); state!.State.Should().Be(AiVideoWorkState.Ingested, "error={0}", state.ErrorCode);
            var status = await client.GetStatusAsync(state.CurrentAttemptId); status.State.Should().Be("COMPLETED");
            await using var verify = _fixture.CreateDbContext();
            var jobs = await verify.ProcessingJobs.ToListAsync(); jobs.Should().HaveCount(pilotCount);
            foreach (var job in jobs) { job.Mode.Should().Be("REAL"); job.Status.Should().Be(ProcessingJobStatus.Completed); job.CompletedAt.Should().NotBeNull(); }
            var proofs = await verify.AiWorkerArtifacts.ToListAsync(); proofs.Count(x => x.Kind == "FRAME").Should().Be(60 * pilotCount);
            (await verify.AIDetections.CountAsync()).Should().BeGreaterThan(0); (await verify.Defects.CountAsync()).Should().Be(0);
            foreach (var proof in proofs)
            {
                proof.FileId.Should().NotBe(Guid.Parse(proof.ArtifactId));
                var file = await verify.Files.SingleAsync(f => f.Id == proof.FileId);
                await using var durable = await artifacts.OpenReadAsync(file.StorageUri);
                durable.Metadata.SizeBytes.Should().Be(proof.Bytes); durable.Metadata.Sha256.Should().Be(proof.Sha256);
                Convert.ToHexString(await SHA256.HashDataAsync(durable.Content)).ToLowerInvariant().Should().Be(proof.Sha256);
            }
            var count = await verify.AIDetections.CountAsync();
            for (var i = 0; i < 3; i++)
            {
                await using var replayDb = _fixture.CreateDbContext();
                using var replay = new AiVideoCoordinator(new AiVideoWorkRepository(replayDb), uploads, artifacts, client, new AiWorkerMediaProbe(options.FfprobePath), options, geometry: new FixtureGeometry(_route, _set, _segment));
                (await replay.TickAsync("replay")).Should().BeFalse();
            }
            (await verify.AIDetections.CountAsync()).Should().Be(count); (await verify.ProcessingJobs.CountAsync()).Should().Be(pilotCount);
            var completedWorks = await verify.AiVideoWorkItems.AsNoTracking().ToListAsync();
            completedWorks.Should().HaveCount(pilotCount);
            foreach (var completedWork in completedWorks)
            {
                var beforeReplay = await client.GetStatusAsync(completedWork.CurrentAttemptId); beforeReplay.State.Should().Be("COMPLETED");
                using var admissionDoc = JsonDocument.Parse(completedWork.AdmissionJson);
                (await client.AdmitAsync(AiWorkerWireContract.ParseAdmission(admissionDoc.RootElement))).Should().BeEquivalentTo(beforeReplay);
                (await verify.AIDetections.CountAsync(x => x.ProcessingJobId == completedWork.ProcessingJobId)).Should().BeGreaterThan(0);
            }
            await File.WriteAllTextAsync(Path.Combine(root, "evidence.json"), JsonSerializer.Serialize(new { status = "CURRENT_VERIFIED", elapsedSeconds = timer.Elapsed.TotalSeconds, pilotCount, geometryEvidence = "MOCK_VERIFIED fixture package; inference, HTTP, MinIO and SQL actual", workload = "repeated authorized one-minute sample, not diverse inputs", frames = 60 * pilotCount, detections = count, artifacts = proofs.Count, artifactBytes = proofs.Sum(p => p.Bytes), workId = state.Id, attemptId = state.CurrentAttemptId, sourceSha256 = VideoHash, checkpointSha256 = ModelHash, zeroDefects = true, replayNoNewJob = true }));
        }
        finally
        {
            // Only this test-owned child and containers are eligible for shutdown.
            if (workerStarted && !worker.HasExited) { worker.Kill(entireProcessTree: true); await worker.WaitForExitAsync(); }
            if (stdout is not null) await File.WriteAllTextAsync(Path.Combine(root, "worker.stdout.log"), await stdout);
            if (stderr is not null) await File.WriteAllTextAsync(Path.Combine(root, "worker.stderr.log"), await stderr);
            await _fixture.DisposeAsync();
            File.Delete(tokenFile);
        }
    }
    // Explicit geometry mechanics fixture; this harness does not certify geometry SQL authorization.
    private sealed class FixtureGeometry(Guid route, Guid set, Guid segment) : RoadGuardSystem.Repositories.Projects.IGeometryWorkflowRepository
    {
        public Task<bool> CanReadAssignedGeometryAsync(Guid actorId, Guid projectId, Guid routeVersionId, Guid setId, CancellationToken ct)
            => Task.FromResult(routeVersionId == route && setId == set);
        public Task<RoadGuardSystem.Repositories.Projects.GeometryWorkflowResult> ExecuteAsync(
            RoadGuardSystem.Repositories.Projects.GeometryWorkflowCommand c,
            Func<RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryDraftInputFact, int, RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPreviewFact> preview,
            Func<Guid, LineString, double, RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentDefinitionFact, RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentPreviewFact> segments,
            CancellationToken ct)
        {
            var shape = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact("LineString", new[] { new[] { 500000d, 1200000d }, new[] { 500100d, 1200000d } });
            var r = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.RoadGeometryVersionViewFact(c.ProjectId, Guid.NewGuid(), route, 1, true, DateTimeOffset.UnixEpoch, null, 32648, 32648, 0, null, 10, shape, null, 100, "COMPLETE", null, null, "fixture-geometry");
            var s = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.SegmentGeometryViewFact(segment, 1, 0, 100, 0, 100, 100, shape, null, []);
            var package = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPackageViewFact("anh01.geometry.v1", c.ProjectId, r,
                new(set, route, "PUBLISHED", null, "fixture-geometry", [s], null, null, "fixture-version", "COMPLETE", []));
            return Task.FromResult(new RoadGuardSystem.Repositories.Projects.GeometryWorkflowResult(200, Value: package, Version: "fixture-version"));
        }
    }
    private static OutboxMessage Intent(Guid id) => OutboxMessage.Create(Guid.NewGuid(), "ai.real.dataset_submitted.v1", DateTimeOffset.UtcNow, id, "{}");
    private sealed record Seed(AiVideoSource[] Sources, Guid ModelId, Dictionary<string, string> Map);
    private async Task<Seed> SeedAsync(int count = 1, bool intent = true, bool malformed = false)
    {
        await using var db = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        await _fixture.SeedRolesAsync(db);
        var owner = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = owner, UserName = $"worker-{owner:N}", NormalizedUserName = $"worker-{owner:N}".ToUpperInvariant(), DisplayName = "Worker fixture", PasswordHash = "fixture-password-hash", RoleCode = UserRoleCode.DroneOperator, Status = UserStatus.Active, CreatedAt = now });
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"WORKER-{Guid.NewGuid():N}", Name = "Isolated worker repository", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var files = Enumerable.Range(0, count).Select(_ => StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "video.mp4", "video/mp4", new FileInfo(Video).Length, VideoHash, owner, now, null)).ToArray();
        var manifest = JsonSerializer.Serialize(files.Select(f => new { fileId = f.Id, checksumSha256 = f.Checksum, sizeBytes = f.SizeBytes, purpose = "SURVEY_VIDEO" }));
        if (malformed) manifest = JsonSerializer.Serialize(new object[] { new { purpose = "SURVEY_VIDEO", fileId = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = files[1].SizeBytes } });
        var dataset = SurveyDataVersion.CreateSubmitted(Guid.NewGuid(), survey.Id, 1, now, now, Guid.NewGuid(), manifest,
            JsonSerializer.Serialize(new[] { new { routeVersionId = _route, segmentSetId = _set, segmentIds = new[] { _segment } } }));
        var model = AIModelVersion.Create(Guid.NewGuid(), "FT95", Guid.NewGuid().ToString("N"), "fixture/model", null, null, AIModelVersionStatus.Released, null, null);
        var map = Labels.Select((label, i) => (label, code: label == "repair" ? "REPAIR_PATCH" : $"FT95_{i}")).ToDictionary(x => x.label, x => x.code);
        db.AddRange(project, road, version, survey, dataset, model);
        foreach (var code in map.Values) db.DefectTypes.Add(DefectType.Create(code, code));
        foreach (var file in files)
        {
            var upload = UploadSession.Create(Guid.NewGuid(), file.Id, owner, file.StorageUri, "SURVEY_VIDEO", "video/mp4", file.SizeBytes, file.Checksum, checked((int)file.SizeBytes), now.AddHours(1));
            db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, owner, "SURVEY_VIDEO", now), upload);
            await db.SaveChangesAsync();
            upload.StartUploading("fixture", now); await db.SaveChangesAsync();
            upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync();
            upload.MarkVerified(); await db.SaveChangesAsync();
        }
        if (intent) { db.OutboxMessages.Add(Intent(dataset.Id)); await db.SaveChangesAsync(); }
        return new(files.Select(f => new AiVideoSource(dataset.Id, project.Id, f.Id, owner, f.StorageUri, f.Checksum, f.SizeBytes)).ToArray(), model.Id, map);
    }
}
