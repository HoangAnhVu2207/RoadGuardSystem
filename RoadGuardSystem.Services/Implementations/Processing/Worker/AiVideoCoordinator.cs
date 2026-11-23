using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Services.Processing.Worker;

/// <summary>One bounded durable step per tick. SQL leases fence publication; no network operation runs inside a SQL transaction.</summary>
public sealed class AiVideoCoordinator : IDisposable
{
    private readonly IAiVideoWorkRepository _repository;
    private readonly IUploadObjectStorage _uploads;
    private readonly IAnh02ArtifactStore _artifacts;
    private readonly AiWorkerClient _client;
    private readonly IAiWorkerMediaProbe _probe;
    private readonly AiWorkerSettings _settings;
    private readonly TimeProvider _clock;
    private readonly RoadGuardSystem.Repositories.Projects.IGeometryWorkflowRepository? _geometry;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public AiVideoCoordinator(IAiVideoWorkRepository repository, IUploadObjectStorage uploads, IAnh02ArtifactStore artifacts,
        AiWorkerClient client, IAiWorkerMediaProbe probe, AiWorkerRuntimeOptions options, TimeProvider? clock = null,
        RoadGuardSystem.Repositories.Projects.IGeometryWorkflowRepository? geometry = null)
    {
        _repository = repository; _uploads = uploads; _artifacts = artifacts; _client = client; _probe = probe;
        _settings = options.Freeze(); _clock = clock ?? TimeProvider.System;
        _geometry = geometry;
    }
    public bool Enabled => _settings.Enabled;
    public void Dispose() => _gate.Dispose();
    public string LogicalConfigurationIdentity => _settings.Identity;
    public async Task<bool> TickAsync(string owner, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled) return false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); deadline.CancelAfter(_settings.OperationTimeout);
            var work = await _repository.ClaimAsync(owner, _clock.GetUtcNow(), _settings.LeaseDuration, deadline.Token);
            if (work is null)
            {
                var excludedDatasets = new HashSet<Guid>();
                while (true)
                {
                    var source = await _repository.NextSourceAsync(_settings.Identity, deadline.Token, excludedDatasets);
                    if (source is null)
                    {
                        if (excludedDatasets.Count > 0) throw new AiVideoSourceNotReadyException();
                        return false;
                    }
                    try { await PrepareAsync(source, deadline.Token); break; }
                    catch (AiVideoSourceNotReadyException) { excludedDatasets.Add(source.DatasetId); }
                    catch (InvalidDataException e) when (e.Message is "SOURCE_CHECKSUM_MISMATCH" or "VIDEO_METADATA_INVALID")
                    {
                        await _repository.RecordSourceFailureAsync(source, _settings.Identity, e.Message, cancellationToken); return true;
                    }
                }
                work = await _repository.ClaimAsync(owner, _clock.GetUtcNow(), _settings.LeaseDuration, deadline.Token);
                if (work is null) return true;
            }
            try { await AdvanceAsync(work, owner, deadline.Token); }
            catch (AiWorkerHttpException e) when (e.Retryable) { await RescheduleAsync(work, owner, cancellationToken); }
            catch (AiWorkerHttpException e)
            {
                await FailAsync(work, owner, e.Code.Length <= 80 ? e.Code : "WORKER_DATA_FAILURE", false, cancellationToken);
            }
            catch (InvalidDataException) { await FailAsync(work, owner, "WORKER_CONTRACT_INVALID", false, cancellationToken); }
            catch (Exception e) when (e is HttpRequestException or IOException || e is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            { await RescheduleAsync(work, owner, cancellationToken); }
            return true;
        }
        finally { _gate.Release(); }
    }
    private async Task PrepareAsync(AiVideoSource source, CancellationToken ct)
    {
        // Do not freeze an immutable manifest until its review geometry is usable.
        // This readiness condition must leave the source eligible without media IO or quarantine.
        var geometry = await AiVideoGeometrySnapshot.CaptureAsync(source, _geometry, ct)
            ?? throw new AiVideoSourceNotReadyException();
        CheckCapacity(source.Bytes + 2L * 1024 * 1024 * 1024);
        var metadata = await StageAndProbeAsync(source, ct);
        var reservation = checked(source.Bytes + (long)_settings.MaxFrames * ((long)metadata.Width * metadata.Height * 3 + 65536) + 64L * 1024 * 1024 + 2L * 1024 * 1024 * 1024);
        CheckCapacity(reservation);
        var manifest = JsonSerializer.Serialize(new { contractVersion = "rg-ai-worker-v1", datasetId = source.DatasetId, projectId = source.ProjectId,
            sourceVideoFileId = source.FileId, inputSha256 = source.Sha256, inputBytes = source.Bytes,
            sourceVideoVersion = source.SourceVideoVersion, geometry,
            inputWidth = metadata.Width, inputHeight = metadata.Height, inputDurationMs = metadata.DurationMs,
            modelVersionId = _settings.ModelVersionId, checkpointSha256 = _settings.CheckpointSha256, preprocessingVersion = _settings.PreprocessingVersion,
            configVersion = _settings.ConfigVersion, sampleFps = _settings.SampleFps, maxFrames = _settings.MaxFrames, classMap = _settings.ClassMap, logicalConfigurationIdentity = _settings.Identity });
        var manifestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        var admission = new AiWorkerAdmission("rg-ai-worker-v1", source.ProjectId, Guid.NewGuid(), Guid.NewGuid(), source.DatasetId, source.FileId,
            manifestHash, _settings.ModelVersionId, _settings.CheckpointSha256, _settings.PreprocessingVersion, _settings.ConfigVersion, source.Sha256, source.Bytes,
            metadata.Width, metadata.Height, metadata.DurationMs, _settings.SampleFps, _settings.MaxFrames);
        AiWorkerWireContract.Validate(admission);
        await _repository.PrepareAsync(source, new(admission.JobId, admission.AttemptId, admission.ModelVersionId, _settings.Identity,
            JsonSerializer.Serialize(admission, AiWorkerWireContract.JsonOptions), manifest, manifestHash), _settings.ClassMap, ct);
    }
    private async Task AdvanceAsync(AiVideoWorkItem work, string owner, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(work.AdmissionJson);
        var admission = AiWorkerWireContract.ParseAdmission(document.RootElement);
        if (work.LogicalConfigurationIdentity != _settings.Identity || admission.ModelVersionId != _settings.ModelVersionId ||
            admission.CheckpointSha256 != _settings.CheckpointSha256 || admission.PreprocessingVersion != _settings.PreprocessingVersion ||
            admission.ConfigVersion != _settings.ConfigVersion || admission.SampleFps != _settings.SampleFps || admission.MaxFrames != _settings.MaxFrames ||
            admission.ProjectId != work.ProjectId || admission.DatasetId != work.DatasetId || admission.SourceVideoFileId != work.SourceVideoFileId ||
            admission.JobId != work.ProcessingJobId || admission.AttemptId != work.CurrentAttemptId)
        { await FailAsync(work, owner, "CONFIGURATION_IDENTITY_MISMATCH", false, ct); return; }
        if (work.State == AiVideoWorkState.ArtifactsReady) { await IngestAsync(work, admission, owner, ct); return; }
        AiWorkerStatus status;
        if (work.State == AiVideoWorkState.Pending)
        {
            status = await _client.AdmitAsync(admission, ct);
            if (status.State == "AWAITING_INPUT")
            {
                var source = await _repository.ReadSourceAsync(work, ct);
                if (source.ProjectId != admission.ProjectId || source.DatasetId != admission.DatasetId || source.FileId != admission.SourceVideoFileId ||
                    source.Bytes != admission.InputBytes || source.Sha256 != admission.InputSha256)
                    throw new InvalidDataException("SOURCE_IDENTITY_MISMATCH");
                await using var input = await _uploads.OpenReadAsync(source.ObjectKey, ct);
                status = await _client.UploadInputAsync(admission, input, ct);
            }
            if (await TerminalStatusAsync(work, owner, status, ct)) return;
            if (status.State == "AWAITING_INPUT") throw new InvalidDataException("INVALID_WORKER_INPUT_STATE");
            await ChangeAsync(work, owner, AiVideoWorkState.Admitted, null, _settings.PollInterval, ct); return;
        }
        status = await _client.GetStatusAsync(work.CurrentAttemptId, ct);
        if (await TerminalStatusAsync(work, owner, status, ct)) return;
        if (status.State == "COMPLETED") await ChangeAsync(work, owner, AiVideoWorkState.ArtifactsReady, null, TimeSpan.Zero, ct);
        else if (status.State == "RUNNING" && work.State == AiVideoWorkState.Admitted) await ChangeAsync(work, owner, AiVideoWorkState.Running, null, _settings.PollInterval, ct);
        else await ChangeAsync(work, owner, null, null, _settings.PollInterval, ct);
    }
    private async Task<bool> TerminalStatusAsync(AiVideoWorkItem work, string owner, AiWorkerStatus status, CancellationToken ct)
    {
        if (status.State == "CANCELLED") { await ChangeAsync(work, owner, AiVideoWorkState.Cancelled, null, TimeSpan.Zero, ct); return true; }
        if (status.State is "FAILED" or "INTERRUPTED")
        {
            var code = status.ErrorCode is { Length: <= 80 } ? status.ErrorCode : status.State == "INTERRUPTED" ? "INTERRUPTED" : "WORKER_DATA_FAILURE";
            var retryable = status.State == "INTERRUPTED" || code is "INPUT_TIMEOUT" or "MEDIA_TIMEOUT" or "ATTEMPT_TIMEOUT" or "WORKER_UNAVAILABLE";
            await FailAsync(work, owner, code, retryable, ct); return true;
        }
        return false;
    }
    private async Task IngestAsync(AiVideoWorkItem work, AiWorkerAdmission admission, string owner, CancellationToken ct)
    {
        var result = await _client.GetResultAsync(admission, ct);
        var inventory = result.FrameArtifacts.Select(a => (Artifact: a, Kind: "FRAME"))
            .Append((result.RawArtifact, "RAW_RESULT")).Concat(result.Telemetry.SrtArtifact is { } srt ? [(srt, "TELEMETRY_SRT")] : []).ToArray();
        var registered = new List<AiRegisteredArtifact>();
        foreach (var (artifact, kind) in inventory)
        {
            CheckCapacity(checked(artifact.Bytes + 2L * 1024 * 1024 * 1024));
            await using var staged = Temporary();
            await _client.DownloadArtifactAsync(admission, result, artifact.ArtifactId, staged, ct);
            staged.Position = 0;
            var key = $"anh02/real/{admission.AttemptId:D}/{artifact.ArtifactId:D}";
            var durable = await _artifacts.WriteAsync(key, staged, artifact.Bytes, artifact.MediaType, ct);
            if (durable.Key != key || durable.SizeBytes != artifact.Bytes || durable.Sha256 != artifact.Sha256 || durable.MediaType != artifact.MediaType)
                throw new InvalidDataException("ARTIFACT_DURABLE_MISMATCH");
            registered.Add(new(artifact.ArtifactId, artifact.Sha256, artifact.Bytes, artifact.MediaType, kind, key, artifact.Width, artifact.Height));
        }
        var proposals = result.Detections.Select(d =>
        {
            if (!_settings.ClassMap.TryGetValue(d.SourceClass, out var code)) throw new InvalidDataException("UNKNOWN_SOURCE_CLASS");
            var bbox = AiWorkerCoordinates.DecimalNormalizedBox(d.Bbox);
            var payload = JsonSerializer.Serialize(new { contractVersion = admission.ContractVersion, resultId = result.ResultId,
                sourceVideoFileId = admission.SourceVideoFileId, sourceVideoSha256 = admission.InputSha256, frameArtifactId = d.FrameArtifactId,
                sourceClass = d.SourceClass, timestampMs = d.TimestampMs, bbox, confidence = d.Confidence,
                telemetry = result.Telemetry, roadMaskStatus = result.RoadMaskStatus, limitations = result.Limitations,
                groundLocationStatus = "NOT_MEASURED", proposalOnly = true }, AiWorkerWireContract.JsonOptions);
            return new AiProposedDetection(d.DetectionId, d.FrameArtifactId, d.SourceClass, code, (decimal)d.Confidence, payload);
        }).ToArray();
        await _repository.CompleteAsync(work.Id, owner, admission.AttemptId, registered, proposals, _settings.ClassMap, _clock.GetUtcNow(), ct);
    }
    private Task FailAsync(AiVideoWorkItem work, string owner, string code, bool retryable, CancellationToken ct)
        => ChangeAsync(work, owner, retryable ? AiVideoWorkState.RetryableFailed : AiVideoWorkState.TerminalFailed, code, TimeSpan.Zero, ct);
    private Task RescheduleAsync(AiVideoWorkItem work, string owner, CancellationToken ct) => ChangeAsync(work, owner, null, null, _settings.RetryDelay, ct);
    private Task ChangeAsync(AiVideoWorkItem work, string owner, AiVideoWorkState? target, string? code, TimeSpan delay, CancellationToken ct)
    {
        var now = _clock.GetUtcNow(); return _repository.ChangeAsync(work.Id, owner, work.CurrentAttemptId, target, code, now, now.Add(delay), ct);
    }
    private async Task<AiWorkerVideoMetadata> StageAndProbeAsync(AiVideoSource source, CancellationToken ct)
    {
        // Native ffprobe cannot open a Windows delete-pending file. Close our
        // owned handle before invoking it, then remove only this random file.
        var staged = Temporary(deleteOnClose: false);
        var path = staged.Name;
        try
        {
            await using (staged)
            await using (var input = await _uploads.OpenReadAsync(source.ObjectKey, ct))
                await CopySourceAsync(input, staged, source.Bytes, source.Sha256, ct);
            try { return await _probe.ProbeAsync(path, ct); }
            catch (InvalidDataException) { throw new InvalidDataException("VIDEO_METADATA_INVALID"); }
        }
        finally { await staged.DisposeAsync(); File.Delete(path); }
    }
    private FileStream Temporary(bool deleteOnClose = true)
    {
        EnsureSafeRoot();
        return new FileStream(Path.Combine(_settings.TempRoot, "be-ai-" + Guid.NewGuid().ToString("N") + ".tmp"), FileMode.CreateNew, FileAccess.ReadWrite,
            FileShare.Read, 64 * 1024, FileOptions.Asynchronous | (deleteOnClose ? FileOptions.DeleteOnClose : FileOptions.None));
    }
    private void EnsureSafeRoot()
    {
        for (var directory = new DirectoryInfo(_settings.TempRoot); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("UNSAFE_TEMP_STORAGE");
        Directory.CreateDirectory(_settings.TempRoot);
    }
    private void CheckCapacity(long reservation)
    {
        EnsureSafeRoot();
        long used = 0;
        foreach (var entry in new DirectoryInfo(_settings.TempRoot).EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("UNSAFE_TEMP_STORAGE");
            if (entry is FileInfo file) used = checked(used + file.Length);
        }
        var volume = new DriveInfo(AiVideoSpoolVolume.ResolvePath(_settings.TempRoot, DriveInfo.GetDrives().Select(drive => drive.Name)));
        var free = volume.AvailableFreeSpace;
        if (reservation < 0 || reservation > _settings.DiskBudgetBytes - used || free < _settings.MinimumFreeBytes || reservation > free - _settings.MinimumFreeBytes)
            throw new IOException("DISK_BUDGET_EXCEEDED");
    }
    private static async Task CopySourceAsync(Stream source, Stream destination, long bytes, string sha256, CancellationToken ct)
    {
        if (bytes <= 0 || sha256 is null || !System.Text.RegularExpressions.Regex.IsMatch(sha256, "\\A[0-9a-f]{64}\\z")) throw new InvalidDataException("SOURCE_CHECKSUM_MISMATCH");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var buffer = new byte[64 * 1024]; long copied = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, copied == bytes ? 1 : (int)Math.Min(buffer.Length, bytes - copied)), ct);
            if (read == 0) break;
            if (read > bytes - copied) throw new InvalidDataException("SOURCE_CHECKSUM_MISMATCH");
            copied += read; hash.AppendData(buffer, 0, read); await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        if (copied != bytes || !string.Equals(Convert.ToHexString(hash.GetHashAndReset()), sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SOURCE_CHECKSUM_MISMATCH");
    }
}
