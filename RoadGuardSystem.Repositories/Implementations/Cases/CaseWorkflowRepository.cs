using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

public sealed class CaseWorkflowRepository(RoadGuardDbContext db) : ICaseWorkflowRepository
{
    public async Task GuardAsync(Guid actor, UserRoleCode role, IReadOnlyCollection<Guid> caseIds, Guid? requestedProject,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) Fail(403, "access_forbidden");
        await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", actor, ct);
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == actor && u.RoleCode == role && u.Status == UserStatus.Active && !u.MustChangePassword, ct)) Fail(403, "access_forbidden");
        var projects = new HashSet<Guid>();
        if (requestedProject is Guid project) projects.Add(project);
        var rows = await db.IncidentCases.AsNoTracking().Where(c => caseIds.Contains(c.Id)).Select(c => new { c.Id, c.ProjectId }).ToArrayAsync(ct);
        if (rows.Length != caseIds.Distinct().Count()) Fail(404, "not_found");
        if (role == UserRoleCode.ProjectManager && rows.Any(c => c.ProjectId == null)) Fail(403, "access_forbidden");
        foreach (var row in rows) if (row.ProjectId is Guid p) projects.Add(p);
        foreach (var p in projects.Order())
        {
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Projects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", p, ct);
            if (!await db.Projects.AsNoTracking().AnyAsync(x => x.Id == p, ct)) Fail(404, "not_found");
            await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [ProjectMembers] WITH (UPDLOCK,HOLDLOCK) WHERE [ProjectId]={p} AND [UserId]={actor}").SingleAsync(ct);
            if (!await projectAccess(p, ct)) Fail(403, "access_forbidden");
        }
        foreach (var id in caseIds.Order()) await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [IncidentCases] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", id, ct);
        // Assignment may have changed before the case lock. Never use the earlier scope as authority.
        var fresh = await db.IncidentCases.AsNoTracking().Where(c => caseIds.Contains(c.Id)).Select(c => c.ProjectId).ToArrayAsync(ct);
        if (fresh.Any(p => p is Guid actual && !projects.Contains(actual)) || role == UserRoleCode.ProjectManager && fresh.Any(p => p is null)) Fail(412, "concurrency_conflict");
    }

    public Task<InternalCaseDto> ReadAsync(Guid actor, UserRoleCode role, Guid caseId, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
        => InReadAsync(async () => { await GuardAsync(actor, role, [caseId], null, projectAccess, ct); return Project(await LoadAsync(caseId, ct)); }, ct);

    public async Task GuardCommandAsync(Guid actor, UserRoleCode role, CaseCommand command,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
    {
        await GuardAsync(actor, role, (command.SourceCaseVersions?.Keys ?? []).Append(command.CaseId).Distinct().ToArray(), command.ProjectId, projectAccess, ct);
        if (command.EvidenceIds is { Count: > 0 })
        {
            var incident = await db.IncidentCases.AsNoTracking().SingleAsync(c => c.Id == command.CaseId, ct);
            await ResolveEvidenceAsync(incident, command.EvidenceIds, ct);
        }
    }

    public Task<CasePageDto> ListAsync(Guid actor, UserRoleCode role, Guid? project, IncidentCaseStatus? status,
        int pageSize, string? cursor, Func<Guid, CancellationToken, Task<bool>> projectAccess, CancellationToken ct)
        => InReadAsync(async () =>
        {
            if (project is null && role != UserRoleCode.Supervisor) Fail(403, "access_forbidden");
            await GuardAsync(actor, role, [], project, projectAccess, ct);
            var query = db.IncidentCases.Where(c => c.ProjectId == project);
            if (status is not null) query = query.Where(c => c.Status == status);
            if (cursor is not null)
            {
                PageCursor value;
                try { value = JsonSerializer.Deserialize<PageCursor>(Convert.FromBase64String(cursor))!; }
                catch (Exception e) when (e is FormatException or JsonException) { throw new CaseWorkflowException(400, "validation_error"); }
                if (value is null || value.Actor != actor || value.Project != project || value.Status != status || value.Id == Guid.Empty) Fail(400, "validation_error");
                query = query.Where(c => c.CreatedAt < value!.CreatedAt || c.CreatedAt == value.CreatedAt && c.Id.CompareTo(value.Id) < 0);
            }
            var rows = await query.AsNoTracking().OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
                .Select(c => new { c.Id, c.CreatedAt }).Take(pageSize + 1).ToArrayAsync(ct);
            var items = new List<InternalCaseDto>();
            foreach (var row in rows.Take(pageSize)) items.Add(Project(await LoadAsync(row.Id, ct)));
            var last = rows.Take(pageSize).LastOrDefault();
            return new CasePageDto(items, rows.Length > pageSize && last is not null ? Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new PageCursor(actor, project, status, last.CreatedAt, last.Id))) : null);
        }, ct);

    public async Task<CaseWriteResult> ApplyAsync(Guid actor, UserRoleCode role, CaseCommand command,
        Func<Guid, CancellationToken, Task<bool>> projectAccess, Func<CancellationToken, Task>? geometryCheck, Guid? correlation, CancellationToken ct)
    {
        var sourceIds = command.SourceCaseVersions?.Keys.ToArray() ?? [];
        await GuardAsync(actor, role, sourceIds.Append(command.CaseId).Distinct().ToArray(), command.ProjectId, projectAccess, ct);
        if (command.Action != "triage" && role != UserRoleCode.ProjectManager) Fail(403, "access_forbidden");
        var incident = await LoadAsync(command.CaseId, ct);
        if (Version(incident) != command.ExpectedVersion) Fail(412, "concurrency_conflict");
        var now = DateTimeOffset.UtcNow;
        var resultCase = incident;
        CasePublication? publication = null;
        var movedReports = command.ReportIds ?? [];
        try
        {
            switch (command.Action)
            {
                case "triage":
                    if (incident.ProjectId is null)
                    {
                        if (role != UserRoleCode.Supervisor) Fail(403, "access_forbidden");
                        incident.Triage(command.ProjectId!.Value, command.Method!.Value, command.Reason, now);
                    }
                    else
                    {
                        if (role != UserRoleCode.ProjectManager) Fail(403, "access_forbidden");
                        if (incident.ProjectId != command.ProjectId) Fail(409, "reference_scope_conflict");
                        incident.SelectVerificationMethod(command.Method!.Value, command.Reason, now);
                    }
                    if (geometryCheck is not null)
                    {
                        await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [RoadSectionVersions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", command.RouteVersionId!.Value, ct);
                        await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [RoadSegmentSets] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", command.SegmentSetId!.Value, ct);
                        await geometryCheck(ct);
                        db.Entry(incident).Property<Guid?>("GeometryRouteVersionId").CurrentValue = command.RouteVersionId;
                        db.Entry(incident).Property<Guid?>("GeometrySegmentSetId").CurrentValue = command.SegmentSetId;
                    }
                    break;
                case "link":
                    var links = await db.Set<HuyCaseReportLink>().Where(l => movedReports.Contains(l.ReportId) && l.EndedAt == null).ToArrayAsync(ct);
                    if (links.Length != movedReports.Count || links.Any(l => !sourceIds.Contains(l.CaseId)) || sourceIds.Any(id => links.All(l => l.CaseId != id))) Fail(412, "concurrency_conflict");
                    foreach (var id in sourceIds.Order())
                    {
                        var source = await LoadAsync(id, ct);
                        if (Version(source) != command.SourceCaseVersions![id]) Fail(412, "concurrency_conflict");
                        if (source.ProjectId != incident.ProjectId) Fail(409, "reference_scope_conflict");
                        incident.LinkReportsFrom(source, links.Where(l => l.CaseId == id).Select(l => l.ReportId).ToArray(), actor, command.Reason, now);
                        Bump(source); PersistHistory(source);
                    }
                    foreach (var link in links) link.EndedAt = now;
                    await db.SaveChangesAsync(ct); // close old active links before inserting replacements; still the same transaction
                    foreach (var id in movedReports) db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = id, StartedAt = now });
                    break;
                case "split":
                    resultCase = incident.SplitReports(Guid.NewGuid(), movedReports, actor, command.Reason, now);
                    db.IncidentCases.Add(resultCase);
                    db.Entry(resultCase).Property<Guid?>("GeometryRouteVersionId").CurrentValue = db.Entry(incident).Property<Guid?>("GeometryRouteVersionId").CurrentValue;
                    db.Entry(resultCase).Property<Guid?>("GeometrySegmentSetId").CurrentValue = db.Entry(incident).Property<Guid?>("GeometrySegmentSetId").CurrentValue;
                    var oldLinks = await db.Set<HuyCaseReportLink>().Where(l => l.CaseId == incident.Id && movedReports.Contains(l.ReportId) && l.EndedAt == null).ToArrayAsync(ct);
                    if (oldLinks.Length != movedReports.Count) Fail(412, "concurrency_conflict");
                    foreach (var link in oldLinks) link.EndedAt = now;
                    Bump(resultCase); Bump(incident); PersistHistory(incident);
                    await db.SaveChangesAsync(ct);
                    foreach (var id in movedReports) db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = resultCase.Id, ReportId = id, StartedAt = now });
                    break;
                case "conclude":
                    if (command.Outcome != CaseConclusionOutcome.NeedsEvidence && incident.VerificationMethod != CaseVerificationMethod.ExistingEvidence)
                        Fail(409, "source_not_ready"); // FIELD/DRONE completion/provenance producer is not integrated
                    var conclusionEvidence = await ResolveEvidenceAsync(incident, command.EvidenceIds!, ct);
                    var verifiedDefects = await ResolveDefectsAsync(incident, command.DefectIds!, ct);
                    var conclusion = CaseConclusion.Create(Guid.NewGuid(), command.Outcome!.Value, command.DefectIds!, command.EvidenceIds!, command.Reason, now);
                    incident.Conclude(conclusion, CaseConclusionPrerequisites.Create(verifiedDefects, conclusionEvidence.Select(e => e.Id).ToArray()));
                    db.Set<CaseConclusion>().Add(conclusion); db.Entry(conclusion).Property<Guid>("CaseId").CurrentValue = incident.Id;
                    foreach (var id in conclusion.DefectIds) db.Set<HuyConclusionDefect>().Add(new() { ConclusionId = conclusion.Id, DefectId = id });
                    foreach (var evidence in conclusionEvidence) db.Set<HuyConclusionEvidence>().Add(new() { ConclusionId = conclusion.Id, EvidenceId = evidence.Id, SourceReportId = evidence.ReportId,
                        OriginalEvidenceId = evidence.SupplementId is null ? evidence.Id : null, SupplementEvidenceId = evidence.SupplementId is not null ? evidence.Id : null });
                    break;
                case "publish":
                    var selected = await ResolveEvidenceAsync(incident, command.EvidenceIds!, ct);
                    var defects = await ResolveDefectsAsync(incident, command.DefectIds!, ct);
                    var recipientFacts = new List<CasePublicationRecipientFacts>();
                    foreach (var recipient in movedReports)
                    {
                        var related = await RelatedDefectsAsync(recipient, ct);
                        recipientFacts.Add(CasePublicationRecipientFacts.Create(recipient, defects.Where(related.Contains).ToArray(), selected.Where(e => e.ReportId == recipient).Select(e => e.Id).ToArray()));
                    }
                    publication = incident.Publish(Guid.NewGuid(), movedReports, command.DefectIds!, command.EvidenceIds!, command.Reason, now, CasePublicationPrerequisites.Create(recipientFacts));
                    db.Set<CasePublication>().Add(publication);
                    foreach (var recipient in movedReports)
                    {
                        db.Set<HuyPublicationRecipient>().Add(new() { PublicationId = publication.Id, ReportId = recipient });
                        foreach (var evidence in selected) db.Set<HuyPublicationEvidence>().Add(new() { PublicationId = publication.Id, RecipientReportId = recipient,
                            EvidenceId = evidence.Id, SourceReportId = evidence.ReportId, OriginalEvidenceId = evidence.SupplementId is null ? evidence.Id : null,
                            SupplementEvidenceId = evidence.SupplementId is not null ? evidence.Id : null });
                    }
                    foreach (var id in publication.DefectIds) db.Set<HuyPublicationDefect>().Add(new() { PublicationId = publication.Id, DefectId = id });
                    break;
                default: Fail(400, "validation_error"); break;
            }
        }
        catch (InvalidOperationException) { throw new CaseWorkflowException(409, "invalid_state_transition"); }
        Bump(incident); PersistHistory(incident);
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, now, "case_" + command.Action, "IncidentCase", incident.Id, null,
            JsonSerializer.Serialize(new { caseId = incident.Id, resultCaseId = resultCase.Id }), command.Reason, "huy01.case", correlation, ["caseId", "resultCaseId"]));
        await db.SaveChangesAsync(ct);
        return new(Project(resultCase), publication is null ? null : new(publication.Id, incident.Id, "PUBLISHED", Version(incident)));
    }

    private async Task<IncidentCase> LoadAsync(Guid id, CancellationToken ct)
    {
        var incident = await db.IncidentCases.Include(c => c.Conclusions.OrderBy(x => x.ConcludedAt).ThenBy(x => x.Id)).Include(c => c.Publications).SingleAsync(c => c.Id == id, ct);
        incident.MaterializeLinkHistory(await db.Set<CaseReportLinkHistory>().Where(h => h.FromCaseId == id || h.ToCaseId == id).ToArrayAsync(ct));
        var links = await db.Set<HuyCaseReportLink>().Where(l => l.CaseId == id && l.EndedAt == null).Select(l => l.ReportId).ToArrayAsync(ct);
        if (!links.Order().SequenceEqual(incident.ActiveReportIds.Order())) Fail(409, "reference_scope_conflict");
        return incident;
    }

    private async Task<ReportEvidence[]> ResolveEvidenceAsync(IncidentCase incident, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var reports = await db.Reports.AsNoTracking().Include(r => r.Supplements).ThenInclude(s => s.Evidence).Where(r => incident.ActiveReportIds.Contains(r.Id)).ToArrayAsync(ct);
        var evidence = reports.SelectMany(r => r.OriginalEvidence.Concat(r.Supplements.SelectMany(s => s.Evidence))).Where(e => ids.Contains(e.Id)).ToArray();
        if (evidence.Length != ids.Count) Fail(404, "not_found");
        foreach (var item in evidence.OrderBy(e => e.FileId))
        {
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", item.FileId, ct);
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={0}", item.FileId, ct);
            await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={0}", item.FileId, ct);
            var source = await (from scope in db.FileScopes.AsNoTracking() join upload in db.UploadSessions.AsNoTracking() on scope.FileId equals upload.FileId
                where scope.FileId == item.FileId select new { scope, upload }).SingleOrDefaultAsync(ct);
            if (source is null || source.scope.OwnerUserId != item.OwnerUserId || source.scope.ProjectId is not null || source.scope.TargetId is not null || source.scope.Purpose != "REPORT_PHOTO") Fail(404, "not_found");
            if (source!.upload.Status != UploadSessionStatus.Verified) Fail(409, "source_not_ready");
            if (Convert.ToBase64String(source.upload.RowVersion) != item.FileVersion) Fail(412, "concurrency_conflict");
        }
        return evidence;
    }

    private async Task<Guid[]> ResolveDefectsAsync(IncidentCase incident, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        foreach (var id in ids.Order()) await LockAsync("SELECT CAST(COUNT(*) AS int) AS [Value] FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={0}", id, ct);
        var related = new HashSet<Guid>();
        foreach (var report in incident.ActiveReportIds) foreach (var id in await RelatedDefectsAsync(report, ct)) related.Add(id);
        var verified = await db.Defects.AsNoTracking().Where(d => ids.Contains(d.Id) && d.ProjectId == incident.ProjectId && d.Status == DefectStatus.Verified).Select(d => d.Id).ToArrayAsync(ct);
        if (verified.Length != ids.Count || ids.Any(id => !related.Contains(id))) Fail(409, "source_not_ready");
        return verified;
    }

    private Task<Guid[]> RelatedDefectsAsync(Guid report, CancellationToken ct)
        => (from head in db.Set<HuyCandidateSourceHead>() join decision in db.SourceDecisions on head.DecisionId equals decision.Id
            where head.SourceKind == CandidateSourceKind.Report && head.SourceId == report && decision.TargetDefectId != null select decision.TargetDefectId!.Value).ToArrayAsync(ct);

    private void PersistHistory(IncidentCase incident)
    {
        foreach (var history in incident.LinkHistory.Where(h => db.Entry(h).State == EntityState.Detached))
        {
            db.Set<CaseReportLinkHistory>().Add(history);
            foreach (var id in history.ReportIds) db.Set<HuyLinkHistoryReport>().Add(new() { HistoryId = history.Id, ReportId = id });
        }
    }
    private void Bump(IncidentCase incident) => db.Entry(incident).Property<long>("Revision").CurrentValue++;
    private string Version(IncidentCase incident) => Convert.ToBase64String(db.Entry(incident).Property<byte[]>("RowVersion").CurrentValue!);
    private InternalCaseDto Project(IncidentCase incident)
    {
        var conclusion = incident.Conclusions.OrderBy(c => c.ConcludedAt).ThenBy(c => c.Id).LastOrDefault();
        return new(incident.Id, incident.ProjectId, WireStatus(incident.Status), Version(incident), incident.ActiveReportIds,
            incident.Conclusions.SelectMany(c => c.DefectIds).Distinct().ToArray(), WireMethod(incident.VerificationMethod), "UNKNOWN",
            conclusion is null ? null : new(conclusion.Id, WireOutcome(conclusion.Outcome), conclusion.Reason, conclusion.ConcludedAt, conclusion.DefectIds, conclusion.EvidenceIds), incident.CreatedAt);
    }
    public static string WireStatus(IncidentCaseStatus status) => status == IncidentCaseStatus.AwaitingEvidence ? "AWAITING_EVIDENCE" : status.ToString().ToUpperInvariant();
    public static string? WireMethod(CaseVerificationMethod? method) => method == CaseVerificationMethod.ExistingEvidence ? "EXISTING_EVIDENCE" : method?.ToString().ToUpperInvariant();
    public static string WireOutcome(CaseConclusionOutcome outcome) => outcome == CaseConclusionOutcome.NoDefect ? "NO_DEFECT" : outcome == CaseConclusionOutcome.NeedsEvidence ? "NEEDS_EVIDENCE" : "CONFIRMED";
    private Task<int> LockAsync(string sql, Guid id, CancellationToken ct) => db.Database.SqlQueryRaw<int>(sql, id).SingleAsync(ct);
    private Task<T> InReadAsync<T>(Func<Task<T>> action, CancellationToken ct) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    { db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct); var value = await action(); await tx.CommitAsync(ct); return value; });
    private static void Fail(int status, string code) => throw new CaseWorkflowException(status, code);
    private sealed record PageCursor(Guid Actor, Guid? Project, IncidentCaseStatus? Status, DateTimeOffset CreatedAt, Guid Id);
}
