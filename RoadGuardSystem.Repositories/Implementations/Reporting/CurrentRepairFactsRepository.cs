using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Reporting;

namespace RoadGuardSystem.Repositories.Implementations.Reporting;

public sealed class CurrentRepairFactsRepository(RoadGuardDbContext db, TimeProvider clock,
    IReportingRepository? consistency = null) : ICurrentRepairFactsRepository
{
    public Task<CurrentRepairInventory> CaptureAsync(Guid actor, Guid project, CurrentRepairFactsQuery filters, CancellationToken token)
        => (consistency ?? new ReportingRepository(db)).ReadConsistentlyAsync(ct => CaptureCore(actor, project, filters, ct), token);

    private async Task<CurrentRepairInventory> CaptureCore(Guid actor, Guid project, CurrentRepairFactsQuery filters, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(filters);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var role = await db.Users.AsNoTracking().Where(user => user.Id == actor && user.Status == UserStatus.Active && !user.MustChangePassword &&
            (user.RoleCode == UserRoleCode.ProjectManager || user.RoleCode == UserRoleCode.Supervisor) &&
            db.Roles.Any(value => value.Code == user.RoleCode && value.IsActive)).Select(user => (UserRoleCode?)user.RoleCode).SingleOrDefaultAsync(token);
        if (actor == Guid.Empty || project == Guid.Empty || role is null || !await db.ProjectMembers.AsNoTracking().AnyAsync(member =>
            member.UserId == actor && member.ProjectId == project && member.RoleCode == role && member.Status == ProjectMemberStatus.Active &&
            member.ValidFrom <= today && (member.ValidTo == null || member.ValidTo >= today), token))
            throw new UnauthorizedAccessException("Current project repair reporting authority is required.");
        // Current stock is independent of period allocation. Segment/shared-road allocation requires its own sourced mapping.
        if (filters.SegmentSetId.HasValue || filters.SegmentIds is { Length: > 0 })
            return new(project, [], ["REPAIR_SEGMENT_ALLOCATION_NOT_VERIFIED"]);
        var rows = await (from item in db.Set<RepairItem>().AsNoTracking()
                          join obligation in db.Set<RepairObligation>().AsNoTracking() on item.ObligationId equals obligation.Id
                          join defect in db.Defects.AsNoTracking() on item.DefectId equals defect.Id
                          where (db.Set<ObligationResponsibility>().Where(owner => owner.ObligationId == obligation.Id)
                              .Select(owner => (Guid?)owner.CurrentProjectId).SingleOrDefault() ?? item.ProjectId) == project &&
                              (!filters.RouteVersionId.HasValue || defect.RoadSectionVersionId == filters.RouteVersionId)
                          select new
                          {
                              item.Id,
                              item.ProjectId,
                              item.DefectId,
                              item.ObligationId,
                              item.Mode,
                              item.State,
                              item.EffectiveDecisionId,
                              item.CurrentBindingId,
                              item.CurrentAttemptId,
                              item.CurrentIntakeLinkId,
                              item.EffectiveIntakeSubmissionId,
                              item.CurrentExecutionFinishId,
                              Version = EF.Property<byte[]>(item, "RowVersion"),
                              ObligationVersion = EF.Property<byte[]>(obligation, "RowVersion"),
                              ObligationProject = obligation.ProjectId,
                              ObligationDefect = obligation.DefectId,
                              obligation.CurrentRepairItemId,
                              LegacyHeadItemId = db.Set<RepairDecision>().Where(value => value.Id == obligation.EffectiveResolutionHeadDecisionId)
                                  .Select(value => (Guid?)value.ItemId).SingleOrDefault(),
                              HasAttempt = db.Set<RepairAttempt>().Any(attempt => attempt.ItemId == item.Id)
                          }).ToArrayAsync(token);
        var missing = new HashSet<string>(StringComparer.Ordinal);
        if (await db.Defects.AsNoTracking().AnyAsync(defect => defect.ProjectId == project &&
            (!filters.RouteVersionId.HasValue || defect.RoadSectionVersionId == filters.RouteVersionId) &&
            !db.Set<RepairObligation>().Any(obligation => obligation.ProjectId == project && obligation.DefectId == defect.Id), token))
            missing.Add("REPAIR_OBLIGATION_INVENTORY_NOT_VERIFIED");
        // A current item is an explicit obligation pin. A legacy resolution head can
        // establish its exact item; collection order and active-state guesses cannot.
        rows = rows.GroupBy(row => row.ObligationId).SelectMany(group =>
        {
            var source = group.First(); var pin = source.CurrentRepairItemId ?? source.LegacyHeadItemId;
            if (pin is Guid id)
            {
                var current = group.SingleOrDefault(row => row.Id == id)
                    ?? throw new InvalidOperationException("Current repair item is outside its actual obligation inventory.");
                return new[] { current };
            }
            if (group.Count() == 1) return group.ToArray();
            missing.Add("REPAIR_CURRENT_ITEM_PIN_NOT_VERIFIED"); return [];
        }).ToArray();
        var ids = rows.Where(row => row.EffectiveDecisionId.HasValue).Select(row => row.EffectiveDecisionId!.Value).ToArray();
        // Projection avoids loading private evidence bytes or relying on EF collection order.
        var heads = await db.Set<RepairDecision>().AsNoTracking().Where(decision => ids.Contains(decision.Id))
            .Select(decision => new
            {
                decision.Id,
                decision.ItemId,
                decision.ObligationId,
                decision.DefectId,
                decision.Mode,
                decision.Role,
                decision.ActorId,
                decision.At,
                decision.Result,
                decision.SupersedesDecisionId,
                decision.Reason,
                Basis = decision.Basis == null ? null : decision.Basis.Text,
                Superseded = db.Set<RepairDecision>().Any(child => child.SupersedesDecisionId == decision.Id)
            })
            .ToDictionaryAsync(decision => decision.Id, token);
        var linkIds = rows.Where(row => row.CurrentIntakeLinkId.HasValue).Select(row => row.CurrentIntakeLinkId!.Value).ToArray();
        var attemptSources = await (from link in db.Set<RepairAttemptSubmissionLink>().AsNoTracking()
                                    join attempt in db.Set<RepairAttempt>().AsNoTracking() on link.AttemptId equals attempt.Id
                                    join submission in db.Set<FieldInspectionSubmission>().AsNoTracking() on link.SubmissionId equals submission.Id
                                    join reviewClock in db.Set<DeadlineClock>().AsNoTracking() on link.ReviewClockId equals reviewClock.Id
                                    where linkIds.Contains(link.Id)
                                    select new
                                    {
                                        LinkId = link.Id,
                                        LinkProject = link.ProjectId,
                                        LinkItem = link.ItemId,
                                        LinkBinding = link.BindingId,
                                        link.FormalRootSubmissionId,
                                        link.PreviousLinkId,
                                        link.ExecutionFinishId,
                                        link.SubmissionContentHash,
                                        link.FormalRootServerReceivedAt,
                                        link.OriginalReviewDueAt,
                                        AttemptId = attempt.Id,
                                        AttemptItem = attempt.ItemId,
                                        AttemptProject = attempt.ProjectId,
                                        AttemptDefect = attempt.DefectId,
                                        AttemptObligation = attempt.ObligationId,
                                        attempt.TaskId,
                                        attempt.AssignmentId,
                                        attempt.CrewId,
                                        attempt.OriginId,
                                        attempt.PayloadHash,
                                        attempt.ServerReceivedAt,
                                        attempt.Performed,
                                        attempt.FinishedAt,
                                        SubmissionId = submission.Id,
                                        SubmissionProject = submission.ProjectId,
                                        SubmissionTask = submission.TaskId,
                                        SubmissionAssignment = submission.AssignmentId,
                                        SubmissionActor = submission.OriginalActorId,
                                        SubmissionRoot = submission.RootId,
                                        submission.ParentId,
                                        submission.Revision,
                                        SubmissionOrigin = submission.OriginId,
                                        SubmissionHash = submission.ContentHash,
                                        SubmissionReceived = submission.ServerReceivedAt,
                                        ClockId = reviewClock.Id,
                                        ClockProject = reviewClock.ProjectId,
                                        ClockTarget = reviewClock.TargetId,
                                        ClockOrigin = reviewClock.OriginEventId,
                                        ClockKind = reviewClock.Kind,
                                        ClockOriginAt = reviewClock.OriginAt,
                                        ClockDue = reviewClock.OriginalDueAt
                                    }).ToArrayAsync(token);
        var roots = await db.Set<FieldInspectionSubmission>().AsNoTracking()
            .Where(source => attemptSources.Select(row => row.FormalRootSubmissionId).Contains(source.Id))
            .Select(source => new
            {
                source.Id,
                source.ProjectId,
                source.TaskId,
                source.AssignmentId,
                source.OriginalActorId,
                source.StartOriginId,
                source.RootId,
                source.ParentId,
                source.Revision,
                source.OriginId,
                source.ContentHash,
                source.ServerReceivedAt
            })
            .ToDictionaryAsync(source => source.Id, token);
        var previousIds = attemptSources.Where(source => source.PreviousLinkId.HasValue)
            .Select(source => source.PreviousLinkId!.Value).ToArray();
        var previousLinks = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking()
            .Where(source => previousIds.Contains(source.Id)).ToDictionaryAsync(source => source.Id, token);
        var previousSubmissions = await db.Set<FieldInspectionSubmission>().AsNoTracking()
            .Where(source => previousLinks.Values.Select(link => link.SubmissionId).Contains(source.Id))
            .ToDictionaryAsync(source => source.Id, token);
        var finishIds = attemptSources.Where(source => source.ExecutionFinishId.HasValue)
            .Select(source => source.ExecutionFinishId!.Value).ToArray();
        var finishes = await db.Set<RepairExecutionFinish>().AsNoTracking().Where(finish => finishIds.Contains(finish.Id))
            .Select(finish => new
            {
                finish.Id,
                finish.ProjectId,
                finish.ItemId,
                finish.BindingId,
                finish.OriginalActorId,
                finish.VerifiedOriginalAt
            }).ToDictionaryAsync(finish => finish.Id, token);
        var facts = new List<CurrentRepairInventoryItem>();
        foreach (var row in rows)
        {
            if (row.ProjectId != row.ObligationProject || row.DefectId != row.ObligationDefect || row.Version.Length != 8 || row.ObligationVersion.Length != 8)
                throw new InvalidOperationException("Repair source identity/version does not match its obligation.");
            var presentation = "UNREPAIRED"; string? decisionVersion = null;
            if (row.EffectiveDecisionId is Guid id)
            {
                if (!heads.TryGetValue(id, out var head) || head.Superseded || head.ItemId != row.Id || head.ObligationId != row.ObligationId ||
                    head.DefectId != row.DefectId || head.Mode != row.Mode ||
                    head.Mode == RepairMode.Normal && head.Role != UserRoleCode.Supervisor ||
                    head.Mode == RepairMode.FastTrack && head.Role != UserRoleCode.ProjectManager ||
                    head.Result is not (RepairPresentationState.Unrepaired or RepairPresentationState.ReportedAwaitingReview or RepairPresentationState.Confirmed) ||
                    head.Result == RepairPresentationState.Confirmed && row.State != RepairItemState.Confirmed ||
                    head.Result != RepairPresentationState.Confirmed && row.State != RepairItemState.CorrectionRequired)
                    throw new InvalidOperationException("Repair effective decision is not the exact terminal source head.");
                presentation = head.Result switch { RepairPresentationState.Confirmed => "CONFIRMED", RepairPresentationState.ReportedAwaitingReview => "REPORTED_AWAITING_REVIEW", _ => "UNREPAIRED" };
                decisionVersion = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
                {
                    head.Id,
                    head.ItemId,
                    head.ObligationId,
                    head.DefectId,
                    head.Mode,
                    head.Role,
                    head.ActorId,
                    head.At,
                    head.Result,
                    head.SupersedesDecisionId,
                    head.Reason,
                    head.Basis
                }))).ToLowerInvariant();
            }
            else if (row.HasAttempt)
            {
                var source = attemptSources.SingleOrDefault(candidate => candidate.LinkId == row.CurrentIntakeLinkId);
                var root = source is null ? null : roots.GetValueOrDefault(source.FormalRootSubmissionId);
                var previous = source?.PreviousLinkId is Guid previousId ?
                    previousLinks.GetValueOrDefault(previousId) : null;
                var parent = previous is null ? null : previousSubmissions.GetValueOrDefault(previous.SubmissionId);
                if (row.State is not (RepairItemState.Submitted or RepairItemState.Reviewed) ||
                    source is null || root is null || source.AttemptId != row.CurrentAttemptId || source.LinkProject != row.ProjectId ||
                    source.AttemptProject != row.ProjectId || source.SubmissionProject != row.ProjectId || source.ClockProject != row.ProjectId ||
                    source.LinkItem != row.Id || source.AttemptItem != row.Id || source.AttemptDefect != row.DefectId ||
                    source.AttemptObligation != row.ObligationId || source.LinkBinding != row.CurrentBindingId ||
                    source.SubmissionId != row.EffectiveIntakeSubmissionId || source.SubmissionRoot != root.Id ||
                    root.ProjectId != row.ProjectId || root.TaskId != source.TaskId || root.AssignmentId != source.AssignmentId ||
                    root.OriginalActorId != source.CrewId || root.RootId != root.Id || root.ParentId is not null ||
                    root.Revision != 1 || source.TaskId != source.SubmissionTask ||
                    source.AssignmentId != source.SubmissionAssignment || source.CrewId != source.SubmissionActor ||
                    source.OriginId != root.OriginId || source.PayloadHash != root.ContentHash ||
                    source.SubmissionContentHash != source.SubmissionHash || source.ServerReceivedAt != root.ServerReceivedAt ||
                    source.FormalRootServerReceivedAt != root.ServerReceivedAt ||
                    (source.PreviousLinkId is null && (source.SubmissionId != root.Id || source.Revision != 1 ||
                        source.ParentId is not null) ||
                     source.PreviousLinkId is not null && (previous is null || parent is null ||
                        previous.ItemId != row.Id || previous.AttemptId != source.AttemptId ||
                        previous.BindingId != source.LinkBinding || previous.FormalRootSubmissionId != root.Id ||
                        previous.ReviewClockId != source.ClockId || previous.OriginalReviewDueAt != source.OriginalReviewDueAt ||
                        parent.RootId != root.Id || source.ParentId != parent.Id || source.Revision != parent.Revision + 1 ||
                        source.SubmissionReceived < parent.ServerReceivedAt || source.Revision < 2)) ||
                    source.ClockKind != DeadlineClockKind.ProjectManagerReview || source.ClockTarget != source.TaskId ||
                    source.ClockOrigin != root.Id || source.ClockOriginAt != root.ServerReceivedAt ||
                    source.ClockDue != source.OriginalReviewDueAt ||
                    source.Performed && (source.ExecutionFinishId != row.CurrentExecutionFinishId ||
                        source.ExecutionFinishId is not Guid finishId || !finishes.TryGetValue(finishId, out var finish) ||
                        finish.ProjectId != row.ProjectId || finish.ItemId != row.Id || finish.BindingId != row.CurrentBindingId ||
                        finish.OriginalActorId != source.CrewId || finish.VerifiedOriginalAt != source.FinishedAt) ||
                    !source.Performed && source.ExecutionFinishId is not null)
                {
                    missing.Add("REPAIR_CURRENT_ATTEMPT_PIN_NOT_VERIFIED"); continue;
                }
                presentation = source.Performed ? "REPORTED_AWAITING_REVIEW" : "UNREPAIRED";
            }
            else if (row.State is RepairItemState.Confirmed or RepairItemState.CorrectionRequired)
                throw new InvalidOperationException("Repair effective state is missing its decision head.");
            facts.Add(new(row.Id, project, row.ObligationId, Convert.ToBase64String(row.Version),
                Convert.ToBase64String(row.ObligationVersion), presentation, row.EffectiveDecisionId, decisionVersion));
        }
        return new(project, facts.OrderBy(fact => fact.Id).ToArray(), missing.OrderBy(reason => reason, StringComparer.Ordinal).ToArray());
    }
}
