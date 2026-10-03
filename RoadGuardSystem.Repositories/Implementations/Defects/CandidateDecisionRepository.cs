using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Defects;

public sealed class CandidateDecisionRepository(RoadGuardDbContext db) : ICandidateDecisionRepository
{
    public async Task LockSourceAsync(Guid reportId, Guid expectedCase, CancellationToken ct)
    {
        await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Reports] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={reportId}").SingleAsync(ct);
        if (!await db.Set<HuyCaseReportLink>().AsNoTracking().AnyAsync(l => l.ReportId == reportId && l.CaseId == expectedCase && l.EndedAt == null, ct))
            throw new CaseWorkflowException(409, "candidate_stale");
        var report = await db.Reports.AsNoTracking().Include(r => r.Supplements).ThenInclude(s => s.Evidence).SingleAsync(r => r.Id == reportId, ct);
        foreach (var file in report.OriginalEvidence.Concat(report.Supplements.SelectMany(s => s.Evidence)).Select(e => e.FileId).Distinct().Order())
        {
            await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={file}").SingleAsync(ct);
            await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={file}").SingleAsync(ct);
            await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={file}").SingleAsync(ct);
        }
        var geometry = await db.IncidentCases.Where(c => c.Id == expectedCase).Select(c => new {
            Route = EF.Property<Guid?>(c, "GeometryRouteVersionId"), Set = EF.Property<Guid?>(c, "GeometrySegmentSetId") }).SingleAsync(ct);
        if (geometry.Route is null || geometry.Set is null) throw new CaseWorkflowException(409, "source_not_ready");
        await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [RoadSectionVersions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={geometry.Route}").SingleAsync(ct);
        await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [RoadSegmentSets] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={geometry.Set}").SingleAsync(ct);
        await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [CandidateSourceHeads] WITH (UPDLOCK,HOLDLOCK) WHERE [SourceKind]={(int)CandidateSourceKind.Report} AND [SourceId]={reportId}").SingleAsync(ct);
    }

    public async Task<CandidateDecisionResponseDto> SaveRejectAsync(Guid actor, CandidateSourceFacts facts, CandidateCorrection? correction,
        string reason, Guid? correlation, CancellationToken ct)
    {
        var head = await db.Set<HuyCandidateSourceHead>().SingleOrDefaultAsync(h => h.SourceKind == facts.Source.Kind && h.SourceId == facts.Source.Id, ct);
        if (head is not null && (facts.ActiveDisposition is null || head.DecisionId != facts.ActiveDisposition.DecisionId)) throw new CaseWorkflowException(412, "concurrency_conflict");
        // A correction that would detach an accepted defect needs the downstream-use contract.
        if (head is not null && await db.SourceDecisions.AnyAsync(d => d.Id == head.DecisionId && d.Decision != CandidateDecisionKind.Reject, ct)) throw new CaseWorkflowException(409, "invalid_state_transition");
        CandidateDecision decision;
        try { decision = CandidateDecision.Create(Guid.NewGuid(), facts, CandidateDecisionKind.Reject, null, Guid.Empty, null, correction, actor, reason, DateTimeOffset.UtcNow); }
        catch (InvalidOperationException) { throw new CaseWorkflowException(412, "concurrency_conflict"); }
        db.SourceDecisions.Add(decision);
        db.Entry(decision).Property<CandidateSourceKind>("SourceKind").CurrentValue = facts.Source.Kind;
        db.Entry(decision).Property<Guid>("SourceId").CurrentValue = facts.Source.Id;
        db.Entry(decision).Property<Guid?>("ReportSourceId").CurrentValue = facts.Source.Id;
        if (head is null) db.Set<HuyCandidateSourceHead>().Add(new() { SourceKind = facts.Source.Kind, SourceId = facts.Source.Id, ProjectId = facts.ProjectId, DecisionId = decision.Id });
        else head.DecisionId = decision.Id;
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, decision.DecidedAt, "candidate_decided", "CandidateDecision", decision.Id,
            null, JsonSerializer.Serialize(new { decisionId = decision.Id, sourceId = facts.Source.Id }), reason, "huy01.candidate", correlation, ["decisionId", "sourceId"]));
        await db.SaveChangesAsync(ct);
        return Project(decision, db.Entry(decision).Property<byte[]>("RowVersion").CurrentValue!);
    }

    public async Task<CandidateDecisionResponseDto> SaveAcceptedAsync(Guid actor, CandidateSourceFacts facts,
        CandidateDecisionKind kind, CandidateClassification? classification, Guid? targetDefectId,
        string? targetVersion, CandidateCorrection? correction, string reason, Guid? correlation, CancellationToken ct)
    {
        if (facts.Source.Kind != CandidateSourceKind.Report || kind is not (CandidateDecisionKind.KeepNew or CandidateDecisionKind.LinkExisting))
            throw new CaseWorkflowException(409, "source_not_ready");
        var head = await db.Set<HuyCandidateSourceHead>().SingleOrDefaultAsync(
            item => item.SourceKind == facts.Source.Kind && item.SourceId == facts.Source.Id, ct);
        if (head is not null && (facts.ActiveDisposition is null || head.DecisionId != facts.ActiveDisposition.DecisionId))
            throw new CaseWorkflowException(412, "concurrency_conflict");
        if (head is not null && await db.SourceDecisions.AnyAsync(
                item => item.Id == head.DecisionId && item.Decision != CandidateDecisionKind.Reject, ct))
            throw new CaseWorkflowException(409, "invalid_state_transition");

        var now = DateTimeOffset.UtcNow;
        Guid defectId;
        Defect? created = null;
        if (kind == CandidateDecisionKind.KeepNew)
        {
            if (classification is null ||
                !await db.DefectTypes.AnyAsync(item => item.Code == classification.DefectTypeCode && item.IsActive, ct) ||
                classification.CauseCategoryCode is { } cause &&
                !await db.CauseCategories.AnyAsync(item => item.Code == cause && item.IsActive, ct))
                throw new CaseWorkflowException(400, "validation_error");
            defectId = Guid.NewGuid();
            created = Defect.CreateFromReport(defectId, facts, classification, null, now);
        }
        else
        {
            if (targetDefectId is not Guid target || target == Guid.Empty || string.IsNullOrWhiteSpace(targetVersion))
                throw new CaseWorkflowException(400, "validation_error");
            var persisted = await db.Defects.FromSqlInterpolated(
                    $"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={target}")
                .AsNoTracking()
                .Select(item => new { item.Id, item.ProjectId, Version = EF.Property<byte[]>(item, "RowVersion") })
                .SingleOrDefaultAsync(ct);
            if (persisted is null || persisted.ProjectId != facts.ProjectId)
                throw new CaseWorkflowException(404, "not_found");
            if (Convert.ToBase64String(persisted.Version) != targetVersion)
                throw new CaseWorkflowException(412, "concurrency_conflict");
            defectId = target;
        }

        CandidateDecision decision;
        try
        {
            decision = CandidateDecision.Create(Guid.NewGuid(), facts, kind, classification,
                targetDefectId ?? Guid.Empty, targetVersion, correction, actor, reason, now);
        }
        catch (InvalidOperationException) { throw new CaseWorkflowException(412, "concurrency_conflict"); }
        db.SourceDecisions.Add(decision);
        db.Entry(decision).Property<CandidateSourceKind>("SourceKind").CurrentValue = facts.Source.Kind;
        db.Entry(decision).Property<Guid>("SourceId").CurrentValue = facts.Source.Id;
        db.Entry(decision).Property<Guid?>("ReportSourceId").CurrentValue = facts.Source.Id;
        if (created is not null) db.Defects.Add(created);
        db.Set<HuyDefectSourceLink>().Add(new()
        {
            Id = Guid.NewGuid(), SourceKind = facts.Source.Kind, SourceId = facts.Source.Id,
            ReportSourceId = facts.Source.Id, ProjectId = facts.ProjectId, DefectId = defectId,
            DecisionId = decision.Id, CreatedAt = now
        });
        if (head is null)
            db.Set<HuyCandidateSourceHead>().Add(new() { SourceKind = facts.Source.Kind,
                SourceId = facts.Source.Id, ProjectId = facts.ProjectId, DecisionId = decision.Id });
        else head.DecisionId = decision.Id;
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, now, "candidate_decided", "CandidateDecision", decision.Id,
            null, JsonSerializer.Serialize(new { decisionId = decision.Id, sourceId = facts.Source.Id }), reason,
            "huy01.candidate", correlation, ["decisionId", "sourceId"]));
        await db.SaveChangesAsync(ct);
        return Project(decision, db.Entry(decision).Property<byte[]>("RowVersion").CurrentValue!, defectId);
    }

    public Task<CandidateDecisionResponseDto?> ReadAsync(Guid projectId, Guid decisionId, Func<CancellationToken, Task> guard, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await guard(ct);
            var row = await db.SourceDecisions.AsNoTracking().Where(d => d.Id == decisionId && d.ProjectId == projectId)
                .Select(d => new { Decision = d, Version = EF.Property<byte[]>(d, "RowVersion") }).SingleOrDefaultAsync(ct);
            var linkedDefect = row is null ? null : await db.Set<HuyDefectSourceLink>().AsNoTracking()
                .Where(link => link.DecisionId == row.Decision.Id).Select(link => (Guid?)link.DefectId)
                .SingleOrDefaultAsync(ct);
            await tx.CommitAsync(ct);
            return row is null ? null : Project(row.Decision, row.Version, linkedDefect);
        });
    private static CandidateDecisionResponseDto Project(CandidateDecision d, byte[] version, Guid? defectId = null)
        => new(d.Id, d.Source.Kind == CandidateSourceKind.Report ? "REPORT" : "AI_DETECTION", d.Source.Id,
            d.Decision == CandidateDecisionKind.Reject ? "REJECT" : d.Decision == CandidateDecisionKind.KeepNew ? "KEEP_NEW" : "LINK_EXISTING",
            defectId ?? d.TargetDefectId, Convert.ToBase64String(version), d.SupersedesDecisionId);
}
