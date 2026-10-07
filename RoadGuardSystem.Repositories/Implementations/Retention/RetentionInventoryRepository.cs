using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Retention;

public sealed class RetentionInventoryRepository(RoadGuardDbContext context, IEnumerable<IRetentionInventoryContributor> contributors) : IRetentionInventoryRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<RetentionInventory?> ReadAsync(Guid fileId, CancellationToken token)
    {
        var file = await context.Files.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId, token);
        if (file is null) return null;
        var refs = new List<RetentionReferenceViewFact>();
        var reasons = new List<string>();
        var additionalBoundaries = new List<DateTimeOffset?>();
        var scopes = await context.FileScopes.AsNoTracking().Where(x => x.FileId == fileId).ToListAsync(token);
        refs.AddRange(scopes.Select(x => new RetentionReferenceViewFact("FILE_SCOPE", x.Id, x.ProjectId, Hash(new { x.ProjectId, x.TargetId, x.Purpose, x.CreatedAt }))));
        var sessions = await context.UploadSessions.AsNoTracking().Where(x => x.FileId == fileId).ToListAsync(token);
        refs.AddRange(sessions.Select(x => new RetentionReferenceViewFact("UPLOAD_SESSION", x.Id, scopes.FirstOrDefault()?.ProjectId, Version(x.RowVersion))));
        var links = await (from sf in context.SurveyFiles.AsNoTracking() join s in context.Surveys.AsNoTracking() on sf.SurveyId equals s.Id where sf.FileId == fileId select new { sf, s.ProjectId }).ToListAsync(token);
        refs.AddRange(links.Select(x => new RetentionReferenceViewFact("SURVEY_FILE", x.sf.Id, x.ProjectId, Hash(new { x.sf.SurveyId, x.sf.Checksum, x.sf.SyncStatus }))));
        var datasets = await (from d in context.SurveyDataVersions.AsNoTracking() join s in context.Surveys.AsNoTracking() on d.SurveyId equals s.Id select new { d, s.ProjectId }).ToListAsync(token);
        foreach (var row in datasets)
        {
            // Canonical source manifests contain concrete fileId fields. Ambiguous legacy manifests fail closed.
            try
            {
                using var doc = JsonDocument.Parse(row.d.SourceManifest);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) { refs.Add(new("UNRESOLVED_DATASET", row.d.Id, null, Version(row.d.RowVersion))); reasons.Add("LEGACY_DATASET_MANIFEST_UNRESOLVED"); continue; }
                var seen = new HashSet<Guid>();
                if (doc.RootElement.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.Object || !x.TryGetProperty("fileId", out var id) || id.ValueKind != JsonValueKind.String || !id.TryGetGuid(out var value) || value == Guid.Empty || !seen.Add(value)))
                { refs.Add(new("UNRESOLVED_DATASET", row.d.Id, null, Version(row.d.RowVersion))); reasons.Add("LEGACY_DATASET_MANIFEST_UNRESOLVED"); continue; }
                if (!ContainsFile(doc.RootElement, fileId)) continue;
                refs.Add(new("DATASET", row.d.Id, row.ProjectId, Version(row.d.RowVersion)));
                var assessments = await context.Set<DatasetAssessment>().AsNoTracking().Where(x => x.DatasetId == row.d.Id).ToListAsync(token);
                refs.AddRange(assessments.Select(x => new RetentionReferenceViewFact("ASSESSMENT", x.Id, row.ProjectId, Version(x.RowVersion))));
                var baseline = await context.Set<BaselineSelectionItem>().AsNoTracking().Where(x => x.DatasetId == row.d.Id).ToListAsync(token);
                refs.AddRange(baseline.Select(x => new RetentionReferenceViewFact("BASELINE_EVIDENCE", x.Id, row.ProjectId, Hash(new { x.DatasetId, x.AssessmentId, x.BaselineSelectionId, x.RouteVersionId, x.SegmentSetId, x.SegmentId, x.TargetBand }))));
                var baselineIds = baseline.Select(x => x.Id).ToArray();
                var pointers = await context.Set<BaselineCurrentPointer>().AsNoTracking().Where(x => baselineIds.Contains(x.SelectionId)).ToListAsync(token);
                refs.AddRange(pointers.Select(x => new RetentionReferenceViewFact("BASELINE_CURRENT", x.Id, x.ProjectId, Version(x.RowVersion))));
            }
            catch (JsonException) { refs.Add(new("UNRESOLVED_DATASET", row.d.Id, null, Version(row.d.RowVersion))); reasons.Add("LEGACY_DATASET_MANIFEST_UNRESOLVED"); }
        }
        var handovers = await context.HandoverDocuments.AsNoTracking().Where(x => x.FileId == fileId).ToListAsync(token);
        refs.AddRange(handovers.Select(x => new RetentionReferenceViewFact("HANDOVER_DOCUMENT", x.Id, x.ProjectId, Version(x.RowVersion))));
        var documents = await context.Warranties.AsNoTracking().Where(x => x.SourceDocumentId == fileId).ToListAsync(token);
        refs.AddRange(documents.Select(x => new RetentionReferenceViewFact("WARRANTY_DOCUMENT", x.Id, x.ProjectId, Hash(new { x.WarrantyEndDate, x.Scope, x.Status, x.RoadSectionId }))));
        foreach (var contributor in contributors)
        {
            var contribution = await contributor.ReadAsync(fileId, token);
            refs.AddRange(contribution.References);
            reasons.AddRange(contribution.ReasonCodes);
            additionalBoundaries.AddRange(contribution.EligibleAfter ?? []);
            if (!contribution.Complete) reasons.Add($"{contributor.Name}_INVENTORY_INCOMPLETE");
        }
        // Huy owns report/supplement/publication/candidate/defect/repair references. Their absence
        // must be visible even when an Anh file has no observed Huy reference today.
        if (!contributors.Any(x => x.Name == "HUY")) reasons.Add("HUY_REFERENCE_INVENTORY_UNAVAILABLE");
        if (!contributors.Any(x => x.Name == "AI")) reasons.Add("AI_REFERENCE_INVENTORY_UNAVAILABLE");
        if (!contributors.Any(x => x.Name == "EXPORT")) reasons.Add("EXPORT_REFERENCE_INVENTORY_UNAVAILABLE");
        if (refs.Count == 0 || refs.All(x => x.ProjectId is null)) reasons.Add("OBLIGATION_SCOPE_UNRESOLVED");
        var projects = refs.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId!.Value).Distinct().ToArray();
        // Conservative full-project obligations: never let a client omit a warranty to shorten retention.
        var exportOnly = refs.Count > 0 && refs.All(x => x.Kind == "GENERATED_EXPORT") && additionalBoundaries.Count > 0;
        var classification = exportOnly ? "TEMPORARY_EXPORT" : "EVIDENCE";
        var warrantyRows = exportOnly ? [] : await context.Warranties.AsNoTracking().Where(x => projects.Contains(x.ProjectId)).ToListAsync(token);
        var warranties = warrantyRows.Select(x => new RetentionWarrantyViewFact(x.Id, x.ProjectId, x.Scope.ToApiCode(), x.WarrantyEndDate,
            Hash(new { x.ProjectId, x.RoadSectionId, x.HandoverDocumentId, x.HandoverDate, x.WarrantyStartDate, x.WarrantyEndDate, x.RetainedValue, x.Scope, x.Terms, x.Status, x.SourceDocumentId }))).OrderBy(x => x.Id).ToArray();
        if (!exportOnly && projects.Any(p => !warranties.Any(w => w.ProjectId == p))) reasons.Add("WARRANTY_OBLIGATION_MISSING");
        if (warrantyRows.Any(x => x.Scope is WarrantyScope.Unknown or WarrantyScope.Other or WarrantyScope.ContractItem || x.WarrantyEndDate == default || x.WarrantyEndDate < x.WarrantyStartDate)) reasons.Add("WARRANTY_OBLIGATION_UNRESOLVED");
        var sorted = refs.Distinct().OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Id).ToArray();
        var codes = reasons.Distinct().Order(StringComparer.Ordinal).ToArray();
        var publicProjects = scopes.Any(x => x.ProjectId is null) ? [] : projects;
        var boundaries = additionalBoundaries.OrderBy(x => x).ToArray();
        return new(fileId, Hash(new { file.Id, file.Checksum, file.SizeBytes, References = sorted, Warranties = warranties, Reasons = codes, Classification = classification, AdditionalEligibleAfter = boundaries }), codes.Length == 0, sorted, warranties, codes, publicProjects, classification, boundaries);
    }
    public async Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token)
    {
        var files = await context.FileScopes.AsNoTracking().Where(x => x.ProjectId == projectId).Select(x => x.FileId).ToListAsync(token);
        files.AddRange(await (from sf in context.SurveyFiles.AsNoTracking() join s in context.Surveys.AsNoTracking() on sf.SurveyId equals s.Id where s.ProjectId == projectId select sf.FileId).ToListAsync(token));
        files.AddRange(await context.HandoverDocuments.AsNoTracking().Where(x => x.ProjectId == projectId && x.FileId != null).Select(x => x.FileId!.Value).ToListAsync(token));
        files.AddRange(await context.Warranties.AsNoTracking().Where(x => x.ProjectId == projectId && x.SourceDocumentId != null).Select(x => x.SourceDocumentId!.Value).ToListAsync(token));
        var manifests = await (from d in context.SurveyDataVersions.AsNoTracking() join s in context.Surveys.AsNoTracking() on d.SurveyId equals s.Id where s.ProjectId == projectId select d.SourceManifest).ToListAsync(token);
        foreach (var manifest in manifests)
        {
            try { using var doc = JsonDocument.Parse(manifest); CollectFiles(doc.RootElement, files); } catch (JsonException) { /* ReadAsync exposes the ambiguity. */ }
        }
        foreach (var contributor in contributors) files.AddRange(await contributor.KnownProjectFilesAsync(projectId, token));
        return files.Distinct().Order().ToArray();
    }
    private static bool ContainsFile(JsonElement node, Guid file) { var values = new List<Guid>(); CollectFiles(node, values); return values.Contains(file); }
    private static void CollectFiles(JsonElement node, List<Guid> values)
    {
        if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) CollectFiles(item, values);
        else if (node.ValueKind == JsonValueKind.Object) foreach (var property in node.EnumerateObject())
        {
            if (property.Name.Equals("fileId", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String && property.Value.TryGetGuid(out var id)) values.Add(id);
            else CollectFiles(property.Value, values);
        }
    }
    internal static string Hash(object value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, Json))).ToLowerInvariant();
    internal static string Version(byte[] value) => Convert.ToBase64String(value);
}
