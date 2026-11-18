using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Exports;

namespace RoadGuardSystem.Services.Exports;

public sealed partial class ExportService
{
    private async Task<ExportCaptureResult> CaptureRepairAsync(Guid actor, UserRoleCode role, Guid project,
        CreateExportRequestDto request, Guid snapshotId, DateTimeOffset now, CancellationToken token)
    {
        if (_repairs is null || _files is null) return new("producer_unavailable", null);
        var selection = await _repo.ResolveRepairItemsAsync(project, request.RepairPackageIds ?? [], request.RepairItemIds ?? [], token);
        if (selection.Length == 0) return new("export_source_unavailable", null);
        var items = new List<RepairDossierItemDto>();
        var files = new Dictionary<Guid, ExportFileDto>();
        var sections = new List<ExportSectionDto>();
        var sources = new List<ExportSourceRevisionDto>();
        foreach (var selected in selection)
        {
            var result = await _repairs.ReadItemAsync(actor, role, project, selected.PackageId, selected.ItemId, true, token);
            if (result.Status != 200 || result.Value is not RepairHistoryView history) return new("forbidden", null);
            var supplement = await _repo.ReadRepairSupplementAsync(project, selected.ItemId, token);
            if (supplement is null) return new("export_source_unavailable", null);
            items.Add(new(selected.PackageId, history, supplement.Scope, supplement.Submissions, supplement.Reviews));
            sources.Add(new("RepairItem", history.Item.Id, history.Item.Version));
            sources.AddRange(history.Decisions.Select(row => new ExportSourceRevisionDto("RepairDecision", row.Id, "snapshot-sha256-" + ExportSerialization.Hash(System.Text.Json.JsonSerializer.Serialize(row, ExportSerialization.Options)))));
            sections.Add(new($"item/{history.Item.Id}/plan", history.Item.RepairPlan is null ? "UNAVAILABLE" : "AVAILABLE",
                history.Item.RepairPlan is null ? ["LEGACY_PLAN_NOT_STORED"] : []));
            sections.Add(new($"item/{history.Item.Id}/finalConfirmation", history.Item.EffectiveDecisionId is null ? "UNAVAILABLE" : "AVAILABLE",
                history.Item.EffectiveDecisionId is null ? ["FINAL_CONFIRMATION_NOT_RECORDED"] : []));
            sections.Add(new($"item/{history.Item.Id}/currentAcceptance", history.Item.Result == "CONFIRMED" ? "AVAILABLE" : "UNAVAILABLE",
                history.Item.Result == "CONFIRMED" ? [] : ["NO_EFFECTIVE_ACCEPTANCE"]));
            sections.Add(new($"item/{history.Item.Id}/measurements", supplement.Submissions.Length == 0 ? "UNAVAILABLE" : "AVAILABLE",
                supplement.Submissions.Length == 0 ? ["FIELD_SUBMISSION_NOT_RECORDED"] : []));
            foreach (var evidence in history.Attempts.SelectMany(row => row.Evidence).Concat(history.Decisions.SelectMany(row => row.Basis?.Evidence ?? [])))
            {
                if (!evidence.Verified || evidence.FileVersion is null || evidence.Sha256 is null) continue;
                var read = await _files.GetFileMetadataAsync(actor, role, evidence.FileId, cancellationToken: token);
                if (read.File is not { Status: "VERIFIED" } file || file.Version != evidence.FileVersion || file.ChecksumSha256 != evidence.Sha256)
                {
                    sections.Add(new($"file/{evidence.FileId}", "UNAVAILABLE", ["EVIDENCE_ACCESS_OR_VERSION_UNAVAILABLE"]));
                    continue;
                }
                if (file.MediaType is not ("image/jpeg" or "image/png"))
                { sections.Add(new($"file/{file.Id}", "UNAVAILABLE", ["REPAIR_ORIGINAL_FORMAT_UNSUPPORTED"])); continue; }
                var path = request.IncludeOriginalFiles ? $"repair-evidence/{file.Id:D}.{(file.MediaType == "image/png" ? "png" : "jpg")}" : null;
                files[file.Id] = new(file.Id, file.Version, file.ChecksumSha256, file.SizeBytes, file.MediaType, path,
                    request.IncludeOriginalFiles, request.IncludeOriginalFiles ? null : "ORIGINALS_NOT_REQUESTED");
            }
        }
        var dossier = new RepairDossierDto("repair.dossier.v1", items.ToArray());
        sources.Add(new("DossierContent", snapshotId, ExportSerialization.Hash(System.Text.Json.JsonSerializer.Serialize(dossier, ExportSerialization.Options))));
        var manifest = new ExportManifestV1Dto("anh02.export.v1", snapshotId, project, "DOSSIER", request.Format,
            actor, now, now, ["repair.dossier.v1"], request, sources.Distinct().ToArray(), files.Values.OrderBy(row => row.FileId).ToArray(),
            sections.ToArray(), null, "");
        return new(null, new ExportSnapshotPayloadDto(manifest, null, dossier));
    }
}
