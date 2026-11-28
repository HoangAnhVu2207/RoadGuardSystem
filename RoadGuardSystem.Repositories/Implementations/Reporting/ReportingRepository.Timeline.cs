using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Reporting;

namespace RoadGuardSystem.Repositories.Implementations.Reporting;

public sealed partial class ReportingRepository
{
    // This is a metadata-only read after current project authority, in the reporting transaction.
    public async Task<ReportingTimelineItemFact[]> OperationalTimelineAsync(Guid project, ReportingFiltersFact filters, CancellationToken token)
    {
        var items = from item in db.Set<RepairItem>().AsNoTracking()
                    join owner in db.Set<ObligationResponsibility>().AsNoTracking() on item.ObligationId equals owner.ObligationId into owners
                    from owner in owners.DefaultIfEmpty()
                    where (owner == null ? item.ProjectId : owner.CurrentProjectId) == project &&
                        (owner == null || owner.OriginProjectId == item.ProjectId)
                    select item;
        var fields = from e in db.Set<FieldInspectionTaskEvent>().AsNoTracking()
                     join task in db.FieldInspectionTasks.AsNoTracking() on e.TaskId equals task.Id
                     where e.ProjectId == task.ProjectId &&
                         (task.RepairItemId == null && task.ProjectId == project ||
                          items.Any(i => i.Id == task.RepairItemId && i.ProjectId == task.ProjectId && i.DefectId == task.DefectId &&
                              db.Set<RepairFieldTaskBinding>().Any(b => b.TaskId == task.Id && b.ItemId == i.Id && b.ObligationId == i.ObligationId))) &&
                         (filters.From == null || e.OccurredAt >= filters.From) && (filters.To == null || e.OccurredAt < filters.To)
                     select e;
        var repairs = from e in db.Set<RepairItemLifecycleEvent>().AsNoTracking()
                      join item in items on e.ItemId equals item.Id
                      where e.ProjectId == item.ProjectId && e.ObligationId == item.ObligationId && e.DefectId == item.DefectId &&
                          (filters.From == null || e.At >= filters.From) && (filters.To == null || e.At < filters.To)
                      select e;
        var result = (await fields.ToArrayAsync(token)).Select(e => new ReportingTimelineItemFact(e.Id, e.OccurredAt, null,
            "FIELD." + e.Kind, new("FieldInspectionTask", e.TaskId, e.Id.ToString("N")), "FIELD workflow event recorded.", e.ActorId))
            .Concat((await repairs.ToArrayAsync(token)).Select(e => new ReportingTimelineItemFact(e.Id, e.At, null,
                "REPAIR." + e.Kind, new("RepairItem", e.ItemId, e.SourceVersion), "Repair workflow event recorded.", e.ActorId))).ToList();
        // Proposal/approval/assignment/cancellation already have lifecycle events above. Do not duplicate their audits.
        var repairAuditKinds = new[] { "repair_assessment", "repair_execution_start", "repair_execution_finish",
            "repair_attempt_intake", "repair_attempt_supplement", "repair_attempt_reviewed", "repair_supplement_requested",
            "repair_final_confirmed", "repair_decision_corrected", "repair_review_requested" };
        var repairAudits = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "RepairItem" &&
            items.Any(i => i.Id == a.EntityId) && repairAuditKinds.Contains(a.EventType) &&
            (filters.From == null || a.OccurredAtUtc >= filters.From) && (filters.To == null || a.OccurredAtUtc < filters.To)).ToArrayAsync(token);
        result.AddRange(repairAudits.Select(a => new ReportingTimelineItemFact(a.Id, a.OccurredAtUtc, null, a.EventType,
            new("RepairItem", a.EntityId, a.Id.ToString("N")), "Repair workflow event recorded; decision and evidence contents require their own read authority.", a.ActorUserId)));
        var jobs = db.ProcessingJobs.AsNoTracking().Where(j => j.ProjectId == project).Select(j => j.Id);
        var mocks = db.Set<RoadGuardSystem.BusinessObjects.Processing.AiMockRun>().AsNoTracking().Where(j => j.ProjectId == project).Select(j => j.Id);
        var audits = await db.AuditLogs.AsNoTracking().Where(a =>
            (a.EntityType == "ProcessingJob" && jobs.Contains(a.EntityId) && (a.EventType == "processing_job_created" || a.EventType == "processing_job_retried") ||
             a.EntityType == "AiMockRun" && mocks.Contains(a.EntityId) && (a.EventType == "anh02_ai_mock_admitted" || a.EventType == "anh02_ai_mock_completed" || a.EventType == "anh02_ai_mock_failed")) &&
            (filters.From == null || a.OccurredAtUtc >= filters.From) && (filters.To == null || a.OccurredAtUtc < filters.To)).ToArrayAsync(token);
        result.AddRange(audits.Select(a => new ReportingTimelineItemFact(a.Id, a.OccurredAtUtc, null, a.EventType,
            new(a.EntityType, a.EntityId, a.Id.ToString("N")), "AI processing workflow event recorded.", a.ActorUserId)));
        var outcomes = await (from e in db.Set<AiVideoAttemptOutcome>().AsNoTracking()
                              join work in db.AiVideoWorkItems.AsNoTracking() on e.WorkId equals work.Id
                              where work.ProjectId == project && e.ProcessingJobId == work.ProcessingJobId &&
                                  (filters.From == null || e.OccurredAt >= filters.From) && (filters.To == null || e.OccurredAt < filters.To)
                              select e).ToArrayAsync(token);
        result.AddRange(outcomes.Select(e => new ReportingTimelineItemFact(e.AttemptId, e.OccurredAt, null,
            "AI_ATTEMPT." + e.State, new("AiVideoAttemptOutcome", e.WorkId, e.AdmissionHash),
            "Persisted worker terminal outcome; no human executor is recorded.", ActorKind: "SYSTEM")));
        return await ResolveTimelineActorsAsync(result.ToArray(), token);
    }

    private async Task<ReportingTimelineItemFact[]> ResolveTimelineActorsAsync(ReportingTimelineItemFact[] rows, CancellationToken token)
    {
        var ids = rows.Where(x => x.ActorId.HasValue).Select(x => x.ActorId!.Value).Distinct().ToArray();
        var users = await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id) && x.Status == UserStatus.Active)
            .Select(x => new { x.Id, x.DisplayName }).ToDictionaryAsync(x => x.Id, token);
        return rows.Select(row => row.ActorKind == "SYSTEM" ? row : row.ActorId is Guid id && users.TryGetValue(id, out var user)
            ? row with
            {
                ActorDisplay = string.IsNullOrWhiteSpace(user.DisplayName) ? null : user.DisplayName,
                ActorKind = "USER",
                ActorUnavailableReason = null,
                ActorDisplaySemantics = "CURRENT_ACCOUNT"
            }
            : row with
            {
                ActorId = null,
                ActorDisplay = null,
                ActorKind = "UNKNOWN",
                ActorUnavailableReason = row.ActorId.HasValue ? "ACCOUNT_UNAVAILABLE" : "ACTOR_NOT_RECORDED",
                ActorDisplaySemantics = null
            }).ToArray();
    }
}
