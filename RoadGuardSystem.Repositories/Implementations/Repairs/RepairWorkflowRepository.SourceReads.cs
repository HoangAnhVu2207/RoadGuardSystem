using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Inspections;
using System.Data;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository
{
    private async Task<RepairHistoryFact> HistoryViewAsync(RepairItem item, RepairItemFact view, CancellationToken token)
    {
        var bindings = await (from binding in db.Set<RepairFieldTaskBinding>().AsNoTracking()
                              join task in db.FieldInspectionTasks.AsNoTracking() on binding.TaskId equals task.Id
                              where binding.ItemId == item.Id && binding.ProjectId == item.ProjectId && binding.ObligationId == item.ObligationId &&
                                  binding.DefectId == item.DefectId && task.RepairItemId == item.Id && task.ProjectId == item.ProjectId && task.DefectId == item.DefectId
                              orderby task.Id
                              select new RepairTaskBindingFact(item.Id, item.ObligationId, task.Id, binding.AssignmentId,
                                  task.TaskMode, binding.AuthorizationId, binding.PolicyRevisionId, task.RoadSectionVersionId,
                                  task.SegmentSetId, task.LayoutRevisionId, task.MapPublicationId, task.CrsProfileRevisionId,
                                  task.SlabId, null)).ToArrayAsync(token);
        var links = await (from link in db.Set<RepairAttemptSubmissionLink>().AsNoTracking()
                           join binding in db.Set<RepairFieldTaskBinding>().AsNoTracking() on link.BindingId equals binding.Id
                           join submission in db.Set<RoadGuardSystem.BusinessObjects.Inspections.FieldInspectionSubmission>().AsNoTracking() on link.SubmissionId equals submission.Id
                           where link.ItemId == item.Id && link.ProjectId == item.ProjectId && binding.ItemId == item.Id &&
                               binding.ObligationId == item.ObligationId && binding.ProjectId == item.ProjectId &&
                               submission.ProjectId == item.ProjectId && submission.TaskId == binding.TaskId &&
                               submission.ContentHash == link.SubmissionContentHash
                           orderby link.Id
                           select new RepairIntakeHistoryFact(link.Id, link.AttemptId, link.SubmissionId,
                               link.FormalRootSubmissionId, link.PreviousLinkId, binding.TaskId, link.SubmissionContentHash,
                               item.CurrentIntakeLinkId == link.Id && item.CurrentAttemptId == link.AttemptId && item.EffectiveIntakeSubmissionId == link.SubmissionId))
            .ToArrayAsync(token);
        var linkIds = links.Select(row => row.Id).ToArray();
        var persistedReviews = await db.Set<RepairAttemptReview>().AsNoTracking()
            .Where(row => row.ItemId == item.Id && row.ProjectId == item.ProjectId && linkIds.Contains(row.IntakeLinkId))
            .OrderBy(row => row.At).ThenBy(row => row.Id).ToArrayAsync(token);
        var reviews = persistedReviews.Where(row => links.Any(link => link.Id == row.IntakeLinkId &&
            link.AttemptId == row.AttemptId && link.SubmissionId == row.SubmissionId && link.ContentHash == row.SubmissionContentHash))
            .Select(row => new RepairReviewHistoryFact(row.Id, row.AttemptId, row.IntakeLinkId, row.SubmissionId,
                row.SubmissionContentHash, row.ActorId, HistoryRole(row.Role), row.Decision, row.Reason, row.At,
                item.CurrentReviewId == row.Id && item.CurrentAttemptId == row.AttemptId &&
                item.CurrentIntakeLinkId == row.IntakeLinkId && item.EffectiveIntakeSubmissionId == row.SubmissionId)).ToArray();
        return HistoryView(item, view) with { IntakeSubmissions = links, AttemptReviews = reviews, TaskBindings = bindings };
    }
    public async Task<RepairWorkflowResult> ReadSourceTaskAsync(Guid actor, UserRoleCode role, Guid project,
        Guid package, Guid item, Guid task, string kind, Guid? resource, CancellationToken token)
    {
        if (kind is not ("geometry" or "history" or "submission" or "evidence")) return new(400, "validation_error");
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                await GuardAsync(actor, role, project, package, item, false, token);
                var origin = await (from work in db.Set<RepairItem>().AsNoTracking()
                                    join binding in db.Set<RepairFieldTaskBinding>().AsNoTracking() on work.Id equals binding.ItemId
                                    join field in db.FieldInspectionTasks.AsNoTracking() on binding.TaskId equals field.Id
                                    where work.Id == item && binding.TaskId == task && binding.ProjectId == work.ProjectId &&
                                        binding.ObligationId == work.ObligationId && binding.DefectId == work.DefectId &&
                                        field.ProjectId == work.ProjectId && field.DefectId == work.DefectId && field.RepairItemId == work.Id
                                    select (Guid?)work.ProjectId).SingleOrDefaultAsync(token);
                if (origin is null) Deny(404, "not_found");
                // A closed read-only FIELD action allowlist, exact task binding and
                // current obligation guard replace origin-project membership here only.
                // The ordinary FIELD endpoints and every mutation retain their guards.
                var read = await FieldCore().ApplyInTransactionAsync(new(origin.Value, task, kind,
                    resource is Guid id ? id : null, null, null, new(actor, role, actor, "DIRECT", true)),
                    async ct =>
                    {
                        await GuardAsync(actor, role, project, package, item, false, ct);
                        return true;
                    }, token);
                await tx.CommitAsync(token);
                return new RepairWorkflowResult(read.Status, read.Code, read.Value, read.Version);
            });
        }
        catch (Denied error) { db.ChangeTracker.Clear(); return error.Result; }
        catch (FieldCoreRejectedException error) { db.ChangeTracker.Clear(); return new(error.Result.Status, error.Result.Code); }
    }
    // Called only after GuardAsync in the caller's serializable transaction. This
    // projection is an exact-item read, never a grant on the origin project.
    private async Task<RepairItemSourceFact?> ItemSourceAsync(RepairItem item, Guid responsibility,
        CancellationToken token)
    {
        var defect = await db.Defects.AsNoTracking().SingleOrDefaultAsync(row =>
            row.Id == item.DefectId && row.ProjectId == item.ProjectId, token);
        var obligation = await db.Set<RepairObligation>().AsNoTracking().Include(row => row.Scope)
            .SingleOrDefaultAsync(row => row.Id == item.ObligationId && row.ProjectId == item.ProjectId &&
                row.DefectId == item.DefectId, token);
        if (defect is null || obligation is null) return null;
        var version = await db.Defects.AsNoTracking().Where(row => row.Id == defect.Id)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync(token);
        var task = await (from binding in db.Set<RepairFieldTaskBinding>().AsNoTracking()
                          join field in db.FieldInspectionTasks.AsNoTracking() on binding.TaskId equals field.Id
                          where binding.Id == item.CurrentBindingId && binding.ProjectId == item.ProjectId &&
                              binding.ItemId == item.Id && binding.ObligationId == item.ObligationId && binding.DefectId == item.DefectId &&
                              field.ProjectId == item.ProjectId && field.RepairItemId == item.Id && field.DefectId == item.DefectId &&
                              field.RoadSectionVersionId == defect.RoadSectionVersionId
                          select new RepairTaskBindingFact(item.Id, item.ObligationId, field.Id, binding.AssignmentId,
                              field.TaskMode, binding.AuthorizationId, binding.PolicyRevisionId, field.RoadSectionVersionId,
                              field.SegmentSetId, field.LayoutRevisionId, field.MapPublicationId, field.CrsProfileRevisionId,
                              field.SlabId, obligation.OriginalCrewFirstStartId)).SingleOrDefaultAsync(token);
        return new(item.ProjectId, responsibility, defect.Id, defect.DefectTypeCode,
            defect.Status.ToString().ToUpperInvariant(), Convert.ToBase64String(version),
            defect.RoadSectionVersionId, SafetyScope(obligation.Scope), task);
    }
}
