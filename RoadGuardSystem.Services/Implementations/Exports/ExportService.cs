using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reporting;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Exports;

public sealed class ExportService : IExportService
{
    private readonly IExportRepository _repo;
    private readonly IIdentityRepository _identity;
    private readonly IProjectScopeGuard _scope;
    private readonly IReportingService _reporting;
    private readonly IApprovedTrainingLabelReader? _labels;
    private readonly ITrainingSourceAccessReader? _sourceAccess;
    private readonly IAnh02ArtifactStore _artifacts;
    private readonly IUploadObjectStorage _sourceStorage;
    private readonly IExportRenderer _renderer;
    private readonly TimeProvider _clock;
    private readonly IServiceScopeFactory _workerScopes;
    public ExportService(IExportRepository repo, IIdentityRepository identity, IProjectScopeGuard scope, IReportingService reporting,
        IEnumerable<IApprovedTrainingLabelReader> labels, IEnumerable<ITrainingSourceAccessReader> sourceAccess,
        IAnh02ArtifactStore artifacts, IUploadObjectStorage sourceStorage, IExportRenderer renderer, TimeProvider clock, IServiceScopeFactory workerScopes)
    { _repo = repo; _identity = identity; _scope = scope; _reporting = reporting; _labels = labels.SingleOrDefault(); _sourceAccess = sourceAccess.SingleOrDefault(); _artifacts = artifacts; _sourceStorage = sourceStorage; _renderer = renderer; _clock = clock; _workerScopes = workerScopes; }
    private async Task<UserRoleCode?> AuthorizeAsync(Guid actor, Guid project, CancellationToken ct)
    {
        var user = await _identity.GetUserSecurityStateAsync(actor, ct);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword || user.RoleCode is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor) || !await _identity.IsRoleActiveAsync(user.RoleCode, ct)) return null;
        return await _scope.AuthorizeAsync(actor, user.RoleCode, project, ct) is null ? null : user.RoleCode;
    }
    public async Task<ExportResult<ExportJobViewDto>> CreateAsync(Guid actorId, Guid projectId, CreateExportRequestDto request, string key, Guid? correlationId, CancellationToken ct)
    {
        if (await AuthorizeAsync(actorId, projectId, ct) is null) return new("forbidden");
        var normalized = Normalize(request);
        if (normalized is null || string.IsNullOrWhiteSpace(key) || key.Trim().Length > 200) return new("export_invalid");
        // Dependencies are checked inside capture, so an already durable receipt can be replayed despite producer downtime.
        var result = await _repo.AdmitAsync(actorId, projectId, normalized, key.Trim(), ExportSerialization.Hash(JsonSerializer.Serialize(normalized, ExportSerialization.Options)), correlationId,
            async (snapshotId, now, token) =>
            {
                var role = await AuthorizeAsync(actorId, projectId, token);
                if (role is null) return new("forbidden", null);
                return await CaptureAsync(actorId, role.Value, projectId, normalized, snapshotId, now, token);
            }, ct);
        return result.Export is null ? new(result.ErrorCode ?? "not_found") : new("success", View(result.Export));
    }
    public static CreateExportRequestDto? Normalize(CreateExportRequestDto? request)
    {
        if (request is null) return null;
        var kind = request.Kind?.Trim().ToUpperInvariant(); var format = request.Format?.Trim().ToUpperInvariant();
        if (kind is not ("DOSSIER" or "TRAINING") || format is not ("PDF" or "ZIP") || kind == "TRAINING" && format != "ZIP" || format == "PDF" && request.IncludeOriginalFiles || request.From >= request.To || request.SegmentIds?.Contains(Guid.Empty) == true || request.DefectIds?.Contains(Guid.Empty) == true || (request.SegmentIds?.Length ?? 0) > 1000 || (request.DefectIds?.Length ?? 0) > 1000) return null;
        // No authoritative defect mapping exists in the Anh reporting reader; reject instead of silently dropping the filter.
        if (request.DefectIds is { Length: > 0 }) return null;
        return request with { Kind = kind, Format = format, SegmentIds = (request.SegmentIds ?? []).Distinct().Order().ToArray(), DefectIds = (request.DefectIds ?? []).Distinct().Order().ToArray(), From = request.From?.ToUniversalTime(), To = request.To?.ToUniversalTime() };
    }
    private async Task<ExportCaptureResult> CaptureAsync(Guid actor, UserRoleCode role, Guid project, CreateExportRequestDto request, Guid snapshotId, DateTimeOffset now, CancellationToken ct)
    {
        ReportingCaptureDto? dossier = null; ExportLabelDto[]? labels = null; ExportFileDto[] files; ExportSectionDto[] sections; ExportSourceRevisionDto[] revisions; string[] definitions;
        if (request.Kind == "DOSSIER")
        {
            var result = await _reporting.CaptureAsync(actor, project, new(request.From, request.To, SegmentIds: request.SegmentIds), ct);
            if (result.Value is null) return new(result.Code, null);
            dossier = result.Value;
            files = dossier.Files.OrderBy(x => x.FileId).Select(f => new ExportFileDto(f.FileId, f.Version, f.Sha256, f.SizeBytes, f.MediaType, request.IncludeOriginalFiles ? ExportArchive.SurveyFilePath(f.FileId, f.MediaType) : null, request.IncludeOriginalFiles, request.IncludeOriginalFiles ? null : "ORIGINALS_NOT_REQUESTED")).ToArray();
            if (!await _repo.CanReadSurveySourcesAsync(project, files.Select(f => f.FileId).ToArray(), ct)) return new("export_source_unavailable", null);
            sections = dossier.Summary.Metrics.Select(m => new ExportSectionDto(m.Code, m.Availability, m.ReasonCodes)).Concat(new[] {
                new ExportSectionDto("reporterEvidence", "UNAVAILABLE", ["REPORTER_EVIDENCE_ACCESS_NOT_AVAILABLE"])
            }).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            revisions = dossier.Items.Select(x => new ExportSourceRevisionDto(x.Type, x.Id, x.Version))
                .Concat(dossier.Summary.Metrics.SelectMany(m => m.SourceRefs).Select(r => new ExportSourceRevisionDto(r.Type, r.Id, r.Version)))
                .Distinct().OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Id).ToArray();
            definitions = [dossier.Summary.DefinitionVersion];
        }
        else
        {
            if (_labels is null || request.IncludeOriginalFiles && _sourceAccess is null) return new("producer_unavailable", null);
            var selection = await _labels.CaptureApprovedAsync(actor, role, project, new(request.SegmentIds!, request.DefectIds!, request.From, request.To), ct);
            if (selection is null) return new("producer_unavailable", null);
            if (selection.Labels is null || selection.SnapshotId == Guid.Empty || string.IsNullOrWhiteSpace(selection.SchemaVersion) || selection.Hash is not { Length: 64 } || !selection.Hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')) return new("producer_invalid", null);
            if (selection.Labels.Count == 0) return new("no_eligible_labels", null);
            if (selection.Labels.Any(l => !ValidLabel(l, project)) || selection.Labels.Select(l => l.LabelId).Distinct().Count() != selection.Labels.Count) return new("producer_invalid", null);
            if (request.SegmentIds is { Length: > 0 } selectedSegments && selection.Labels.Any(l => l.SegmentId is null || !selectedSegments.Contains(l.SegmentId.Value))) return new("producer_invalid", null);
            labels = selection.Labels.OrderBy(l => l.LabelId).ThenBy(l => l.Revision).Select(l => new ExportLabelDto(l.LabelId, l.Revision, l.RevisionId, l.ProjectId, l.TypeCode, l.Annotation.X, l.Annotation.Y, l.Annotation.Width, l.Annotation.Height, l.FileId, l.FileVersion, l.Sha256, l.SizeBytes, l.MediaType, l.SourceKind, l.SourceId, l.SourceVersion, l.ApprovalId, l.ApprovedBy, l.ApprovedAt.ToUniversalTime(), l.ProcessingJobId, l.ModelVersionId, l.DatasetVersionId, l.Mode, l.SegmentId)).ToArray();
            files = labels.GroupBy(l => l.FileId).OrderBy(g => g.Key).Select(g => { var l = g.First(); return new ExportFileDto(l.FileId, l.FileVersion, l.Sha256, l.SizeBytes, l.MediaType, request.IncludeOriginalFiles ? ExportArchive.FilePath(l.FileId, l.MediaType) : null, request.IncludeOriginalFiles, request.IncludeOriginalFiles ? null : "ORIGINALS_NOT_REQUESTED"); }).ToArray();
            foreach (var f in files)
            {
                var original = await _repo.GetSourceFileAsync(f.FileId, ct);
                if (original is null || original.Checksum != f.Sha256 || original.SizeBytes != f.SizeBytes || original.MimeType != f.MediaType) return new("export_source_unavailable", null);
                if (labels.Where(l => l.FileId == f.FileId).Any(l => l.FileVersion != f.FileVersion || l.Sha256 != f.Sha256 || l.SizeBytes != f.SizeBytes || l.MediaType != f.MediaType)) return new("producer_invalid", null);
            }
            if (request.IncludeOriginalFiles && !await _sourceAccess!.CanReadAsync(actor, role, project, files.Select(f => f.FileId).ToArray(), ct)) return new("forbidden", null);
            sections = [new("approvedLabels", "AVAILABLE", [])];
            revisions = labels.Select(l => new ExportSourceRevisionDto("labelRevision", l.RevisionId, l.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture))).Append(new("approvedLabelSnapshot", selection.SnapshotId, selection.Hash)).ToArray();
            definitions = [selection.SchemaVersion];
        }
        var manifest = new ExportManifestV1Dto("anh02.export.v1", snapshotId, project, request.Kind, request.Format, actor, now, now, definitions, request, revisions, files, sections, labels, "");
        return new(null, new(manifest, dossier));
    }
    private static bool ValidLabel(ApprovedTrainingLabelV1 l, Guid project) => l is not null && l.Annotation is not null && l.ProjectId == project && l.LabelId != Guid.Empty && l.RevisionId != Guid.Empty && l.ApprovalId != Guid.Empty && l.ApprovedBy != Guid.Empty && l.FileId != Guid.Empty && l.SourceId != Guid.Empty && l.Revision > 0 && !string.IsNullOrWhiteSpace(l.TypeCode) && !string.IsNullOrWhiteSpace(l.SourceKind) && !string.IsNullOrWhiteSpace(l.FileVersion) && !string.IsNullOrWhiteSpace(l.SourceVersion) && l.SizeBytes > 0 && l.Sha256 is { Length: 64 } && l.Sha256.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') && l.MediaType is "image/jpeg" or "image/png" && l.Annotation.X >= 0 && l.Annotation.Y >= 0 && l.Annotation.Width > 0 && l.Annotation.Height > 0 && l.Annotation.X + l.Annotation.Width <= 1 && l.Annotation.Y + l.Annotation.Height <= 1 && l.Mode is "REAL" or "MOCK" or "SYNTHETIC";
    public async Task<ExportResult<ExportJobViewDto>> GetAsync(Guid actorId, Guid projectId, Guid exportId, CancellationToken ct)
    {
        if (await AuthorizeAsync(actorId, projectId, ct) is null) return new("forbidden");
        var data = await _repo.GetAsync(projectId, exportId, ct); return data is null ? new("not_found") : new("success", View(data));
    }
    public async Task<ExportResult<ExportManifestV1Dto>> ManifestAsync(Guid actorId, Guid projectId, Guid exportId, CancellationToken ct)
    {
        if (await AuthorizeAsync(actorId, projectId, ct) is null) return new("forbidden");
        var data = await _repo.GetAsync(projectId, exportId, ct); return data is null ? new("not_found") : new("success", ExportSerialization.Read(data.Snapshot).Manifest with { SnapshotHash = data.Snapshot.Hash });
    }
    public async Task<ExportResult<ExportContentDto>> ContentAsync(Guid actorId, Guid projectId, Guid exportId, CancellationToken ct)
    {
        var role = await AuthorizeAsync(actorId, projectId, ct); if (role is null) return new("forbidden");
        var data = await _repo.GetAsync(projectId, exportId, ct); if (data is null) return new("not_found");
        if (data.Job.Status != "SUCCEEDED" || data.ArtifactFile is null) return new("export_not_ready");
        if (_clock.GetUtcNow() >= data.Job.ExpiresAt) return new("export_expired");
        var payload = ExportSerialization.Read(data.Snapshot);
        if (!await CanReadFilesAsync(actorId, role.Value, projectId, payload.Manifest, ct)) return new("forbidden");
        try
        {
            var read = await _artifacts.OpenReadAsync(data.ArtifactFile.StorageUri, ct);
            if (read.Metadata.Sha256 != data.ArtifactFile.Checksum || read.Metadata.SizeBytes != data.ArtifactFile.SizeBytes || read.Metadata.MediaType != data.ArtifactFile.MimeType) { await read.DisposeAsync(); return new("export_storage_unavailable"); }
            // A large disk spool may finish after revocation or expiry. Check again before returning any bytes to HTTP.
            var currentRole = await AuthorizeAsync(actorId, projectId, ct);
            if (currentRole is null || !await CanReadFilesAsync(actorId, currentRole.Value, projectId, payload.Manifest, ct)) { await read.DisposeAsync(); return new("forbidden"); }
            if (_clock.GetUtcNow() >= data.Job.ExpiresAt) { await read.DisposeAsync(); return new("export_expired"); }
            return new("success", new(read.Content, data.ArtifactFile.MimeType, data.ArtifactFile.OriginalName, read.Metadata.Sha256));
        }
        catch (Exception ex) when (ex is IOException or FileStorageException or HttpRequestException) { return new("export_storage_unavailable"); }
    }
    private Task<bool> CanReadFilesAsync(Guid actor, UserRoleCode role, Guid project, ExportManifestV1Dto manifest, CancellationToken ct)
    {
        var ids = manifest.Files.Where(f => f.Included).Select(f => f.FileId).ToArray();
        if (ids.Length == 0) return Task.FromResult(true);
        if (manifest.Kind == "DOSSIER") return _repo.CanReadSurveySourcesAsync(project, ids, ct);
        return _sourceAccess?.CanReadAsync(actor, role, project, ids, ct) ?? Task.FromResult(false);
    }
    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var claim = await _repo.ClaimAsync(ct); if (claim is null) return false;
        using var leaseCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeat = RenewWhileProcessingAsync(claim, leaseCancellation);
        try { return await ProcessClaimAsync(claim, leaseCancellation.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return true; }
        finally
        {
            await leaseCancellation.CancelAsync();
            try { await heartbeat; } catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested) { }
        }
    }
    private async Task RenewWhileProcessingAsync(ExportClaim claim, CancellationTokenSource leaseCancellation)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20), _clock);
        try
        {
            while (await timer.WaitForNextTickAsync(leaseCancellation.Token))
            {
                // A separate scoped context is required: storage and renderer may still use the worker's context.
                using var heartbeatScope = _workerScopes.CreateScope();
                if (!await heartbeatScope.ServiceProvider.GetRequiredService<IExportRepository>().RenewAsync(claim.Id, claim.Token, leaseCancellation.Token))
                { await leaseCancellation.CancelAsync(); break; }
            }
        }
        catch (OperationCanceledException) when (leaseCancellation.IsCancellationRequested) { }
        catch { await leaseCancellation.CancelAsync(); throw; }
    }
    private async Task<bool> ProcessClaimAsync(ExportClaim claim, CancellationToken ct)
    {
        try
        {
            var role = await AuthorizeAsync(claim.RequestedBy, claim.ProjectId, ct);
            if (role is null) { await _repo.FailAsync(claim, "export_authority_revoked", true, ct); return true; }
            var payload = ExportSerialization.Read(claim.Snapshot);
            var manifest = payload.Manifest with { SnapshotHash = claim.Snapshot.Hash };
            if (!await CanReadFilesAsync(claim.RequestedBy, role.Value, claim.ProjectId, manifest, ct)) { await _repo.FailAsync(claim, "export_source_access_revoked", true, ct); return true; }
            var key = $"anh02/exports/{claim.Id:D}/{claim.Snapshot.Hash}.{manifest.Format.ToLowerInvariant()}";
            Anh02ArtifactMetadata metadata;
            try
            {
                await using var prior = await _artifacts.OpenReadAsync(key, ct);
                if (!await ExportArchive.ProvesSnapshotAsync(prior.Content, manifest, ct)) throw new ExportRenderException("export_storage_corrupt");
                metadata = prior.Metadata;
            }
            catch (FileNotFoundException)
            {
                await using var rendered = await _renderer.RenderAsync(payload with { Manifest = manifest }, async (f, token) =>
                {
                    var currentRole = await AuthorizeAsync(claim.RequestedBy, claim.ProjectId, token);
                    if (currentRole is null || !await CanReadFilesAsync(claim.RequestedBy, currentRole.Value, claim.ProjectId, manifest, token)) throw new ExportRenderException("export_source_access_revoked");
                    var source = await _repo.GetSourceFileAsync(f.FileId, token);
                    if (source is null || source.SizeBytes != f.SizeBytes || source.Checksum != f.Sha256 || source.MimeType != f.MediaType) throw new ExportRenderException("export_source_unavailable");
                    try { return await _sourceStorage.OpenReadAsync(source.StorageUri, token); }
                    catch (FileStorageException ex) when (ex.InnerException is Amazon.S3.AmazonS3Exception s3 && s3.StatusCode == System.Net.HttpStatusCode.NotFound) { throw new ExportRenderException("export_source_unavailable"); }
                }, async token => { if (!await _repo.RenewAsync(claim.Id, claim.Token, token)) throw new ExportRenderException("export_lease_lost"); }, ct);
                if (!await _repo.RenewAsync(claim.Id, claim.Token, ct)) return true;
                metadata = await _artifacts.WriteAsync(key, rendered, rendered.Length, manifest.Format == "PDF" ? "application/pdf" : "application/zip", ct);
            }
            if (!await _repo.RenewAsync(claim.Id, claim.Token, ct)) return true;
            await _repo.CompleteAsync(claim, metadata, async token =>
            {
                var current = await AuthorizeAsync(claim.RequestedBy, claim.ProjectId, token);
                return current.HasValue && await CanReadFilesAsync(claim.RequestedBy, current.Value, claim.ProjectId, manifest, token);
            }, ct);
        }
        catch (ExportRenderException ex) { await _repo.FailAsync(claim, ex.Code, ex.Code != "export_lease_lost", ct); }
        catch (InvalidDataException) { await _repo.FailAsync(claim, "export_snapshot_corrupt", true, ct); }
        catch (Exception ex) when (ex is FileStorageException or IOException or HttpRequestException) { await _repo.FailAsync(claim, "export_storage_unavailable", false, ct); }
        return true;
    }
    private static ExportJobViewDto View(ExportPersistenceView data)
    {
        var manifest = ExportSerialization.Read(data.Snapshot).Manifest;
        var missing = manifest.Sections.Where(s => s.Availability != "AVAILABLE").Select(s => s.Name).ToArray();
        return new(data.Job.Id, data.Job.ProjectId, data.Job.Kind, data.Job.Format, data.Job.Status, data.Snapshot.Id, data.Snapshot.Hash, data.Job.ArtifactId, data.Job.CreatedAt, data.Job.CompletedAt, data.Job.ExpiresAt, data.Job.ErrorCode, missing.Length == 0 ? "COMPLETE" : "PARTIAL", missing, Convert.ToBase64String(data.Job.RowVersion));
    }
}
