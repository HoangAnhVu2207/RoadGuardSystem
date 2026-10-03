using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Defects;

public sealed class DefectWorkflowRepository(RoadGuardDbContext db) : IDefectWorkflowRepository
{
    public Task<DefectViewDto?> ReadAsync(Guid project, Guid defect,
        Func<CancellationToken, Task> guard, CancellationToken token)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await guard(token);
            var row = await db.Defects.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == defect && item.ProjectId == project, token);
            var result = row is null ? null : await ProjectAsync(row, token);
            await transaction.CommitAsync(token);
            return result;
        });

    public Task<DefectPageDto> ListAsync(Guid project, DefectStatus? status, string? type,
        Guid? segment, int pageSize, Guid? afterId,
        Func<CancellationToken, Task> guard, CancellationToken token)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await guard(token);
            var query = db.Defects.AsNoTracking().Where(row => row.ProjectId == project);
            if (status is not null) query = query.Where(row => row.Status == status);
            if (type is not null) query = query.Where(row => row.DefectTypeCode == type);
            if (segment is Guid segmentId)
            {
                var ids = await (from link in db.Set<HuyDefectSourceLink>().AsNoTracking()
                    join decision in db.SourceDecisions.AsNoTracking() on link.DecisionId equals decision.Id
                    where link.ProjectId == project && link.EndedAt == null &&
                        decision.Classification != null && decision.Classification.SegmentId == segmentId
                    select link.DefectId).Distinct().ToArrayAsync(token);
                query = query.Where(row => ids.Contains(row.Id));
            }
            if (afterId is Guid last) query = query.Where(row => row.Id.CompareTo(last) > 0);
            var rows = await query.OrderBy(row => row.Id).Take(pageSize + 1).ToArrayAsync(token);
            var items = new List<DefectViewDto>();
            foreach (var row in rows.Take(pageSize)) items.Add(await ProjectAsync(row, token));
            await transaction.CommitAsync(token);
            return new DefectPageDto(items, rows.Length > pageSize && items.Count > 0
                ? Convert.ToBase64String(items[^1].Id.ToByteArray()) : null);
        });

    public Task<IReadOnlyList<Guid>> LinkedReportsAsync(Guid project, Guid defect, CancellationToken token)
        => ReadLinkedReportsAsync(project, defect, token);

    private async Task<IReadOnlyList<Guid>> ReadLinkedReportsAsync(Guid project, Guid defect, CancellationToken token)
        => await db.Set<HuyDefectSourceLink>().AsNoTracking().Where(link => link.ProjectId == project &&
                link.DefectId == defect && link.EndedAt == null && link.ReportSourceId != null)
            .Select(link => link.ReportSourceId!.Value).Distinct().OrderBy(id => id).ToArrayAsync(token);

    public async Task LockTargetAsync(Guid project, Guid defect, CancellationToken token)
    {
        var row = await db.Defects.FromSqlInterpolated(
                $"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={defect}")
            .AsNoTracking().Select(item => item.ProjectId).SingleOrDefaultAsync(token);
        if (row != project) throw new CaseWorkflowException(404, "not_found");
    }

    public async Task<DefectViewDto> ApplyAssessmentAsync(Guid actor, Guid project, Guid defect,
        string expectedVersion, string type, string? cause, DefectSeverity severity,
        IReadOnlyCollection<Guid> evidenceIds, string reason, Guid? correlation, CancellationToken token)
    {
        var row = await LoadCurrentAsync(project, defect, expectedVersion, token);
        if (!await db.DefectTypes.AnyAsync(item => item.Code == type && item.IsActive, token) ||
            cause is not null && !await db.CauseCategories.AnyAsync(item => item.Code == cause && item.IsActive, token))
            throw new CaseWorkflowException(400, "validation_error");
        DefectVerificationLog log;
        try { log = row.Assess(type, cause, severity, actor, reason, evidenceIds); }
        catch (InvalidOperationException) { throw new CaseWorkflowException(409, "invalid_state_transition"); }
        db.DefectVerificationLogs.Add(log);
        Audit(actor, defect, "defect_assessed", reason, correlation);
        await db.SaveChangesAsync(token);
        return await ProjectAsync(row, token);
    }

    public async Task<DefectViewDto> ApplyVerificationAsync(Guid actor, Guid project, Guid defect,
        string expectedVersion, DefectVerificationAction action, IReadOnlyCollection<Guid> evidenceIds,
        IReadOnlyCollection<Guid> verifiedRelatedEvidenceIds, string reason, Guid? correlation,
        CancellationToken token)
    {
        var row = await LoadCurrentAsync(project, defect, expectedVersion, token);
        DefectVerificationLog log;
        try { log = row.DecideFromExistingEvidence(action, evidenceIds, verifiedRelatedEvidenceIds, actor, reason); }
        catch (InvalidOperationException) { throw new CaseWorkflowException(409, "invalid_state_transition"); }
        db.DefectVerificationLogs.Add(log);
        Audit(actor, defect, "defect_verified", reason, correlation);
        await db.SaveChangesAsync(token);
        return await ProjectAsync(row, token);
    }

    private async Task<Defect> LoadCurrentAsync(Guid project, Guid defect,
        string expectedVersion, CancellationToken token)
    {
        var row = await db.Defects.SingleOrDefaultAsync(item => item.Id == defect && item.ProjectId == project, token)
            ?? throw new CaseWorkflowException(404, "not_found");
        if (Convert.ToBase64String(db.Entry(row).Property<byte[]>("RowVersion").CurrentValue!) != expectedVersion)
            throw new CaseWorkflowException(412, "concurrency_conflict");
        return row;
    }

    private async Task<DefectViewDto> ProjectAsync(Defect row, CancellationToken token)
    {
        var version = db.Entry(row).Property<byte[]>("RowVersion").CurrentValue;
        if (version is null || version.Length == 0)
            version = await db.Defects.AsNoTracking().Where(item => item.Id == row.Id)
                .Select(item => EF.Property<byte[]>(item, "RowVersion")).SingleAsync(token);
        var link = await db.Set<HuyDefectSourceLink>().AsNoTracking().Where(item => item.DefectId == row.Id && item.EndedAt == null)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id).FirstOrDefaultAsync(token);
        Guid? segment = null;
        if (link is not null)
            segment = await db.SourceDecisions.AsNoTracking().Where(item => item.Id == link.DecisionId)
                .Select(item => item.Classification == null ? null : item.Classification.SegmentId)
                .SingleOrDefaultAsync(token);
        return new(row.Id, row.ProjectId!.Value, row.RoadSectionVersionId, segment,
            row.DefectTypeCode, row.CauseCategoryCode, row.Severity.ToString().ToUpperInvariant(),
            row.Status.ToString().ToUpperInvariant(), row.Geometry is null ? null : new(row.Geometry.SRID, row.Geometry.AsText()),
            Convert.ToBase64String(version));
    }

    private void Audit(Guid actor, Guid defect, string eventType, string reason, Guid? correlation)
        => db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, DateTimeOffset.UtcNow, eventType,
            "Defect", defect, null, JsonSerializer.Serialize(new { defectId = defect }), reason,
            "huy01.defect", correlation, ["defectId"]));
}
