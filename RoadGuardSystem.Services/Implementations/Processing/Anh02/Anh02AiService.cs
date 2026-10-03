using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Processing.Anh02;

public sealed class Anh02AiService(IAnh02AiRepository repository, IAnhHuyProducerService geometry,
    IUploadObjectStorage sourceStorage, IAnh02ArtifactStore artifacts, SyntheticAiFixture fixture,
    IServiceProvider services, IOptions<Anh02AiOptions> options,
    IHostEnvironment environment, TimeProvider clock) : IAnh02AiService, IAiCandidateFactsReader
{
    private IEnumerable<IMatchingCandidateSnapshotReader> matchingReaders => services.GetServices<IMatchingCandidateSnapshotReader>();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private void Enabled()
    {
        if (!options.Value.MockEnabled || (environment.EnvironmentName is not ("Development" or "Testing" or "Test")))
            throw new AiRequestException(404, "not_found");
    }
    public async Task<AiMockRunView> CreateAsync(Guid actor, UserRoleCode role, Guid project, CreateAiMockRunRequest request, string key, CancellationToken ct)
    {
        Enabled(); Validate(request);
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200) throw new AiRequestException(422, "ai_contract_invalid");
        await repository.AuthorizeAsync(actor, role, project, true, ct);
        var normalized = request with { Scope = request.Scope with { SegmentIds = request.Scope.SegmentIds.OrderBy(x => x.ToString("D"), StringComparer.Ordinal).ToArray() } };
        var fingerprint = AiManifestCanonicalizer.Hash(JsonSerializer.SerializeToUtf8Bytes(new { project, request = normalized }, Json));
        return await repository.AdmitAsync(actor, role, project, normalized, key, fingerprint, async (run, job, attempt, sources, token) =>
        {
            SyntheticAiFixture.CheckSources(sources, normalized.FixtureVersion);
            var context = await geometry.ResolveGeometryAsync(actor, role, project, normalized.Scope.RouteVersionId,
                normalized.Scope.SegmentSetId, normalized.ExpectedGeometryVersion, false, token);
            if (context.Status != AnhHuyProducerStatus.Ready || normalized.Scope.SegmentIds.Any(id => !context.Facts!.Segments.Any(s => s.Id == id)))
                throw new AiRequestException(context.Status == AnhHuyProducerStatus.NotFound ? 404 : 409, "source_not_ready");
            MatchingCandidateSnapshotV1? snapshot = null; AiResultProvenance? analysisResult = null;
            if (normalized.Stage == "DUPLICATE_MATCHING")
            {
                analysisResult = await repository.GetResultAsync(normalized.AnalysisRunId!.Value, token);
                if (analysisResult is null) throw new AiRequestException(409, "source_not_ready");
                var detections = ParseResult(analysisResult).Detections.Select(d => d.DetectionId).ToArray();
                var reader = matchingReaders.SingleOrDefault();
                if (reader is null) throw new AiRequestException(409, "source_not_ready");
                snapshot = await reader.CaptureAsync(actor, role, project, normalized.Scope.RouteVersionId,
                    normalized.Scope.SegmentSetId, context.Facts!.Version, detections, normalized.CandidateSnapshotId!.Value, token);
                if (snapshot is null || snapshot.ProjectId != project || snapshot.SnapshotId != normalized.CandidateSnapshotId
                    || snapshot.RouteVersionId != normalized.Scope.RouteVersionId || snapshot.SegmentSetId != normalized.Scope.SegmentSetId
                    || snapshot.GeometryVersion != context.Facts.Version) throw new AiRequestException(409, "candidate_stale");
            }
            var manifest = new AiManifestDraft(run, normalized.Stage, project, job, attempt, sources.DatasetId, sources.DatasetManifestHash,
                normalized.Scope.RouteVersionId, normalized.Scope.SegmentSetId, context.Facts!.Version, normalized.Scope.SegmentIds,
                normalized.Scope.TargetBand, normalized.ModelVersionId, normalized.PreprocessingVersion, normalized.ConfigVersion,
                normalized.FixtureVersion, actor, clock.GetUtcNow(), sources.Files.Select(f => new AiManifestFileDraft(f.FileId,
                    f.FileVersion, f.Sha256, f.SizeBytes, f.MediaType, f.Purpose)).ToArray(),
                sources.Pairs.Where(p => p.TelemetryFileId.HasValue).Select(p => new AiManifestPairDraft(p.VideoFileId, p.TelemetryFileId!.Value)).ToArray(),
                sources.Files.Any(f => f.Purpose == "TELEMETRY") ? "PRESENT" : "MISSING", analysisResult?.Id,
                snapshot?.SnapshotId, snapshot?.Hash, snapshot?.Items.Select(i => new AiMatchingItemDraft(i.DefectId, i.Version, i.SegmentId, i.RouteVersionId)).ToArray());
            var bytes = AiManifestCanonicalizer.Encode(manifest);
            return new(Encoding.UTF8.GetString(bytes), AiManifestCanonicalizer.Hash(bytes), context.Facts.Version);
        }, ct);
    }

    public async Task<AiMockRunView> GetAsync(Guid actor, UserRoleCode role, Guid project, Guid run, CancellationToken ct)
    {
        Enabled(); await repository.AuthorizeAsync(actor, role, project, false, ct);
        return Anh02AiRepository.View(await repository.GetAsync(project, run, ct) ?? throw new AiRequestException(404, "not_found"));
    }
    public async Task<AiResultV1> GetResultAsync(Guid actor, UserRoleCode role, Guid project, Guid run, CancellationToken ct)
    {
        await GetAsync(actor, role, project, run, ct);
        return ParseResult(await repository.GetResultAsync(run, ct) ?? throw new AiRequestException(409, "result_not_ready"));
    }
    private static AiResultV1 ParseResult(AiResultProvenance result)
    {
        if (AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes(result.CanonicalResult)) != result.ResultHash)
            throw new AiRequestException(409, "source_not_ready");
        return (JsonSerializer.Deserialize<AiResultV1>(result.CanonicalResult, Json) ?? throw new AiRequestException(409, "source_not_ready"))
            with { ResultHash = result.ResultHash };
    }

    public async Task<bool> ProcessOneAsync(CancellationToken ct)
    {
        Enabled(); var owner = Guid.NewGuid(); var run = await repository.ClaimAsync(owner, clock.GetUtcNow(), ct);
        if (run is null) return false;
        try
        {
            await repository.AuthorizeAsync(run.CreatedBy, UserRoleCode.ProjectManager, run.ProjectId, true, ct);
            if (AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes(run.CanonicalManifest)) != run.ManifestHash)
                throw new AiRequestException(422, "ai_contract_invalid");
            using var manifest = JsonDocument.Parse(run.CanonicalManifest);
            var scope = manifest.RootElement; var segments = scope.GetProperty("segmentIds").EnumerateArray().Select(s => s.GetGuid()).ToArray();
            var input = new CreateAiMockRunRequest(run.DatasetVersionId, new(run.RouteVersionId, run.SegmentSetId, segments,
                scope.GetProperty("targetBand").GetString()!), run.ModelVersionId, scope.GetProperty("preprocessingVersion").GetString()!,
                scope.GetProperty("configVersion").GetString()!, run.FixtureVersion, run.Stage, run.GeometryVersion, run.AnalysisRunId,
                scope.TryGetProperty("candidateSnapshotId", out var snap) ? snap.GetGuid() : null);
            var sources = await repository.ReadSourcesAsync(run.ProjectId, input, ct);
            SyntheticAiFixture.CheckSources(sources, run.FixtureVersion);
            var admittedFiles = scope.GetProperty("sourceFiles").EnumerateArray().ToArray();
            if (sources.DatasetManifestHash != scope.GetProperty("datasetManifestHash").GetString()
                || admittedFiles.Length != sources.Files.Length
                || sources.Files.Any(f => !admittedFiles.Any(a => a.GetProperty("fileId").GetGuid() == f.FileId
                    && a.GetProperty("fileVersion").GetString() == f.FileVersion && a.GetProperty("sha256").GetString() == f.Sha256
                    && a.GetProperty("sizeBytes").GetInt64() == f.SizeBytes)))
                throw new AiRequestException(409, "source_not_ready");
            foreach (var source in sources.Files.Where(f => f.Purpose == "SURVEY_VIDEO"))
            {
                await using var content = await sourceStorage.OpenReadAsync(source.ObjectKey, ct);
                using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920]; long bytes = 0; int read;
                while ((read = await content.ReadAsync(buffer, ct)) != 0)
                {
                    bytes = checked(bytes + read); if (bytes > SyntheticAiFixture.VideoBytes) throw new AiRequestException(422, "mock_fixture_source_mismatch");
                    digest.AppendData(buffer, 0, read);
                }
                if (bytes != source.SizeBytes || !Convert.ToHexString(digest.GetHashAndReset()).Equals(source.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new AiRequestException(422, "mock_fixture_source_mismatch");
            }
            var resultId = SyntheticAiFixture.Identity(run.Id, "result");
            var detections = new List<AIDetection>(); var proofs = new List<AiDetectionProvenance>(); var frames = new List<StoredFile>();
            var resultDetections = new List<AiDetectionResultV1>(); var matches = new List<AiMatchResultV1>();
            if (run.Stage == "VIDEO_ANALYSIS" && run.FixtureVersion != "synthetic-empty-v1")
            {
                var image = fixture.Frame();
                foreach (var source in sources.Files.Where(f => f.Purpose == "SURVEY_VIDEO").OrderBy(f => f.FileId.ToString("D"), StringComparer.Ordinal))
                {
                    var frameId = SyntheticAiFixture.Identity(run.Id, $"frame:{source.FileId:D}");
                    var detectionId = SyntheticAiFixture.Identity(run.Id, $"detection:{source.FileId:D}");
                    var key = $"anh02/ai/{run.Id:D}/{source.FileId:D}/{SyntheticAiFixture.FrameHash}.png";
                    await using var imageStream = new MemoryStream(image, false);
                    var durable = await artifacts.WriteAsync(key, imageStream, image.LongLength, "image/png", ct);
                    if (durable.Sha256 != SyntheticAiFixture.FrameHash || durable.SizeBytes != SyntheticAiFixture.FrameBytes)
                        throw new AiRequestException(422, "ai_contract_invalid");
                    frames.Add(StoredFile.Create(frameId, key, $"{frameId:D}.png", "image/png", durable.SizeBytes, durable.Sha256,
                        run.CreatedBy, run.ResultTimestamp!.Value, null));
                    var box = new[] { .375m, .333333333m, .25m, .333333333m };
                    var dto = new AiDetectionResultV1(detectionId, source.FileId, frameId, durable.Sha256,
                        SyntheticAiFixture.TimestampMilliseconds, "CRACK", .8m, box, null, "UNKNOWN");
                    resultDetections.Add(dto);
                    detections.Add(AIDetection.Create(detectionId, run.ProcessingJobId, run.ModelVersionId, run.RouteVersionId, null,
                        "CRACK", .8m, null, null, JsonSerializer.Serialize(dto, Json)));
                    proofs.Add(new() { DetectionId = detectionId, RunId = run.Id, ResultId = resultId, SourceVideoFileId = source.FileId,
                        SourceVideoFileVersion = source.FileVersion, FrameFileId = frameId, FrameFileVersion = durable.Sha256,
                        DerivationHash = AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes($"{SyntheticAiFixture.VideoHash}:frame=1:{durable.Sha256}")),
                        TimestampMilliseconds = SyntheticAiFixture.TimestampMilliseconds, SourceDurationMilliseconds = SyntheticAiFixture.DurationMilliseconds,
                        BoxX = box[0], BoxY = box[1], BoxWidth = box[2], BoxHeight = box[3] });
                }
            }
            else if (run.Stage == "DUPLICATE_MATCHING")
            {
                var analysis = ParseResult(await repository.GetResultAsync(run.AnalysisRunId!.Value, ct) ?? throw new AiRequestException(409, "source_not_ready"));
                var reader = matchingReaders.SingleOrDefault() ?? throw new AiRequestException(409, "source_not_ready");
                var snapshot = await reader.CaptureAsync(run.CreatedBy, UserRoleCode.ProjectManager, run.ProjectId, run.RouteVersionId, run.SegmentSetId,
                    run.GeometryVersion, analysis.Detections.Select(d => d.DetectionId).ToArray(), input.CandidateSnapshotId!.Value, ct);
                if (snapshot is null || snapshot.Hash != scope.GetProperty("candidateSnapshotHash").GetString()) throw new AiRequestException(409, "candidate_stale");
                foreach (var detection in analysis.Detections)
                {
                    var ranked = snapshot.Items.OrderBy(i => i.DefectId.ToString("D"), StringComparer.Ordinal).ToArray();
                    for (var index = 0; index < ranked.Length; index++) matches.Add(new(detection.DetectionId, ranked[index].DefectId,
                        ranked[index].Version, index + 1, null, ["MOCK_SCOPED_RECOMMENDATION"]));
                }
            }
            var result = new AiResultV1("anh02.ai.v1", run.Id, run.Stage, "MOCK", run.ProcessingJobId, run.AttemptId,
                run.ManifestHash, run.ModelVersionId, run.FixtureVersion, "", run.ResultTimestamp!.Value, resultDetections, matches);
            var raw = AiResultCanonicalizer.Encode(result); var resultHash = AiManifestCanonicalizer.Hash(raw);
            var provenance = new AiResultProvenance { Id = resultId, RunId = run.Id, ProcessingJobId = run.ProcessingJobId, AttemptId = run.AttemptId,
                ManifestHash = run.ManifestHash, ResultHash = resultHash, CanonicalResult = Encoding.UTF8.GetString(raw), CompletedAt = result.CompletedAt };
            await repository.CompleteAsync(run.Id, owner, new(provenance, detections, proofs, frames), async token =>
            {
                var context = await geometry.ResolveGeometryAsync(run.CreatedBy, UserRoleCode.ProjectManager, run.ProjectId, run.RouteVersionId,
                    run.SegmentSetId, run.GeometryVersion, false, token);
                if (context.Status != AnhHuyProducerStatus.Ready) return false;
                if (run.Stage != "DUPLICATE_MATCHING") return true;
                var analysis = ParseResult(await repository.GetResultAsync(run.AnalysisRunId!.Value, token)
                    ?? throw new AiRequestException(409, "source_not_ready"));
                var current = await matchingReaders.Single().CaptureAsync(run.CreatedBy, UserRoleCode.ProjectManager, run.ProjectId,
                    run.RouteVersionId, run.SegmentSetId, run.GeometryVersion, analysis.Detections.Select(d => d.DetectionId).ToArray(),
                    input.CandidateSnapshotId!.Value, token);
                if (current is null || current.SnapshotId != input.CandidateSnapshotId || current.ProjectId != run.ProjectId
                    || current.RouteVersionId != run.RouteVersionId || current.SegmentSetId != run.SegmentSetId
                    || current.GeometryVersion != run.GeometryVersion || current.Hash != scope.GetProperty("candidateSnapshotHash").GetString())
                    throw new AiRequestException(409, "candidate_stale");
                return true;
            }, ct);
        }
        catch (AiRequestException ex) { await repository.FailAsync(run.Id, owner, ex.Code, ct); }
        catch (IOException) { /* Durable lease is recoverable; retry the same run after expiry. */ }
        return true;
    }

    public async Task<AnhHuyProducerResult<AiCandidateFactsV1>> ResolveAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid detectionId, string? expectedSourceVersion = null, string? expectedGeometryVersion = null,
        string? expectedDispositionVersion = null, CancellationToken cancellationToken = default)
    {
        try { await repository.AuthorizeAsync(actorId, role, projectId, false, cancellationToken); }
        catch (AiRequestException) { return new(AnhHuyProducerStatus.Forbidden); }
        var facts = await repository.ReadCandidateAsync(projectId, detectionId, cancellationToken);
        if (facts is null) return new(await repository.DetectionProjectAsync(detectionId, cancellationToken) == projectId
            ? AnhHuyProducerStatus.SourceNotReady : AnhHuyProducerStatus.NotFound);
        if (facts.Result.RunId != facts.Run.Id || facts.Result.ProcessingJobId != facts.Run.ProcessingJobId
            || facts.Detection.ProcessingJobId != facts.Run.ProcessingJobId || facts.Detection.ModelVersionId != facts.Run.ModelVersionId
            || facts.Proof.FrameFileVersion != facts.FrameSha256 || facts.FrameBytes != SyntheticAiFixture.FrameBytes
            || facts.FrameSha256 != SyntheticAiFixture.FrameHash || facts.Result.ManifestHash != facts.Run.ManifestHash)
            return new(AnhHuyProducerStatus.SourceNotReady);
        try
        {
            var parsed = ParseResult(facts.Result);
            var detection = parsed.Detections.SingleOrDefault(d => d.DetectionId == detectionId);
            if (parsed.RunId != facts.Run.Id || parsed.JobId != facts.Run.ProcessingJobId || parsed.AttemptId != facts.Run.AttemptId
                || AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes(facts.Run.CanonicalManifest)) != facts.Run.ManifestHash
                || detection is null || detection.SourceVideoFileId != facts.Proof.SourceVideoFileId
                || detection.FrameFileId != facts.Proof.FrameFileId || detection.FrameFileVersion != facts.Proof.FrameFileVersion
                || detection.TimestampMs != facts.Proof.TimestampMilliseconds || detection.Confidence != facts.Detection.Confidence
                || detection.TypeCode != facts.Detection.DefectTypeCode || detection.SegmentId != facts.Proof.SegmentId
                || !detection.Bbox.SequenceEqual(new[] { facts.Proof.BoxX, facts.Proof.BoxY, facts.Proof.BoxWidth, facts.Proof.BoxHeight })
                || facts.Proof.SourceVideoFileVersion != facts.SourceVideoVersion
                || facts.SourceVideoSha256 != SyntheticAiFixture.VideoHash || facts.SourceVideoBytes != SyntheticAiFixture.VideoBytes
                || facts.FrameMediaType != "image/png") return new(AnhHuyProducerStatus.SourceNotReady);
        }
        catch (Exception ex) when (ex is AiRequestException or JsonException or InvalidOperationException or NullReferenceException)
        { return new(AnhHuyProducerStatus.SourceNotReady); }
        if (expectedGeometryVersion is not null && expectedGeometryVersion != facts.Run.GeometryVersion) return new(AnhHuyProducerStatus.StaleGeometry);
        var context = await geometry.ResolveGeometryAsync(actorId, role, projectId, facts.Run.RouteVersionId, facts.Run.SegmentSetId,
            facts.Run.GeometryVersion, true, cancellationToken);
        if (context.Status != AnhHuyProducerStatus.Ready) return new(context.Status);
        var sourceVersion = AiManifestCanonicalizer.Hash(Encoding.UTF8.GetBytes($"{facts.Run.ManifestHash}:{facts.Result.ResultHash}:{facts.Detection.Id:D}:{facts.Proof.FrameFileVersion}"));
        if (expectedSourceVersion is not null && sourceVersion != expectedSourceVersion) return new(AnhHuyProducerStatus.StaleSource);
        if (expectedDispositionVersion is not null && expectedDispositionVersion != facts.DispositionVersion) return new(AnhHuyProducerStatus.StaleDisposition);
        return new(AnhHuyProducerStatus.Ready, new("anh02.ai-candidate.v1", detectionId, projectId, facts.Run.DatasetVersionId,
            facts.Run.ModelVersionId, facts.Run.Id, facts.Run.ProcessingJobId, facts.Run.AttemptId, facts.Result.Id,
            facts.Run.ManifestHash, facts.Result.ResultHash, facts.Run.FixtureVersion, "MOCK/SYNTHETIC", facts.Proof.SourceVideoFileId,
            facts.Proof.SourceVideoFileVersion, facts.Proof.FrameFileId, facts.Proof.FrameFileVersion, facts.FrameSha256, facts.FrameBytes,
            facts.FrameMediaType, facts.Proof.TimestampMilliseconds, facts.Detection.DefectTypeCode!, facts.Detection.Confidence,
            new(facts.Proof.BoxX, facts.Proof.BoxY, facts.Proof.BoxWidth, facts.Proof.BoxHeight), facts.Run.RouteVersionId,
            facts.Run.SegmentSetId, facts.Proof.SegmentId, facts.Run.GeometryVersion, "UNKNOWN", sourceVersion, facts.Disposition, facts.DispositionVersion));
    }
    private static void Validate(CreateAiMockRunRequest request)
    {
        if (request is null || request.DatasetId == Guid.Empty || request.ModelVersionId == Guid.Empty || request.Scope is null
            || request.Scope.RouteVersionId == Guid.Empty || request.Scope.SegmentSetId == Guid.Empty
            || request.Scope.SegmentIds is not { Length: > 0 and <= 1000 } || request.Scope.SegmentIds.Contains(Guid.Empty)
            || request.Scope.SegmentIds.Distinct().Count() != request.Scope.SegmentIds.Length
            || request.Scope.TargetBand is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE") || !SyntheticAiFixture.Supported(request.FixtureVersion)
            || new[] { request.PreprocessingVersion, request.ConfigVersion, request.ExpectedGeometryVersion }.Any(string.IsNullOrWhiteSpace)
            || request.Stage is not ("VIDEO_ANALYSIS" or "DUPLICATE_MATCHING")
            || (request.Stage == "VIDEO_ANALYSIS" && (request.AnalysisRunId.HasValue || request.CandidateSnapshotId.HasValue))
            || (request.Stage == "DUPLICATE_MATCHING" && (!request.AnalysisRunId.HasValue || !request.CandidateSnapshotId.HasValue
                || request.AnalysisRunId == Guid.Empty || request.CandidateSnapshotId == Guid.Empty)))
            throw new AiRequestException(422, "ai_contract_invalid");
    }
}
