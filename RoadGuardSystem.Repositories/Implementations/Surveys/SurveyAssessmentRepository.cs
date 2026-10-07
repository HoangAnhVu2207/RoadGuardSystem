using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Surveys;

public sealed class SurveyAssessmentRepository(RoadGuardDbContext context, IdempotencyOperationService idempotency) : ISurveyAssessmentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<AssessmentRecord?> ReadAsync(Guid dataset, Guid? assessment, CancellationToken token)
    {
        var row = await context.Set<DatasetAssessment>().AsNoTracking().Where(x => x.DatasetId == dataset && (!assessment.HasValue || x.Id == assessment))
            .OrderByDescending(x => x.ReviewedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(token);
        if (row is null) return null;
        var items = await context.Set<DatasetAssessmentItem>().AsNoTracking().Where(x => x.AssessmentId == row.Id).OrderBy(x => x.Id).ToListAsync(token);
        return new(row.Id, dataset, row.MethodVersion, row.ReviewedBy, row.ReviewedAt, items.Select(ToItem).ToArray(), Convert.ToBase64String(row.RowVersion));
    }

    public async Task<AssessmentOutcome> CreateAsync(Guid actor, Guid project, Guid dataset, string version, IReadOnlyList<AssessmentItem> items,
        string key, string fingerprint, Guid? correlation, CancellationToken token)
    {
        try
        {
            var outcome = await idempotency.ExecuteAsync(actor, project, "DatasetAssessmentCreated", key, fingerprint, async ct =>
            {
                var source = await context.SurveyDataVersions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dataset, ct);
                if (source is null) throw new AssessmentReject("not_found");
                if (!TryVersion(version, out var expectedVersion) || !source.RowVersion.SequenceEqual(expectedVersion)) throw new AssessmentReject("concurrency_conflict");
                var scopes = JsonSerializer.Deserialize<AssessmentScope[]>(source.ScopeManifest ?? "[]", JsonOptions) ?? [];
                var files = JsonSerializer.Deserialize<SourceFile[]>(source.SourceManifest, JsonOptions) ?? [];
                var verified = await context.UploadSessions.AsNoTracking().Where(x => files.Select(f => f.FileId).Contains(x.FileId) && x.Status == UploadSessionStatus.Verified).Select(x => x.FileId).ToListAsync(ct);
                foreach (var item in items)
                {
                    if (!scopes.Any(s => s.RouteVersionId == item.RouteVersionId && s.SegmentSetId == item.SegmentSetId && s.TargetBand == item.TargetBand && s.SegmentIds.Contains(item.SegmentId)) ||
                        item.Evidence.Any(e => !verified.Contains(e.FileId))) throw new AssessmentReject("assessment_validation_failed");
                }
                var assessment = DatasetAssessment.Create(Guid.NewGuid(), dataset, actor, DateTimeOffset.UtcNow);
                context.Set<DatasetAssessment>().Add(assessment);
                context.Set<DatasetAssessmentItem>().AddRange(items.Select(i => DatasetAssessmentItem.Create(assessment.Id, i.RouteVersionId,
                    i.SegmentSetId, i.SegmentId, i.TargetBand, i.PositionStatus, i.QualityStatus, i.CoverageStatus, i.Reason.Trim(), JsonSerializer.Serialize(i.Evidence))));
                Audit(actor, assessment.Id, "dataset_assessment_created", correlation);
                await context.SaveChangesAsync(ct);
                var record = new AssessmentRecord(assessment.Id, dataset, assessment.MethodVersion, actor, assessment.ReviewedAt, items, Convert.ToBase64String(assessment.RowVersion));
                return (assessment.Id, JsonSerializer.Serialize(new AssessmentOutcome("success", record)));
            }, token);
            return outcome.Status == IdempotencyOperationStatus.Conflict ? new("duplicate_request") : JsonSerializer.Deserialize<AssessmentOutcome>(outcome.OutcomeJson)!;
        }
        catch (AssessmentReject e) { context.ChangeTracker.Clear(); return new(e.Code); }
        catch (DbUpdateConcurrencyException) { context.ChangeTracker.Clear(); return new("concurrency_conflict"); }
    }

    public async Task<AssessmentOutcome> SelectAsync(Guid actor, Guid project, IReadOnlyList<BaselineItem> items, string reason,
        string key, string fingerprint, Guid? correlation, CancellationToken token)
    {
        try
        {
            var outcome = await idempotency.ExecuteAsync(actor, project, "BaselineSelected", key, fingerprint, async ct =>
            {
                var selection = BaselineSelection.Create(project, actor, reason.Trim(), DateTimeOffset.UtcNow);
                context.Set<BaselineSelection>().Add(selection);
                var selected = new List<SelectedBaselineItem>();
                foreach (var item in items)
                {
                    var belongs = await (from d in context.SurveyDataVersions.AsNoTracking()
                                         join s in context.Surveys.AsNoTracking() on d.SurveyId equals s.Id
                                         join a in context.Set<DatasetAssessment>().AsNoTracking() on d.Id equals a.DatasetId
                                         where d.Id == item.DatasetId && s.ProjectId == project && a.Id == item.AssessmentId && a.MethodVersion == "pm-evidence-review.v1"
                                         select a.Id).AnyAsync(ct);
                    if (!belongs) throw new AssessmentReject("not_found");
                    var eligible = await context.Set<DatasetAssessmentItem>().AsNoTracking().AnyAsync(a => a.AssessmentId == item.AssessmentId &&
                        a.RouteVersionId == item.RouteVersionId && a.SegmentSetId == item.SegmentSetId && a.SegmentId == item.SegmentId && a.TargetBand == item.TargetBand &&
                        a.PositionStatus == "PASS" && a.QualityStatus == "PASS" && a.CoverageStatus == "PASS", ct);
                    if (!eligible) throw new AssessmentReject("baseline_not_eligible");
                    var pointer = await context.Set<BaselineCurrentPointer>().SingleOrDefaultAsync(p => p.ProjectId == project && p.RouteVersionId == item.RouteVersionId &&
                        p.SegmentSetId == item.SegmentSetId && p.SegmentId == item.SegmentId && p.TargetBand == item.TargetBand, ct);
                    if (pointer?.SelectionId != item.ExpectedBaselineSelectionId) throw new AssessmentReject("baseline_selection_conflict");
                    var entry = BaselineSelectionItem.Create(selection.Id, item.DatasetId, item.AssessmentId, item.RouteVersionId, item.SegmentSetId, item.SegmentId, item.TargetBand);
                    context.Set<BaselineSelectionItem>().Add(entry);
                    if (pointer is null) context.Set<BaselineCurrentPointer>().Add(BaselineCurrentPointer.Create(project, entry)); else pointer.Select(entry.Id);
                    selected.Add(ToSelected(entry));
                }
                Audit(actor, selection.Id, "baseline_selected", correlation);
                await context.SaveChangesAsync(ct);
                var record = new BaselineRecord(selection.Id, selected, actor, selection.SelectedAt, Convert.ToBase64String(selection.RowVersion));
                return (selection.Id, JsonSerializer.Serialize(new AssessmentOutcome("success", Baseline: record)));
            }, token);
            return outcome.Status == IdempotencyOperationStatus.Conflict ? new("duplicate_request") : JsonSerializer.Deserialize<AssessmentOutcome>(outcome.OutcomeJson)!;
        }
        catch (AssessmentReject e) { context.ChangeTracker.Clear(); return new(e.Code); }
        catch (DbUpdateConcurrencyException) { context.ChangeTracker.Clear(); return new("baseline_selection_conflict"); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 }) { context.ChangeTracker.Clear(); return new("baseline_selection_conflict"); }
    }

    public async Task<BaselineRecord?> HistoryAsync(Guid project, Guid id, CancellationToken token)
    {
        var batch = await context.Set<BaselineSelection>().AsNoTracking().SingleOrDefaultAsync(x => x.ProjectId == project && x.Id == id, token);
        if (batch is null) return null;
        var entries = await context.Set<BaselineSelectionItem>().AsNoTracking().Where(x => x.BaselineSelectionId == batch.Id).OrderBy(x => x.Id).ToListAsync(token);
        return new(batch.Id, entries.Select(ToSelected).ToArray(), batch.SelectedBy, batch.SelectedAt, Convert.ToBase64String(batch.RowVersion));
    }
    public async Task<IReadOnlyList<SelectedBaselineItem>> CurrentAsync(Guid project, Guid? segmentSetId, CancellationToken token)
    {
        var rows = await (from p in context.Set<BaselineCurrentPointer>().AsNoTracking()
                          join i in context.Set<BaselineSelectionItem>().AsNoTracking() on p.SelectionId equals i.Id
                          where p.ProjectId == project && (!segmentSetId.HasValue || p.SegmentSetId == segmentSetId)
                          orderby p.SegmentId, p.TargetBand
                          select i).ToListAsync(token);
        return rows.Select(ToSelected).ToArray();
    }
    private void Audit(Guid actor, Guid id, string action, Guid? correlation) => context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, DateTimeOffset.UtcNow,
        action, "SurveyAssessment", id, null, "{}", "Manual survey evidence review", "anh01", correlation, []));
    private static AssessmentItem ToItem(DatasetAssessmentItem i) => new(i.RouteVersionId, i.SegmentSetId, i.SegmentId, i.TargetBand,
        i.PositionStatus, i.QualityStatus, i.CoverageStatus, i.Reason, JsonSerializer.Deserialize<AssessmentEvidence[]>(i.EvidenceJson) ?? []);
    private static SelectedBaselineItem ToSelected(BaselineSelectionItem i) => new(i.Id, i.RouteVersionId, i.SegmentSetId, i.SegmentId, i.TargetBand, i.DatasetId, i.AssessmentId);
    private static bool TryVersion(string version, out byte[] bytes)
    {
        try { bytes = Convert.FromBase64String(version); return bytes.Length == 8; }
        catch (FormatException) { bytes = []; return false; }
    }
    private sealed record AssessmentScope(Guid RouteVersionId, Guid SegmentSetId, Guid[] SegmentIds, string TargetBand);
    private sealed record SourceFile(Guid FileId);
    private sealed class AssessmentReject(string code) : Exception { public string Code { get; } = code; }
}
