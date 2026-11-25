using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

/// <summary>Explicit mock repository, object store, media probe and HTTP evidence. No SQL or inference claims.</summary>
public sealed class AiVideoCoordinatorTests
{
    private static string Fixture(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "tests", "fixtures", "ai-worker-v1"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "tests", "fixtures", "ai-worker-v1", name + ".json"));
    }
    private static AiWorkerAdmission Admission()
    {
        using var doc = JsonDocument.Parse(Fixture("admission")); return AiWorkerWireContract.ParseAdmission(doc.RootElement);
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 19, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Repo(Clock clock) : IAiVideoWorkRepository
    {
        public AiVideoWorkItem? Work { get; set; }
        public AiVideoSource? Source { get; set; }
        public List<AiVideoSource> AdditionalSources { get; } = [];
        public List<Guid> SelectedDatasets { get; } = [];
        public AiVideoSource? PreparedSource { get; private set; }
        public int Prepared { get; private set; }
        public string? Manifest { get; private set; }
        public List<string> Rejected { get; } = [];
        public IReadOnlyList<AiRegisteredArtifact> Registered { get; private set; } = [];
        public IReadOnlyList<AiProposedDetection> Proposals { get; private set; } = [];
        public Task<AiVideoSource?> NextSourceAsync(string logicalConfigurationIdentity, CancellationToken ct, IReadOnlySet<Guid>? excludedDatasetIds = null)
        {
            IEnumerable<AiVideoSource> sources = Source is { } first ? [first] : [];
            var next = sources.Concat(AdditionalSources).FirstOrDefault(x => excludedDatasetIds?.Contains(x.DatasetId) != true);
            if (next is not null) SelectedDatasets.Add(next.DatasetId);
            return Task.FromResult(next);
        }
        public Task RecordSourceFailureAsync(AiVideoSource source, string logicalConfigurationIdentity, string errorCode, CancellationToken ct)
        { Rejected.Add(errorCode); Source = null; return Task.CompletedTask; }
        public Task<Guid> PrepareAsync(AiVideoSource source, AiPreparedVideo p, IReadOnlyDictionary<string, string> classMap, CancellationToken ct)
        {
            Prepared++;
            PreparedSource = source;
            Manifest = p.ManifestJson;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(p.ManifestJson))).ToLowerInvariant(); hash.Should().Be(p.ManifestHash);
            Work = AiVideoWorkItem.Create(Guid.NewGuid(), source.DatasetId, source.FileId, source.ProjectId, p.JobId, p.AttemptId,
                p.LogicalConfigurationIdentity, p.AdmissionJson, clock.Now); return Task.FromResult(Work.Id);
        }
        public Task<AiVideoWorkItem?> ClaimAsync(string owner, DateTimeOffset now, TimeSpan lease, CancellationToken ct)
        {
            if (Work is null || Work.State is AiVideoWorkState.Ingested or AiVideoWorkState.TerminalFailed or AiVideoWorkState.RetryableFailed or AiVideoWorkState.Cancelled || Work.NextPollAt > now) return Task.FromResult<AiVideoWorkItem?>(null);
            Work.AcquireLease(owner, now, now.Add(lease)); return Task.FromResult<AiVideoWorkItem?>(Work);
        }
        public Task<AiVideoSource> ReadSourceAsync(AiVideoWorkItem work, CancellationToken ct) => Task.FromResult(Source!);
        public Task ChangeAsync(Guid id, string owner, Guid attemptId, AiVideoWorkState? state, string? errorCode, DateTimeOffset now, DateTimeOffset nextPoll, CancellationToken ct)
        {
            if (state is { } target) Work!.Transition(owner, attemptId, target, now, nextPoll, errorCode);
            if (Work!.State is AiVideoWorkState.Pending or AiVideoWorkState.Admitted or AiVideoWorkState.Running or AiVideoWorkState.ArtifactsReady) Work.ReleaseLease(owner, attemptId, now, nextPoll);
            return Task.CompletedTask;
        }
        public Task CompleteAsync(Guid id, string owner, Guid attemptId, IReadOnlyList<AiRegisteredArtifact> artifacts, IReadOnlyList<AiProposedDetection> detections,
            IReadOnlyDictionary<string, string> classMap, DateTimeOffset now, CancellationToken ct)
        {
            Registered = artifacts; Proposals = detections; Work!.Transition(owner, attemptId, AiVideoWorkState.Ingested, now); return Task.CompletedTask;
        }
    }
    private sealed class Uploads(byte[] bytes) : IUploadObjectStorage
    {
        public int ReadCalls { get; private set; }
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        { ReadCalls++; return Task.FromResult<Stream>(new MemoryStream(bytes)); }
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Artifacts : IAnh02ArtifactStore
    {
        public bool Corrupt { get; set; }
        public Dictionary<string, byte[]> Bytes { get; } = new(StringComparer.Ordinal);
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default)
        {
            using var memory = new MemoryStream(); await content.CopyToAsync(memory, cancellationToken); var bytes = memory.ToArray();
            if (Bytes.TryGetValue(key, out var old)) old.Should().Equal(bytes); else Bytes.Add(key, bytes);
            return new(key, bytes.Length, Corrupt ? new string('f', 64) : Hash(bytes), mediaType);
        }
        public Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class Probe : IAiWorkerMediaProbe
    {
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public Task<AiWorkerVideoMetadata> ProbeAsync(string localFile, CancellationToken cancellationToken)
        {
            Calls++;
            File.Exists(localFile).Should().BeTrue();
            if (Failure is not null) throw Failure;
            return Task.FromResult(new AiWorkerVideoMetadata(1920, 1080, 60000));
        }
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => action(request, cancellationToken);
    }
    private sealed class Geometry : IGeometryWorkflowRepository
    {
        public Guid Route { get; } = Guid.NewGuid();
        public Guid Set { get; } = Guid.NewGuid();
        public Guid Segment { get; } = Guid.NewGuid();
        public bool Ready { get; set; } = true;
        public Task<bool> CanReadAssignedGeometryAsync(Guid actorId, Guid projectId, Guid routeVersionId, Guid setId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Unexpected mock geometry call");
        public Task<GeometryWorkflowResult> ExecuteAsync(GeometryWorkflowCommand command,
            Func<GeometryDraftInputFact, int, GeometryPreviewFact> preview,
            Func<Guid, NetTopologySuite.Geometries.LineString, double, SegmentDefinitionFact, SegmentPreviewFact> segments,
            CancellationToken cancellationToken)
        {
            command.Action.Should().Be("package"); command.Role.Should().Be(UserRoleCode.DroneOperator);
            command.RouteVersionId.Should().Be(Route); command.SetId.Should().Be(Set);
            if (!Ready) return Task.FromResult(new GeometryWorkflowResult(409, "geometry_not_ready"));
            var shape = new GeometryShapeFact("LineString", Array.Empty<double>());
            var package = new GeometryPackageViewFact("geometry.v1", command.ProjectId,
                new(command.ProjectId, Guid.NewGuid(), Route, 1, true, DateTimeOffset.UtcNow, null, null, 32648, null, null, null, shape, null, 100,
                    "COMPLETE", null, null, new string('f', 64)),
                new(Set, Route, "PUBLISHED", null, null, [new(Segment, 1, null, null, null, null, null, null, null, [])],
                    null, null, "set-version", "COMPLETE", []));
            return Task.FromResult(new GeometryWorkflowResult(200, Value: package, Version: "frozen-package-version"));
        }
    }
    private sealed class Harness : IDisposable
    {
        public Clock Clock { get; } = new();
        public Repo Repo { get; }
        public Artifacts Artifacts { get; } = new();
        public Probe Probe { get; } = new();
        public Geometry Geometry { get; } = new();
        public Uploads Uploads { get; }
        public byte[] Input { get; } = Encoding.UTF8.GetBytes("mock-video-input");
        public AiWorkerRuntimeOptions Options { get; }
        public Harness()
        {
            Repo = new(Clock); var a = Admission();
            Uploads = new(Input);
            Options = new()
            {
                Enabled = true,
                ReleasedModelVersionId = a.ModelVersionId,
                CheckpointSha256 = a.CheckpointSha256,
                PreprocessingVersion = a.PreprocessingVersion,
                ConfigVersion = a.ConfigVersion,
                SampleFps = a.SampleFps,
                MaxFrames = a.MaxFrames,
                TempRoot = Path.Combine(Path.GetTempPath(), "roadguard-coordinator-test-" + Guid.NewGuid().ToString("N")),
                ClassMap = new(StringComparer.Ordinal)
                {
                    ["alligator crack"] = "ALLIGATOR_CRACK",
                    ["longitudinal crack"] = "LONGITUDINAL_CRACK",
                    ["oblique crack"] = "OBLIQUE_CRACK",
                    ["pothole"] = "POTHOLE",
                    ["repair"] = "REPAIR_PATCH",
                    ["transverse crack"] = "TRANSVERSE_CRACK"
                }
            };
            Repo.Source = new(a.DatasetId, a.ProjectId, a.SourceVideoFileId, Guid.NewGuid(), "test-video", Hash(Input), Input.Length,
                JsonSerializer.Serialize(new[] { new { routeVersionId = Geometry.Route, segmentSetId = Geometry.Set, segmentIds = new[] { Geometry.Segment } } }));
        }
        public AiVideoCoordinator Coordinator(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            => new(Repo, Uploads, Artifacts, new AiWorkerClient(new HttpClient(new Handler(handler)), new Uri("http://127.0.0.1:8999/"), new string('t', 32)), Probe, Options, Clock, Geometry);
        public void Existing(AiVideoCoordinator coordinator, AiVideoWorkState state = AiVideoWorkState.Pending)
        {
            var a = Admission();
            Repo.Source = Repo.Source! with { Bytes = a.InputBytes, Sha256 = a.InputSha256 };
            Repo.Work = AiVideoWorkItem.Create(Guid.NewGuid(), a.DatasetId, a.SourceVideoFileId, a.ProjectId, a.JobId, a.AttemptId,
                coordinator.LogicalConfigurationIdentity, Fixture("admission"), Clock.Now);
            if (state == AiVideoWorkState.Pending) return;
            Repo.Work.AcquireLease("setup", Clock.Now, Clock.Now.AddMinutes(1));
            Repo.Work.Transition("setup", a.AttemptId, AiVideoWorkState.Admitted, Clock.Now);
            if (state != AiVideoWorkState.Admitted) Repo.Work.Transition("setup", a.AttemptId, state, Clock.Now);
            Repo.Work.ReleaseLease("setup", a.AttemptId, Clock.Now);
        }
        public void Dispose()
        {
            if (Directory.Exists(Options.TempRoot)) { Directory.EnumerateFileSystemEntries(Options.TempRoot).Should().BeEmpty(); Directory.Delete(Options.TempRoot); }
        }
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage State(Guid attempt, string state) => Json(JsonSerializer.Serialize(new { attemptId = attempt, state, cancellationRequested = false, errorCode = (string?)null }));
    private static Task<HttpResponseMessage> Unexpected(HttpRequestMessage request, CancellationToken ct) => throw new InvalidOperationException("Unexpected mock HTTP call");
    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public async Task PendingGeometryDatasetsDoNotStarveLaterReadySource(int blockedCount)
    {
        using var h = new Harness(); var ready = h.Repo.Source! with { DatasetId = Guid.NewGuid(), FileId = Guid.NewGuid() };
        h.Repo.Source = h.Repo.Source! with { ScopeManifest = null };
        for (var i = 1; i < blockedCount; i++)
            h.Repo.AdditionalSources.Add(h.Repo.Source! with { DatasetId = Guid.NewGuid(), FileId = Guid.NewGuid() });
        h.Repo.AdditionalSources.Add(h.Repo.Source! with { FileId = Guid.NewGuid() });
        h.Repo.AdditionalSources.Add(ready);
        var requests = 0;
        using var coordinator = h.Coordinator(async (request, ct) =>
        {
            requests++; request.Method.Should().Be(HttpMethod.Post);
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var admission = AiWorkerWireContract.ParseAdmission(document.RootElement);
            admission.DatasetId.Should().Be(ready.DatasetId);
            return State(admission.AttemptId, "QUEUED");
        });
        (await coordinator.TickAsync("owner")).Should().BeTrue();
        h.Repo.Prepared.Should().Be(1); h.Repo.PreparedSource.Should().BeSameAs(ready);
        h.Repo.Work!.State.Should().Be(AiVideoWorkState.Admitted);
        h.Repo.SelectedDatasets.Should().HaveCount(blockedCount + 1).And.OnlyHaveUniqueItems();
        h.Repo.Rejected.Should().BeEmpty(); h.Repo.Source.Should().NotBeNull();
        h.Uploads.ReadCalls.Should().Be(1); h.Probe.Calls.Should().Be(1); requests.Should().Be(1);
    }
    [Fact]
    public async Task MissingGeometryLeavesSourceEligibleWithoutMediaOrJobAndRecoversWhenReady()
    {
        using var h = new Harness(); h.Geometry.Ready = false;
        var source = h.Repo.Source; var requests = 0;
        using var coordinator = h.Coordinator(async (request, ct) =>
        {
            requests++; request.Method.Should().Be(HttpMethod.Post);
            using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            var admission = AiWorkerWireContract.ParseAdmission(doc.RootElement);
            return State(admission.AttemptId, "QUEUED");
        });
        var error = await Assert.ThrowsAsync<AiVideoSourceNotReadyException>(() => coordinator.TickAsync("owner"));
        error.Code.Should().Be("GEOMETRY_NOT_READY"); error.Message.Should().Be(error.Code);
        h.Repo.Prepared.Should().Be(0); h.Repo.Work.Should().BeNull(); h.Repo.Manifest.Should().BeNull();
        h.Repo.Rejected.Should().BeEmpty(); h.Repo.Source.Should().BeSameAs(source);
        h.Uploads.ReadCalls.Should().Be(0); h.Probe.Calls.Should().Be(0); requests.Should().Be(0);
        Directory.Exists(h.Options.TempRoot).Should().BeFalse();

        h.Geometry.Ready = true;
        (await coordinator.TickAsync("owner")).Should().BeTrue();
        h.Repo.Prepared.Should().Be(1); h.Repo.Work!.State.Should().Be(AiVideoWorkState.Admitted);
        h.Repo.Rejected.Should().BeEmpty(); h.Uploads.ReadCalls.Should().Be(1); h.Probe.Calls.Should().Be(1); requests.Should().Be(1);
        using var manifest = JsonDocument.Parse(h.Repo.Manifest!);
        var geometry = manifest.RootElement.GetProperty("geometry");
        geometry.GetProperty("RouteVersionId").GetGuid().Should().Be(h.Geometry.Route);
        geometry.GetProperty("GeometryVersion").GetString().Should().Be("frozen-package-version");
    }
    [Fact]
    public async Task MockEndToEndRegistersBytesAndAllSixProposals()
    {
        using var h = new Harness(); AiWorkerAdmission? accepted = null;
        var media = Encoding.UTF8.GetBytes("synthetic artifact bytes");
        using var coordinator = h.Coordinator(async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post)
            {
                using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)); accepted = AiWorkerWireContract.ParseAdmission(doc.RootElement);
                return State(accepted.AttemptId, "AWAITING_INPUT");
            }
            if (request.Method == HttpMethod.Put)
            { (await request.Content!.ReadAsByteArrayAsync(ct)).Should().Equal(h.Input); return State(accepted!.AttemptId, "QUEUED"); }
            if (path.EndsWith("/result", StringComparison.Ordinal))
            {
                var result = JsonNode.Parse(Fixture("result"))!.AsObject();
                var fields = JsonSerializer.SerializeToNode(accepted, AiWorkerWireContract.JsonOptions)!.AsObject();
                foreach (var field in fields) result[field.Key] = field.Value!.DeepClone();
                foreach (var artifact in new[] { result["rawArtifact"]!, result["frameArtifacts"]![0]! }) { artifact["sha256"] = Hash(media); artifact["bytes"] = media.Length; }
                result["telemetry"]!["sourceVideoSha256"] = accepted!.InputSha256;
                result["telemetry"]!["source"] = "EMBEDDED"; result["telemetry"]!["outcome"] = "EXTRACTED_VALID"; result["telemetry"]!["streamIndex"] = 2;
                result["telemetry"]!["srtArtifact"] = JsonSerializer.SerializeToNode(new { artifactId = "00000000-0000-0000-0000-00000000000c", sha256 = Hash(media), bytes = media.Length, mediaType = "application/x-subrip" });
                return Json(result.ToJsonString());
            }
            if (path.Contains("/artifacts/", StringComparison.Ordinal))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(media) };
                response.Content.Headers.ContentType = new(path.EndsWith("00000000000a", StringComparison.Ordinal) ? "application/json" : path.EndsWith("00000000000c", StringComparison.Ordinal) ? "application/x-subrip" : "image/jpeg"); return response;
            }
            return State(accepted!.AttemptId, "COMPLETED");
        });
        await coordinator.TickAsync("mock-owner"); h.Repo.Work!.State.Should().Be(AiVideoWorkState.Admitted);
        h.Clock.Now = h.Clock.Now.AddSeconds(6); await coordinator.TickAsync("mock-owner"); h.Repo.Work.State.Should().Be(AiVideoWorkState.ArtifactsReady);
        await coordinator.TickAsync("mock-owner"); h.Repo.Work.State.Should().Be(AiVideoWorkState.Ingested);
        h.Repo.Prepared.Should().Be(1); h.Repo.Registered.Should().HaveCount(3); h.Artifacts.Bytes.Should().HaveCount(3);
        h.Repo.Registered.Should().ContainSingle(a => a.Kind == "TELEMETRY_SRT");
        h.Repo.Proposals.Should().HaveCount(6); h.Repo.Proposals.Single(p => p.SourceClass == "repair").TypeCode.Should().Be("REPAIR_PATCH");
        foreach (var proposal in h.Repo.Proposals)
        {
            using var payload = JsonDocument.Parse(proposal.PayloadJson); var root = payload.RootElement;
            root.GetProperty("sourceVideoFileId").GetGuid().Should().Be(accepted!.SourceVideoFileId);
            root.GetProperty("groundLocationStatus").GetString().Should().Be("NOT_MEASURED"); root.TryGetProperty("latitude", out _).Should().BeFalse();
            root.GetProperty("timestampMs").GetInt32().Should().Be(1235);
        }
    }
    [Theory]
    [InlineData(429)]
    [InlineData(507)]
    [InlineData(503)]
    public async Task QueueDiskAndTransportFailuresPreserveSamePendingAttempt(int status)
    {
        using var h = new Harness(); using var coordinator = h.Coordinator((request, ct) => Task.FromResult(Json("{\"code\":\"CAPACITY_UNAVAILABLE\"}", (HttpStatusCode)status)));
        h.Existing(coordinator); var attempt = h.Repo.Work!.CurrentAttemptId;
        await coordinator.TickAsync("owner");
        h.Repo.Work.State.Should().Be(AiVideoWorkState.Pending); h.Repo.Work.CurrentAttemptId.Should().Be(attempt);
        h.Repo.Work.NextPollAt.Should().Be(h.Clock.Now.Add(h.Options.RetryDelay)); h.Repo.Work.LeaseOwner.Should().BeNull();
    }
    [Theory]
    [InlineData("INPUT_TIMEOUT", true)]
    [InlineData("MEDIA_TIMEOUT", true)]
    [InlineData("ATTEMPT_TIMEOUT", true)]
    [InlineData("INVALID_VIDEO_METADATA", false)]
    public async Task OperationalWorkerFailureCanRetryButInvalidMediaCannot(string code, bool retryable)
    {
        using var h = new Harness();
        using var coordinator = h.Coordinator((request, ct) => Task.FromResult(Json(JsonSerializer.Serialize(new
        {
            attemptId = Admission().AttemptId,
            state = "FAILED",
            cancellationRequested = false,
            errorCode = code
        }))));
        h.Existing(coordinator, AiVideoWorkState.Admitted);
        await coordinator.TickAsync("owner");
        h.Repo.Work!.State.Should().Be(retryable ? AiVideoWorkState.RetryableFailed : AiVideoWorkState.TerminalFailed);
        h.Repo.Work.ErrorCode.Should().Be(code);
    }
    [Fact]
    public async Task LostAdmissionResponseReplaysIdenticalAttempt()
    {
        using var h = new Harness(); var attempts = new List<Guid>(); var lose = true;
        using var coordinator = h.Coordinator(async (request, ct) =>
        {
            if (request.Method == HttpMethod.Put) { await request.Content!.CopyToAsync(Stream.Null, ct); return State(attempts[0], "QUEUED"); }
            using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct)); var admission = AiWorkerWireContract.ParseAdmission(doc.RootElement);
            attempts.Add(admission.AttemptId); if (lose) { lose = false; throw new HttpRequestException("simulated response loss"); }
            return State(admission.AttemptId, "AWAITING_INPUT");
        });
        await coordinator.TickAsync("owner"); h.Repo.Work!.State.Should().Be(AiVideoWorkState.Pending);
        h.Clock.Now = h.Clock.Now.AddSeconds(31); await coordinator.TickAsync("owner");
        h.Repo.Work.State.Should().Be(AiVideoWorkState.Admitted); attempts.Should().HaveCount(2); attempts.Distinct().Should().ContainSingle(); h.Repo.Prepared.Should().Be(1);
    }
    [Theory]
    [InlineData(true, "SOURCE_CHECKSUM_MISMATCH")]
    [InlineData(false, "VIDEO_METADATA_INVALID")]
    public async Task PermanentSourceFailureIsDurableAndNeverDispatched(bool checksum, string expected)
    {
        using var h = new Harness();
        if (checksum) h.Repo.Source = h.Repo.Source! with { Sha256 = new string('0', 64) }; else h.Probe.Failure = new InvalidDataException("mock bad media");
        using var coordinator = h.Coordinator(Unexpected); await coordinator.TickAsync("owner");
        h.Repo.Rejected.Should().Equal(expected); h.Repo.Prepared.Should().Be(0); h.Repo.Work.Should().BeNull();
    }
    [Fact]
    public async Task MissingProbeToolIsNotPermanentSourceFailure()
    {
        using var h = new Harness(); h.Probe.Failure = new IOException("MEDIA_PROBE_UNAVAILABLE"); using var coordinator = h.Coordinator(Unexpected);
        await Assert.ThrowsAsync<IOException>(() => coordinator.TickAsync("owner")); h.Repo.Rejected.Should().BeEmpty(); h.Repo.Prepared.Should().Be(0);
    }
    [Fact]
    public async Task ConfigMutationDoesNotChangeFrozenRunAndPersistedMismatchFailsBeforeHttp()
    {
        using var h = new Harness(); using var coordinator = h.Coordinator(Unexpected); h.Existing(coordinator);
        h.Options.ClassMap["repair"] = "MUTATED"; h.Options.SampleFps = 999;
        var a = Admission(); h.Repo.Work = AiVideoWorkItem.Create(Guid.NewGuid(), a.DatasetId, a.SourceVideoFileId, a.ProjectId, a.JobId, a.AttemptId, "different-config", Fixture("admission"), h.Clock.Now);
        await coordinator.TickAsync("owner"); h.Repo.Work.State.Should().Be(AiVideoWorkState.TerminalFailed); h.Repo.Work.ErrorCode.Should().Be("CONFIGURATION_IDENTITY_MISMATCH");
    }
    [Fact]
    public void NormalizedBoundaryConversionPreservesValidDecimalEdgesWithoutClamping()
    {
        var box = AiWorkerCoordinates.DecimalNormalizedBox([.00476366281509399, .974594455295139, .115257902940114, .0254055447048611]);
        (box[1] + box[3]).Should().Be(1m);
        FluentActions.Invoking(() => AiWorkerCoordinates.DecimalNormalizedBox([.9, 0, .2, .1])).Should().Throw<ArgumentException>();
    }
    [Fact]
    public async Task DisabledCoordinatorDoesNoWork()
    {
        using var h = new Harness(); h.Options.Enabled = false; using var coordinator = h.Coordinator(Unexpected);
        (await coordinator.TickAsync("owner")).Should().BeFalse(); h.Repo.Prepared.Should().Be(0);
    }
    [Fact]
    public void DuplicateClassMappingFailsReadiness()
    {
        using var h = new Harness(); h.Options.ClassMap["repair"] = "POTHOLE";
        Action create = () => { using var coordinator = h.Coordinator(Unexpected); }; create.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void ProbeUsesDurationAndRejectsMissingOrMultipleVideoStreams()
    {
        AiWorkerMediaProbe.Parse("{\"streams\":[{\"width\":1920,\"height\":1080}],\"format\":{\"duration\":\"1.2345\"}}").DurationMs.Should().Be(1235);
        Action missing = () => AiWorkerMediaProbe.Parse("{\"streams\":[],\"format\":{\"duration\":\"1\"}}"); missing.Should().Throw<InvalidDataException>();
        Action infinite = () => AiWorkerMediaProbe.Parse("{\"streams\":[{\"width\":1920,\"height\":1080}],\"format\":{\"duration\":\"NaN\"}}"); infinite.Should().Throw<InvalidDataException>();
    }
}
