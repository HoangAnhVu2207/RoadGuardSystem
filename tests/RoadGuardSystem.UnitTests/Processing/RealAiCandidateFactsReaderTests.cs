using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;
using Work = RoadGuardSystem.BusinessObjects.Processing.Worker.AiVideoWorkItem;
using State = RoadGuardSystem.BusinessObjects.Processing.Worker.AiVideoWorkState;
using ArtifactProof = RoadGuardSystem.BusinessObjects.Processing.Worker.AiWorkerArtifact;

namespace RoadGuardSystem.UnitTests.Processing;

/// <summary>Mock read repositories and geometry authority; no SQL, storage or inference execution.</summary>
public sealed class RealAiCandidateFactsReaderTests
{
    public class Proxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = (_, _) => throw new InvalidOperationException("Unexpected mock call");
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
    private static T Mock<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    { var mock = DispatchProxy.Create<T, Proxy>(); ((Proxy)(object)mock).Handler = handler; return mock; }
    private sealed class Repo(RealAiCandidateReadFacts? facts, Guid? project) : IRealAiCandidateRepository
    {
        public Task<RealAiCandidateReadFacts?> ReadAsync(Guid projectId, Guid detectionId, CancellationToken ct) => Task.FromResult(facts);
        public Task<Guid?> DetectionProjectAsync(Guid detectionId, CancellationToken ct) => Task.FromResult(project);
    }
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static RealAiCandidateReadFacts Facts(string label = "repair", Action<JsonObject>? mutate = null, bool geometry = true)
    {
        var now = DateTimeOffset.UtcNow; var project = Guid.NewGuid(); var dataset = Guid.NewGuid(); var video = Guid.NewGuid();
        var jobId = Guid.NewGuid(); var attempt = Guid.NewGuid(); var model = Guid.NewGuid(); var workId = Guid.NewGuid();
        var frameArtifact = Guid.NewGuid(); var frameFile = Guid.NewGuid(); var result = Guid.NewGuid(); var detection = Guid.NewGuid();
        var map = new Dictionary<string, string> { ["alligator crack"] = "ALLIGATOR_CRACK", ["longitudinal crack"] = "LONGITUDINAL_CRACK", ["oblique crack"] = "OBLIQUE_CRACK", ["pothole"] = "POTHOLE", ["repair"] = "REPAIR_PATCH", ["transverse crack"] = "TRANSVERSE_CRACK" };
        var source = new AiVideoSource(dataset, project, video, Guid.NewGuid(), "uploads/video", new string('a', 64), 100, null, "source-row-version");
        var manifest = JsonSerializer.Serialize(new
        {
            contractVersion = "rg-ai-worker-v1",
            datasetId = dataset,
            projectId = project,
            sourceVideoFileId = video,
            inputSha256 = source.Sha256,
            inputBytes = source.Bytes,
            modelVersionId = model,
            sourceVideoVersion = source.SourceVideoVersion,
            checkpointSha256 = new string('b', 64),
            preprocessingVersion = "ffmpeg-pts-v1",
            configVersion = "ft95-cpu-v1",
            inputWidth = 1920,
            inputHeight = 1080,
            inputDurationMs = 10000,
            sampleFps = 1,
            maxFrames = 10,
            logicalConfigurationIdentity = "frozen",
            classMap = map,
            geometry = geometry ? new AiVideoGeometrySnapshot(Guid.NewGuid(), Guid.NewGuid(), "geometry-version", [Guid.NewGuid()]) : null
        }, Json);
        var admission = new AiWorkerAdmission("rg-ai-worker-v1", project, jobId, attempt, dataset, video, Hash(manifest), model, new string('b', 64),
            "ffmpeg-pts-v1", "ft95-cpu-v1", source.Sha256, 100, 1920, 1080, 10000, 1, 10);
        var work = Work.Create(workId, dataset, video, project, jobId, attempt, "frozen", JsonSerializer.Serialize(admission, AiWorkerWireContract.JsonOptions), now);
        work.AcquireLease("mock", now, now.AddMinutes(1)); work.Transition("mock", attempt, State.Admitted, now);
        work.Transition("mock", attempt, State.ArtifactsReady, now); work.Transition("mock", attempt, State.Ingested, now);
        var job = ProcessingJob.CreateQueued(jobId, Guid.NewGuid(), model, project, Hash(manifest), manifest, "REAL"); job.Complete(now);
        var payload = JsonSerializer.SerializeToNode(new
        {
            workItemId = workId,
            attemptId = attempt,
            contractVersion = "rg-ai-worker-v1",
            resultId = result,
            sourceVideoFileId = video,
            sourceVideoSha256 = source.Sha256,
            frameArtifactId = frameArtifact,
            frameFileId = frameFile,
            sourceClass = label,
            timestampMs = 1500,
            bbox = new[] { .1m, .2m, .3m, .4m },
            confidence = .9m,
            groundLocationStatus = "NOT_MEASURED",
            proposalOnly = true
        }, Json)!.AsObject(); mutate?.Invoke(payload);
        var d = AIDetection.Create(detection, jobId, model, null, null, map[label], .9m, null, null, payload.ToJsonString());
        RealAiArtifactReadFact Artifact(Guid artifact, Guid file, string kind, string type)
        {
            var proof = ArtifactProof.Create(Guid.NewGuid(), workId, attempt, artifact.ToString("D"), new string('c', 64), 123, kind,
                kind == "FRAME" ? 1920 : null, kind == "FRAME" ? 1080 : null); proof.MarkRegistered(file);
            return new(proof, StoredFile.Create(file, $"anh02/real/{attempt:D}/{artifact:D}", "artifact", type, 123, proof.Sha256, null, now, null));
        }
        return new(work, job, d, source, [Artifact(frameArtifact, frameFile, "FRAME", "image/jpeg"), Artifact(Guid.NewGuid(), Guid.NewGuid(), "RAW_RESULT", "application/json")], null, null);
    }
    private static RealAiCandidateFactsReader Reader(RealAiCandidateReadFacts? facts, Guid? knownProject = null, bool forbidden = false, AnhHuyProducerStatus geometryStatus = AnhHuyProducerStatus.Ready)
    {
        var authorization = Mock<IAnh02AiRepository>((m, _) => m.Name == "AuthorizeAsync" ? forbidden ? throw new AiRequestException(403, "forbidden") : Task.CompletedTask : throw new InvalidOperationException());
        var geometry = Mock<IAnhHuyProducerService>((m, args) =>
        {
            m.Name.Should().Be("ResolveGeometryAsync"); args![5].Should().Be("geometry-version"); args[6].Should().Be(true);
            return Task.FromResult(new AnhHuyProducerResult<ProjectGeometryContext>(geometryStatus));
        });
        return new(new Repo(facts, knownProject), authorization, geometry);
    }
    [Theory]
    [InlineData("alligator crack", "ALLIGATOR_CRACK")]
    [InlineData("longitudinal crack", "LONGITUDINAL_CRACK")]
    [InlineData("oblique crack", "OBLIQUE_CRACK")]
    [InlineData("pothole", "POTHOLE")]
    [InlineData("repair", "REPAIR_PATCH")]
    [InlineData("transverse crack", "TRANSVERSE_CRACK")]
    public async Task Six_proposals_keep_distinct_types_and_unknown_ground_position(string label, string code)
    {
        var f = Facts(label); var result = await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id);
        result.Status.Should().Be(AnhHuyProducerStatus.Ready); result.Facts!.TypeCode.Should().Be(code);
        result.Facts.Mode.Should().Be("REAL"); result.Facts.SegmentId.Should().BeNull(); result.Facts.PositionStatus.Should().Be("UNKNOWN");
        result.Facts.FrameFileId.Should().NotBe(Guid.Parse(f.Artifacts[0].Proof.ArtifactId));
        result.Facts.TimestampMs.Should().Be(1500); result.Facts.BoundingBox.Should().Be(new NormalizedBoundingBoxV1(.1m, .2m, .3m, .4m));
    }
    [Theory]
    [InlineData("attemptId")]
    [InlineData("workItemId")]
    [InlineData("sourceVideoFileId")]
    [InlineData("frameFileId")]
    [InlineData("frameArtifactId")]
    [InlineData("sourceClass")]
    [InlineData("timestampMs")]
    [InlineData("bbox")]
    [InlineData("confidence")]
    [InlineData("proposalOnly")]
    public async Task Corrupt_persisted_provenance_is_not_ready(string field)
    {
        var f = Facts(mutate: p => p[field] = field switch
        {
            "sourceClass" => JsonValue.Create("crack"),
            "timestampMs" => JsonValue.Create(10001),
            "bbox" => JsonSerializer.SerializeToNode(new[] { 0m, 0m, 2m, 1m }),
            "confidence" => JsonValue.Create(.8m),
            "proposalOnly" => JsonValue.Create(false),
            _ => JsonValue.Create(Guid.NewGuid())
        });
        (await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
    }
    [Theory]
    [InlineData("0.7093191146850586", "0.70932", true)]
    [InlineData("0.8513269424438477", "0.85133", true)]
    [InlineData("0.7648734450340271", "0.76487", true)]
    [InlineData("0.709325", "0.70933", true)]
    [InlineData("0.7093191146850586", "0.70933", false)]
    [InlineData("1.000001", "1", false)]
    public async Task Raw_confidence_matches_only_the_exact_SQL_decimal_projection(string raw, string persisted, bool valid)
    {
        var f = Facts(mutate: p => p["confidence"] = JsonValue.Create(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture)));
        var d = f.Detection;
        f = f with
        {
            Detection = AIDetection.Create(d.Id, d.ProcessingJobId, d.ModelVersionId, null, null,
            d.DefectTypeCode, decimal.Parse(persisted, System.Globalization.CultureInfo.InvariantCulture), null, null, d.RawPayload)
        };
        (await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, d.Id)).Status
            .Should().Be(valid ? AnhHuyProducerStatus.Ready : AnhHuyProducerStatus.SourceNotReady);
    }

    [Fact]
    public async Task Missing_snapshot_never_invents_current_geometry()
    {
        var f = Facts(geometry: false);
        (await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
    }
    [Fact]
    public async Task Current_authorization_and_geometry_revisions_remain_authoritative()
    {
        var f = Facts(); var actor = Guid.NewGuid();
        (await Reader(f, forbidden: true).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.Forbidden);
        (await Reader(f, geometryStatus: AnhHuyProducerStatus.StaleGeometry).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.StaleGeometry);
        (await Reader(f).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id, expectedSourceVersion: "old")).Status.Should().Be(AnhHuyProducerStatus.StaleSource);
        (await Reader(f).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id, expectedDispositionVersion: "old")).Status.Should().Be(AnhHuyProducerStatus.StaleDisposition);
        (await Reader(f with { Source = f.Source with { SourceVideoVersion = "changed" } }).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
    }
    [Theory]
    [InlineData(AnhHuyProducerStatus.NotFound, true)]
    [InlineData(AnhHuyProducerStatus.Forbidden, false)]
    [InlineData(AnhHuyProducerStatus.SourceNotReady, false)]
    [InlineData(AnhHuyProducerStatus.StaleSource, false)]
    [InlineData(AnhHuyProducerStatus.StaleGeometry, false)]
    [InlineData(AnhHuyProducerStatus.StaleDisposition, false)]
    [InlineData(AnhHuyProducerStatus.Ready, false)]
    public async Task Composite_only_falls_back_on_not_found(AnhHuyProducerStatus status, bool fallback)
    {
        var called = false;
        var real = Mock<IAiCandidateFactsReader>((_, _) => Task.FromResult(new AnhHuyProducerResult<AiCandidateFactsV1>(status)));
        var mock = Mock<IAiCandidateFactsReader>((_, _) => { called = true; return Task.FromResult(new AnhHuyProducerResult<AiCandidateFactsV1>(AnhHuyProducerStatus.Ready)); });
        var result = await new CompositeAiCandidateFactsReader(real, mock).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, Guid.NewGuid(), Guid.NewGuid());
        called.Should().Be(fallback); result.Status.Should().Be(fallback ? AnhHuyProducerStatus.Ready : status);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ambiguous_or_malformed_scope_never_selects_first_route(bool malformed)
    {
        var source = Facts().Source with
        {
            ScopeManifest = malformed ? "not-json" : JsonSerializer.Serialize(new[] {
            new { routeVersionId = Guid.NewGuid(), segmentSetId = Guid.NewGuid(), segmentIds = new[] { Guid.NewGuid() } },
            new { routeVersionId = Guid.NewGuid(), segmentSetId = Guid.NewGuid(), segmentIds = new[] { Guid.NewGuid() } } })
        };
        var called = false; var repo = Mock<IGeometryWorkflowRepository>((_, _) => { called = true; throw new InvalidOperationException(); });
        (await AiVideoGeometrySnapshot.CaptureAsync(source, repo, CancellationToken.None)).Should().BeNull(); called.Should().BeFalse();
    }
    [Fact]
    public async Task Admission_freezes_authorized_unambiguous_package_and_union_of_scope_segments()
    {
        var source = Facts().Source; var route = Guid.NewGuid(); var set = Guid.NewGuid(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        source = source with
        {
            ScopeManifest = JsonSerializer.Serialize(new[] {
            new { RouteVersionId = route, SegmentSetId = set, SegmentIds = new[] { first } },
            new { RouteVersionId = route, SegmentSetId = set, SegmentIds = new[] { second } } })
        };
        var shape = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryShapeFact("LineString", Array.Empty<double>());
        var package = new RoadGuardSystem.BusinessObjects.PersistenceFacts.Projects.GeometryPackageViewFact("geometry.v1", source.ProjectId,
            new(source.ProjectId, Guid.NewGuid(), route, 1, true, DateTimeOffset.UtcNow, null, null, 32648, null, null, null, shape, null, 100,
                "COMPLETE", null, null, new string('f', 64)),
            new(set, route, "PUBLISHED", null, null,
                [new(first, 1, null, null, null, null, null, null, null, []), new(second, 2, null, null, null, null, null, null, null, [])],
                null, null, "set-version", "COMPLETE", []));
        var repo = Mock<IGeometryWorkflowRepository>((m, args) =>
        {
            m.Name.Should().Be("ExecuteAsync"); var command = (GeometryWorkflowCommand)args![0]!;
            command.ActorId.Should().Be(source.OwnerId); command.Role.Should().Be(UserRoleCode.DroneOperator);
            command.RouteVersionId.Should().Be(route); command.SetId.Should().Be(set);
            return Task.FromResult(new GeometryWorkflowResult(200, Value: package, Version: "frozen-package-version"));
        });
        var snapshot = await AiVideoGeometrySnapshot.CaptureAsync(source, repo, CancellationToken.None);
        snapshot!.GeometryVersion.Should().Be("frozen-package-version"); snapshot.RouteVersionId.Should().Be(route);
        snapshot.SegmentIds.Should().BeEquivalentTo(new[] { first, second });
    }
    [Fact]
    public async Task Changed_registered_artifact_hash_is_not_accepted()
    {
        var f = Facts(); var frame = f.Artifacts[0];
        var changed = StoredFile.Create(frame.File.Id, frame.File.StorageUri, "changed", frame.File.MimeType, frame.File.SizeBytes,
            new string('d', 64), null, DateTimeOffset.UtcNow, null);
        f = f with { Artifacts = [frame with { File = changed }, f.Artifacts[1]] };
        (await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
    }

    private static AiVideoTelemetryRevision Revision(RealAiCandidateReadFacts f, int number = 1, Guid? previous = null, string kind = "OPERATOR_ESTIMATE")
        => AiVideoTelemetryRevision.Create(Guid.NewGuid(), f.Work.Id, number, previous, Guid.NewGuid(), DateTimeOffset.UtcNow,
            f.Source.Sha256, kind, "[{\"startMs\":0,\"endMs\":5000,\"latitude\":10,\"longitude\":106}]", 10000,
            kind == "EXTERNAL_SRT" ? "telemetry/original.srt" : null, kind == "EXTERNAL_SRT" ? new string('d', 64) : null,
            kind == "EXTERNAL_SRT" ? 100 : null, 0, "Operator review", new string('e', 64), new string('f', 64));

    [Fact]
    public async Task Telemetry_head_invalidates_stale_review_and_preserves_saved_decision_snapshot()
    {
        var f = Facts(); var actor = Guid.NewGuid();
        var before = (await Reader(f).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Facts!;
        before.TelemetryReview!.HeadRevisionId.Should().Be(Guid.Empty);
        var revision = Revision(f); var updated = f with { TelemetryRevisions = [revision] };
        var current = (await Reader(updated).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Facts!;
        current.DetectionId.Should().Be(before.DetectionId); current.SourceVersion.Should().NotBe(before.SourceVersion);
        current.TelemetryReview!.HeadRevisionId.Should().Be(revision.Id);
        current.SourceVersion.Length.Should().BeLessThanOrEqualTo(200);
        current.SourceVersion.Should().Contain(revision.Id.ToString("N"));
        var decision = CandidateDecision.Create(Guid.NewGuid(), CandidateSourceFacts.Create(
            CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection, before.DetectionId, before.SourceVersion),
            before.ProjectId, before.GeometryVersion, null), CandidateDecisionKind.Reject, null, Guid.Empty, null, null, actor, "Review", DateTimeOffset.UtcNow);
        (await Reader(updated).ResolveAsync(actor, UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id,
            expectedSourceVersion: before.SourceVersion)).Status.Should().Be(AnhHuyProducerStatus.StaleSource);
        decision.Source.SourceVersion.Should().Be(before.SourceVersion);
        current.PositionStatus.Should().Be("UNKNOWN"); current.SegmentId.Should().BeNull();
    }

    [Fact]
    public async Task Review_records_measured_revision_separately_from_newer_estimate_head()
    {
        var f = Facts(); var measured = Revision(f, kind: "EXTERNAL_SRT"); var estimate = Revision(f, 2, measured.Id);
        var result = await Reader(f with { TelemetryRevisions = [measured, estimate] }).ResolveAsync(Guid.NewGuid(),
            UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id);
        result.Facts!.TelemetryReview!.HeadRevisionId.Should().Be(estimate.Id);
        result.Facts.TelemetryReview.MeasuredRevisionId.Should().Be(measured.Id);
        result.Facts.SourceVersion.Should().Contain(measured.Id.ToString("N"));
    }

    [Fact]
    public async Task Paired_srt_proof_and_manifest_change_review_version()
    {
        var f = Facts(); var paired = new AiPairedTelemetryFile(new(Guid.Empty, Guid.NewGuid(), "uploads/pair.srt", new string('a', 64), 100, "application/x-subrip"), 0);
        f = f with { PairedSrt = paired, PairsManifestHash = Hash("pair-manifest") };
        var before = (await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Facts!;
        before.TelemetryReview!.PairedSrtFileId.Should().Be(paired.File.FileId);
        foreach (var changed in new[] { f with { PairedSrt = paired with { OffsetMs = 1000 } }, f with { PairsManifestHash = Hash("changed") } })
            (await Reader(changed).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id,
                expectedSourceVersion: before.SourceVersion)).Status.Should().Be(AnhHuyProducerStatus.StaleSource);
    }

    [Fact]
    public async Task Invalid_telemetry_chain_is_not_ready_and_mock_wire_remains_unchanged()
    {
        var f = Facts(); var foreign = Revision(Facts());
        (await Reader(f with { TelemetryRevisions = [foreign] }).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager,
            f.Work.ProjectId, f.Detection.Id)).Status.Should().Be(AnhHuyProducerStatus.SourceNotReady);
        var facts = (await Reader(f).ResolveAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, f.Work.ProjectId, f.Detection.Id)).Facts!;
        var mock = facts with { Mode = "MOCK", TelemetryReview = null };
        JsonSerializer.Serialize(mock, Json).Should().NotContain("telemetryReview");
    }
}
