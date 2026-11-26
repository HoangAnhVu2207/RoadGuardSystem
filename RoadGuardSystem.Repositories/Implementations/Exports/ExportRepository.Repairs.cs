using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Exports;

public sealed partial class ExportRepository
{
    public async Task<RepairExportSelection[]> ResolveRepairItemsAsync(Guid project, Guid[] packages, Guid[] items, CancellationToken token)
    {
        var packageIds = packages.Select(id => (Guid?)id).ToArray();
        var rows = await _db.RepairItems.AsNoTracking().Where(row =>
            items.Contains(row.Id) || packageIds.Contains(EF.Property<Guid?>(row, "PackageId")))
            .Select(row => new { row.Id, row.ObligationId, row.ProjectId, Package = EF.Property<Guid?>(row, "PackageId") })
            .OrderBy(row => row.Id).Take(101).ToArrayAsync(token);
        if (rows.Length > 100 || items.Any(id => !rows.Any(row => row.Id == id)) ||
            packages.Any(id => !rows.Any(row => row.Package == id))) return [];
        foreach (var row in rows)
            if (row.Package is null || await ObligationResponsibilityScope.ResolveAsync(_db, row.ObligationId, row.ProjectId, token) != project) return [];
        return rows.Select(row => new RepairExportSelection(row.Package!.Value, row.Id)).ToArray();
    }
    public async Task<RepairExportSupplement?> ReadRepairSupplementAsync(Guid project, Guid item, CancellationToken token)
    {
        var obligation = await (from row in _db.RepairItems.AsNoTracking()
                                join source in _db.RepairObligations.AsNoTracking().Include(source => source.Scope) on row.ObligationId equals source.Id
                                where row.Id == item && row.ProjectId == source.ProjectId && row.DefectId == source.DefectId
                                select source).SingleOrDefaultAsync(token);
        if (obligation is null || await ObligationResponsibilityScope.ResolveAsync(_db, obligation.Id, obligation.ProjectId, token) != project) return null;
        var links = await _db.Set<RepairAttemptSubmissionLink>().AsNoTracking().Where(row => row.ItemId == item && row.ProjectId == obligation.ProjectId)
            .Select(row => row.SubmissionId).ToArrayAsync(token);
        var submissions = await _db.Set<FieldInspectionSubmission>().AsNoTracking().Where(row => links.Contains(row.Id) && row.ProjectId == obligation.ProjectId)
            .OrderBy(row => row.ServerReceivedAt).ThenBy(row => row.Id).ToArrayAsync(token);
        var reviews = await _db.Set<RepairAttemptReview>().AsNoTracking().Where(row => row.ItemId == item && row.ProjectId == obligation.ProjectId)
            .OrderBy(row => row.At).ThenBy(row => row.Id).Select(row => new
            { row.Id, row.AttemptId, row.SubmissionId, row.Decision, row.Reason, row.ActorId, row.At, row.ChecklistVersion, row.PlanHash }).ToArrayAsync(token);
        var safe = submissions.Select(row =>
        {
            using var payload = JsonDocument.Parse(row.PayloadJson);
            var measurements = payload.RootElement.EnumerateObject().SingleOrDefault(field => field.Name.Equals("measurements", StringComparison.OrdinalIgnoreCase));
            return JsonSerializer.SerializeToElement(new
            {
                row.Id,
                row.RootId,
                row.ParentId,
                row.Revision,
                row.ServerReceivedAt,
                row.Readiness,
                measurements = measurements.Value.ValueKind == JsonValueKind.Undefined ? (JsonElement?)null : measurements.Value.Clone()
            }, ExportSerialization.Options);
        }).ToArray();
        return new(JsonSerializer.SerializeToElement(obligation.Scope, ExportSerialization.Options), safe,
            reviews.Select(row => JsonSerializer.SerializeToElement(row, ExportSerialization.Options)).ToArray());
    }
}
