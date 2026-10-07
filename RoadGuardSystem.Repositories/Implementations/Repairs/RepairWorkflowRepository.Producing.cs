using RoadGuardSystem.Repositories.Messaging;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository : IRepairProducerRepository
{
    private sealed record ProducingOutcome<T>(T Value, string Version);
    private const string PackageCreateOperation = "h4.repair.package-create.v1";
    private const string ProposeOperation = "h4.repair.propose.v1";
    private const string ApproveOperation = "h4.repair.approve.v1";
    private const string AssignOperation = "h4.repair.assign.v1";
    private const string AttemptSubmitOperation = "h4.repair.attempt-submit.v1";
    private const string AttemptSupplementOperation = "h4.repair.attempt-supplement.v1";
    private const string AttemptReviewOperation = "h4.repair.attempt-review.v1";
    private const string FinalConfirmOperation = "h4.repair.final-confirm.v1";

    public Task<RepairWorkflowResult> ConfirmFinalAsync(RepairFinalConfirmCommand command,
        CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, command.Role, command.ProjectId, command.Key,
            FinalConfirmOperation, command, ct => GuardFinalResource(command.ActorId, command.ProjectId, command.PackageId,
                command.ItemId, command.Role, ct), async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.Input.Reason) || command.Input.Reason.Length > 2000)
                    Deny(400, "validation_error");
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId);
                var obligation = package.Obligations.Single(row => row.Id == item.ObligationId);
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                if (item.State != RepairItemState.Reviewed || item.CurrentReviewId is null ||
                    item.CurrentAttemptId is null || item.CurrentIntakeLinkId is null ||
                    obligation.CurrentRepairItemId != item.Id || obligation.IsResolved)
                    Deny(409, "invalid_state_transition");
                var review = await db.Set<RepairAttemptReview>().AsNoTracking().SingleOrDefaultAsync(row =>
                    row.Id == item.CurrentReviewId && row.ProjectId == command.ProjectId && row.ItemId == item.Id &&
                    row.AttemptId == item.CurrentAttemptId && row.IntakeLinkId == item.CurrentIntakeLinkId &&
                    row.SubmissionId == item.EffectiveIntakeSubmissionId && row.Decision == "ACCEPT" &&
                    row.EvidenceSufficient && row.ExecutionAuthority == RepairFactState.Confirmed &&
                    row.Role == UserRoleCode.ProjectManager, ct);
                if (review is null) Deny(409, "accepted_review_source_conflict");
                DeadlineClock? supervisorClock = null;
                if (item.Mode == RepairMode.Normal)
                {
                    supervisorClock = await db.Set<DeadlineClock>().SingleOrDefaultAsync(row =>
                        row.ProjectId == command.ProjectId && row.TargetId == item.Id &&
                        row.Kind == DeadlineClockKind.SupervisorFinalConfirmation && row.OriginEventId == review.Id &&
                        row.OriginAt == review.At, ct);
                    if (supervisorClock is null) Deny(409, "supervisor_final_clock_source_conflict");
                }
                var now = clock.GetUtcNow();
                RepairDecision decision;
                try
                {
                    decision = item.Confirm(Guid.NewGuid(), command.ActorId, command.Role,
                    command.Input.Reason, now, obligation.EffectiveResolutionHeadDecisionId); obligation.Resolve(decision);
                }
                catch (InvalidOperationException) { Deny(409, "final_confirmation_source_conflict"); throw; }
                supervisorClock?.Complete(now);
                Touch(package);
                AuditProducer(command.ActorId, "repair_final_confirmed", "RepairItem", item.Id,
                    command.Input.Reason, new
                    {
                        decision.Id,
                        reviewId = review.Id,
                        obligationId = obligation.Id,
                        mode = item.Mode.ToString(),
                        supervisorClockId = supervisorClock?.Id
                    });
                await db.SaveChangesAsync(ct);
                return (decision.Id, new ProducingOutcome<RepairItemFact>(ItemView(item, VersionOf(item)),
                    VersionOf(item)));
            }, cancellationToken);

    public Task<RepairWorkflowResult> ReviewAttemptAsync(RepairAttemptReviewCommand command,
        CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.ProjectManager, command.ProjectId, command.Key,
            AttemptReviewOperation, command, ct => GuardReviewResource(command.ActorId, command.ProjectId, command.PackageId,
                command.ItemId, command.TaskId, ct), async ct =>
            {
                if (command.Input.Decision is not ("SUPPLEMENT" or "ACCEPT") || string.IsNullOrWhiteSpace(command.Input.Reason) ||
                    command.Input.Reason.Length > 2000) Deny(400, "validation_error");
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId);
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                if (item.State != RepairItemState.Submitted || item.CurrentReviewId is not null ||
                    item.CurrentAttemptId is null || item.CurrentIntakeLinkId is null ||
                    item.EffectiveIntakeSubmissionId != command.Input.FieldSubmissionId)
                    Deny(409, "invalid_state_transition");
                var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentBindingId && row.TaskId == command.TaskId, ct);
                var link = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentIntakeLinkId && row.ItemId == item.Id && row.BindingId == binding.Id &&
                    row.AttemptId == item.CurrentAttemptId && row.SubmissionId == command.Input.FieldSubmissionId, ct);
                var submission = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(row =>
                    row.Id == link.SubmissionId && row.ProjectId == command.ProjectId && row.TaskId == binding.TaskId &&
                    row.AssignmentId == binding.AssignmentId, ct);
                if (submission.ContentHash != link.SubmissionContentHash ||
                    await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.RootId == link.FormalRootSubmissionId &&
                        row.Revision > submission.Revision, ct)) Deny(409, "submission_lineage_conflict");
                var native = await db.FieldInspectionTasks.FromSqlInterpolated(
                    $"SELECT * FROM [FieldInspectionTasks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={binding.TaskId}")
                    .SingleAsync(ct);
                if (native.Status != FieldInspectionTaskStatus.Submitted || native.RepairItemId != item.Id)
                    Deny(409, "formal_intake_source_conflict");
                var now = clock.GetUtcNow();
                var evidence = new List<RepairEvidenceReference>();
                if (command.Input.Decision == "ACCEPT")
                {
                    var reasons = JsonSerializer.Deserialize<string[]>(submission.MissingReasonsJson, Json);
                    var payload = JsonSerializer.Deserialize<RepairFieldSubmissionData>(submission.PayloadJson, Json);
                    var attempt = await db.Set<RepairAttempt>().AsNoTracking().SingleAsync(row =>
                        row.Id == link.AttemptId && row.ItemId == item.Id, ct);
                    var finish = await db.Set<RepairExecutionFinish>().AsNoTracking().SingleOrDefaultAsync(row =>
                        row.Id == link.ExecutionFinishId && row.ItemId == item.Id && row.BindingId == binding.Id, ct);
                    if (reasons is null || reasons.Length != 1 || reasons[0] != "AFTER_ATTEMPT_BINDING_UNAVAILABLE" ||
                        payload is null || payload.CaptureType != "REPAIR_CLAIM" || payload.Repaired != true ||
                        attempt.Performed != true || attempt.FinishedAt is null || finish is null ||
                        finish.Id != item.CurrentExecutionFinishId || finish.VerifiedOriginalAt != attempt.FinishedAt ||
                        finish.OriginalActorId != attempt.CrewId || finish.TimeProvenance == RepairTimeProvenance.Uncertain)
                        Deny(409, "repair_review_source_insufficient");
                    var links = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking()
                        .Where(row => row.SubmissionId == submission.Id).ToArrayAsync(ct);
                    if (payload.Evidence is null || links.Length != payload.Evidence.Length ||
                        links.Select(row => row.CaptureOriginId).Distinct().Count() != links.Length)
                        Deny(409, "repair_review_evidence_source_conflict");
                    foreach (var declared in payload.Evidence)
                    {
                        var sourceLink = links.SingleOrDefault(row => row.CaptureOriginId == declared.CaptureOriginId);
                        if (sourceLink is null || sourceLink.ProjectId != command.ProjectId ||
                            sourceLink.TaskId != binding.TaskId || sourceLink.AssignmentId != binding.AssignmentId ||
                            sourceLink.FileId is null || sourceLink.FileId != declared.FileId ||
                            sourceLink.Purpose != declared.Purpose ||
                            !string.Equals(sourceLink.DeclaredChecksum, declared.ChecksumSha256,
                                StringComparison.OrdinalIgnoreCase))
                            Deny(409, "repair_review_evidence_source_conflict");
                        ReporterFileFacts file;
                        try
                        {
                            file = await FieldCore().ValidateRepairEvidenceInTransactionAsync(binding.TaskId,
                            declared, attempt.CrewId, ct);
                        }
                        catch (RepairFieldCoreRejectedException rejection)
                        { throw new Denied(rejection.Status, rejection.Code ?? "repair_review_evidence_not_ready"); }
                        using var captureFacts = JsonDocument.Parse(sourceLink.CaptureFactsJson);
                        var capturedVersion = captureFacts.RootElement.TryGetProperty("fileVersion", out var captured)
                            ? captured.GetString() : null;
                        if (file.FileId != sourceLink.FileId || file.Checksum != sourceLink.DeclaredChecksum ||
                            file.MediaType != sourceLink.MediaType || capturedVersion != file.FileVersion ||
                            declared.Purpose == "AFTER" && (declared.AttemptChecklist != "POST_REPAIR_CAPTURE" ||
                                declared.CapturedAt is null || declared.CapturedAt < finish.VerifiedOriginalAt ||
                                declared.CapturedAt > now || file.UploadedAt < finish.VerifiedOriginalAt))
                            Deny(409, "repair_review_evidence_source_conflict");
                        if (declared.Purpose is "BEFORE" or "AFTER")
                        {
                            var reuse = declared.Purpose == "BEFORE" ? await db.Set<FieldInspectionEvidenceReuseDecision>()
                                .AsNoTracking().Where(row => row.TaskId == binding.TaskId && row.FileId == file.FileId &&
                                    row.FileChecksum == file.Checksum).OrderByDescending(row => row.OccurredAt)
                                .Select(row => (Guid?)row.Id).FirstOrDefaultAsync(ct) : null;
                            evidence.Add(new(file.FileId, file.FileVersion, file.Checksum,
                                declared.Purpose == "AFTER" ? RepairEvidencePurpose.After : RepairEvidencePurpose.Before,
                                true, true, "FIELD", sourceLink.Id, declared.CapturedAt, reuse, reuse is not null));
                        }
                    }
                    if (!evidence.Any(row => row.Purpose == RepairEvidencePurpose.After))
                        Deny(409, "repair_review_after_evidence_required");
                    await db.Entry(item).Collection(row => row.Attempts).LoadAsync(ct);
                    try { item.Review(command.ActorId, command.Role, now, evidence); }
                    catch (InvalidOperationException) { Deny(409, "repair_review_source_insufficient"); throw; }
                }
                var source = FieldInspectionReview.Create(Guid.NewGuid(), command.ProjectId, binding.TaskId,
                    submission.Id, command.ActorId,
                    command.Input.Decision == "ACCEPT" ? "CONFIRM" : "SUPPLEMENT", command.Input.Reason, now);
                var review = new RepairAttemptReview(Guid.NewGuid(), command.ProjectId, item.Id, binding.Id,
                    link.AttemptId, link.Id, submission.Id, submission.ContentHash, binding.PlanHash,
                    binding.ChecklistVersion, command.ActorId, command.Role, command.Input.Decision, command.Input.Reason,
                    now, command.Input.Decision == "ACCEPT",
                    command.Input.Decision == "ACCEPT" ? RepairFactState.Confirmed : RepairFactState.Unknown,
                    JsonSerializer.Serialize(new
                    {
                        fieldReviewId = source.Id,
                        source.ReceiptActivation,
                        evidence = evidence.Select(row => new
                        {
                            row.FileId,
                            row.FileVersion,
                            row.Hash,
                            row.Purpose,
                            row.SourceId,
                            row.CapturedAt
                        }),
                        finishId = link.ExecutionFinishId
                    }, Json));
                db.AddRange(source, review);
                native.Transition(command.Input.Decision == "ACCEPT" ? FieldInspectionTaskStatus.Completed :
                    FieldInspectionTaskStatus.SupplementRequired);
                if (command.Input.Decision == "SUPPLEMENT")
                {
                    BusinessRequestProducer.Supplement(db, command.ProjectId, "RepairReview", review.Id,
                        binding.TaskId, binding.CrewId, now);
                    var rework = new RepairItemLifecycleEvent(Guid.NewGuid(), command.ProjectId,
                        item.DefectId, item.ObligationId, item.Id, item.Mode, "REWORK", command.ActorId,
                        command.Role, command.Input.Reason, now, binding.Id, link.AttemptId,
                        submission.Id, review.Id, null, command.ExpectedItemVersion);
                    db.Add(rework);
                    Notify(rework, "repair.work.rework_requested.v1", "REWORK");
                }
                if (command.Input.Decision == "ACCEPT")
                {
                    var reviewClock = await db.Set<DeadlineClock>().SingleAsync(row => row.Id == link.ReviewClockId &&
                        row.ProjectId == command.ProjectId && row.TargetId == binding.TaskId &&
                        row.OriginEventId == link.FormalRootSubmissionId, ct);
                    reviewClock.Complete(now);
                    if (item.Mode == RepairMode.Normal)
                    {
                        db.Add(DeadlineClock.Create(Guid.NewGuid(), command.ProjectId,
                            DeadlineClockKind.SupervisorFinalConfirmation, item.Id, review.Id, now));
                        var handoff = new RepairItemLifecycleEvent(Guid.NewGuid(), command.ProjectId,
                            item.DefectId, item.ObligationId, item.Id, item.Mode, "PM_REVIEWED", command.ActorId,
                            command.Role, command.Input.Reason, now, binding.Id, link.AttemptId,
                            submission.Id, review.Id, null, command.ExpectedItemVersion);
                        db.Add(handoff);
                        Notify(handoff, "review.supervisor_required.v1", "SUPERVISOR_REQUIRED");
                    }
                }
                db.Entry(item).Property(row => row.CurrentReviewId).CurrentValue = review.Id;
                Touch(package);
                AuditProducer(command.ActorId, command.Input.Decision == "ACCEPT" ? "repair_attempt_reviewed" :
                    "repair_supplement_requested", "RepairItem", item.Id,
                    command.Input.Reason, new
                    {
                        reviewId = review.Id,
                        fieldReviewId = source.Id,
                        submissionId = submission.Id,
                        link.ReviewClockId
                    });
                await db.SaveChangesAsync(ct);
                return (review.Id, new ProducingOutcome<RepairAttemptReviewFact>(
                    new(review.Id, item.Id, submission.Id, command.Input.Decision, source.ReceiptActivation), VersionOf(item)));
            }, cancellationToken);

    public Task<RepairWorkflowResult> SupplementAttemptAsync(RepairAttemptSupplementCommand command,
        CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.RepairCrew, command.ProjectId, command.Key,
            AttemptSupplementOperation, command, ct => GuardExecutionResource(command.ActorId, command.ProjectId,
                command.PackageId, command.ItemId, command.TaskId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId);
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                if (item.State != RepairItemState.Submitted || item.CurrentAttemptId is null ||
                    item.CurrentIntakeLinkId is null || item.CurrentReviewId is null)
                    Deny(409, "invalid_state_transition");
                var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentBindingId && row.TaskId == command.TaskId, ct);
                var previous = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentIntakeLinkId && row.AttemptId == item.CurrentAttemptId &&
                    row.ItemId == item.Id && row.BindingId == binding.Id, ct);
                var review = await db.Set<RepairAttemptReview>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentReviewId && row.IntakeLinkId == previous.Id &&
                    row.Decision == "SUPPLEMENT" && row.ItemId == item.Id, ct);
                var prior = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(row =>
                    row.Id == previous.SubmissionId && row.ProjectId == command.ProjectId, ct);
                var submission = await db.Set<FieldInspectionSubmission>().FromSqlInterpolated(
                    $"SELECT * FROM [FieldInspectionSubmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={command.Input.FieldSubmissionId}")
                    .AsNoTracking().SingleOrDefaultAsync(ct);
                if (submission is null || submission.ProjectId != command.ProjectId ||
                    submission.TaskId != binding.TaskId || submission.AssignmentId != binding.AssignmentId ||
                    submission.OriginalActorId != command.ActorId || submission.RootId != previous.FormalRootSubmissionId ||
                    submission.ParentId != previous.SubmissionId || submission.Revision != prior.Revision + 1 ||
                    submission.StartOriginId != prior.StartOriginId || submission.ContentHash != command.Input.ExpectedContentHash ||
                    submission.ServerReceivedAt < prior.ServerReceivedAt ||
                    await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.RootId == submission.RootId &&
                        row.Revision > submission.Revision, ct)) Deny(409, "submission_lineage_conflict");
                var native = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == binding.TaskId, ct);
                if (native.Status != FieldInspectionTaskStatus.Submitted || native.RepairItemId != item.Id)
                    Deny(409, "formal_intake_source_conflict");
                var attempt = await db.Set<RepairAttempt>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentAttemptId && row.ItemId == item.Id && row.CrewId == command.ActorId, ct);
                var root = await db.Set<FieldInspectionSubmission>().AsNoTracking().SingleAsync(row =>
                    row.Id == previous.FormalRootSubmissionId, ct);
                var lineage = await db.Set<FieldInspectionSubmission>().AsNoTracking()
                    .Where(row => row.RootId == root.Id && row.Revision > 1 && row.Revision <= submission.Revision)
                    .OrderBy(row => row.Revision).ToArrayAsync(ct);
                if (lineage.Length != submission.Revision - 1) Deny(409, "submission_lineage_conflict");
                RepairAttemptIntake intake;
                try
                {
                    intake = RepairAttemptIntake.Create(attempt, root);
                    foreach (var revision in lineage) intake.Supplement(revision);
                }
                catch (InvalidOperationException) { Deny(409, "physical_claim_source_conflict"); throw; }
                if (intake.EffectiveSubmission.Id != submission.Id || review.SubmissionId != prior.Id)
                    Deny(409, "submission_lineage_conflict");
                var link = new RepairAttemptSubmissionLink(Guid.NewGuid(), command.ProjectId, item.Id, binding.Id,
                    attempt.Id, submission.Id, root.Id, previous.Id, previous.ExecutionFinishId,
                    submission.ContentHash, previous.FormalRootServerReceivedAt, previous.ReviewClockId,
                    previous.OriginalReviewDueAt);
                db.Add(link);
                db.Entry(item).Property(row => row.CurrentIntakeLinkId).CurrentValue = link.Id;
                db.Entry(item).Property(row => row.EffectiveIntakeSubmissionId).CurrentValue = submission.Id;
                db.Entry(item).Property(row => row.CurrentReviewId).CurrentValue = null;
                Touch(package);
                AuditProducer(command.ActorId, "repair_attempt_supplement", "RepairItem", item.Id,
                    "Actual immutable H3 supplement; original physical attempt and PM clock retained",
                    new
                    {
                        linkId = link.Id,
                        previousLinkId = previous.Id,
                        submissionId = submission.Id,
                        rootId = root.Id,
                        reviewId = review.Id
                    });
                await db.SaveChangesAsync(ct);
                return (link.Id, new ProducingOutcome<RepairAttemptIntakeFact>(new(attempt.Id, link.Id, item.Id,
                    submission.Id, previous.ReviewClockId, previous.OriginalReviewDueAt, submission.Readiness),
                    VersionOf(item)));
            }, cancellationToken);

    public Task<RepairWorkflowResult> SubmitAttemptAsync(RepairAttemptSubmitCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.RepairCrew, command.ProjectId, command.Key,
            AttemptSubmitOperation, command, ct => GuardExecutionResource(command.ActorId, command.ProjectId,
                command.PackageId, command.ItemId, command.TaskId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct);
                await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId);
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                if (item.State != RepairItemState.InProgress || item.CurrentAttemptId is not null ||
                    item.CurrentIntakeLinkId is not null) Deny(409, "invalid_state_transition");
                var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row =>
                    row.Id == item.CurrentBindingId && row.TaskId == command.TaskId, ct);
                var submission = await db.Set<FieldInspectionSubmission>().FromSqlInterpolated(
                    $"SELECT * FROM [FieldInspectionSubmissions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={command.Input.FieldSubmissionId} AND [ProjectId]={command.ProjectId}")
                    .AsNoTracking().SingleOrDefaultAsync(ct);
                if (submission is null || submission.TaskId != binding.TaskId || submission.AssignmentId != binding.AssignmentId ||
                    submission.OriginalActorId != command.ActorId || submission.RootId != submission.Id ||
                    submission.ParentId is not null || submission.Revision != 1 ||
                    submission.ContentHash != command.Input.ExpectedContentHash ||
                    await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.RootId == submission.Id && row.Revision > 1, ct))
                    Deny(409, "formal_intake_source_conflict");
                var native = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(row => row.Id == binding.TaskId, ct);
                if (native.Status != FieldInspectionTaskStatus.Submitted || native.RepairItemId != item.Id ||
                    native.Purpose != FieldInspectionPurpose.PostRepair) Deny(409, "formal_intake_source_conflict");
                var capture = JsonSerializer.Deserialize<RepairFieldSubmissionData>(submission.PayloadJson, Json);
                if (capture is null || capture.CaptureType != "REPAIR_CLAIM" || capture.Repaired is null ||
                    capture.StartOriginId != submission.StartOriginId || capture.OriginId != submission.OriginId)
                    Deny(409, "formal_intake_source_conflict");
                var physicalStart = await db.Set<RepairExecutionStart>().AsNoTracking().SingleOrDefaultAsync(row =>
                    row.Id == item.CurrentExecutionStartId && row.ItemId == item.Id && row.BindingId == binding.Id &&
                    row.OriginalActorId == command.ActorId && row.FirstStartId == submission.StartOriginId, ct);
                if (physicalStart is null ||
                    (physicalStart.VerifiedOriginalAt is DateTimeOffset verifiedStart
                        ? verifiedStart != item.StartedAt
                        : physicalStart.TimeProvenance != RepairTimeProvenance.Uncertain || physicalStart.ServerReceivedAt != item.StartedAt))
                    Deny(409, "execution_start_source_conflict");
                RepairExecutionFinish? finish = null;
                if (capture.Repaired == true)
                {
                    finish = await db.Set<RepairExecutionFinish>().AsNoTracking().SingleOrDefaultAsync(row =>
                        row.Id == command.Input.ExecutionFinishId && row.ItemId == item.Id && row.BindingId == binding.Id &&
                        row.ExecutionStartId == physicalStart.Id && row.OriginalActorId == command.ActorId, ct);
                    if (finish is null || item.CurrentExecutionFinishId != finish.Id ||
                        (finish.VerifiedOriginalAt is DateTimeOffset verifiedFinish
                            ? verifiedFinish > submission.ServerReceivedAt
                            : finish.TimeProvenance != RepairTimeProvenance.Uncertain || finish.ServerReceivedAt > submission.ServerReceivedAt))
                        Deny(409, "execution_finish_source_conflict");
                }
                else if (command.Input.ExecutionFinishId is not null) Deny(409, "execution_finish_source_conflict");
                var reviewClock = await db.Set<DeadlineClock>().FromSqlInterpolated(
                    $"SELECT * FROM [DeadlineClocks] WITH (UPDLOCK,HOLDLOCK) WHERE [TargetId]={binding.TaskId} AND [Kind]={(byte)DeadlineClockKind.ProjectManagerReview}")
                    .AsNoTracking().SingleOrDefaultAsync(row => row.ProjectId == command.ProjectId &&
                        row.OriginEventId == submission.Id && row.OriginAt == submission.ServerReceivedAt, ct);
                if (reviewClock is null) Deny(409, "pm_review_clock_source_conflict");
                var attempt = RepairAttempt.Submit(Guid.NewGuid(), submission.OriginId, submission.ContentHash,
                    item.Id, item.ObligationId, command.ProjectId, item.DefectId, command.ActorId, binding.TaskId,
                    binding.AssignmentId, binding.AuthorizationId, binding.LocationVersion, binding.PolicyRevisionId,
                    capture.Repaired == true, capture.Repaired == true ? null : capture.UnrepairedReason,
                    physicalStart.VerifiedOriginalAt, finish?.VerifiedOriginalAt, submission.ServerReceivedAt,
                    finish?.TimeProvenance ?? RepairTimeProvenance.Uncertain, []);
                var link = new RepairAttemptSubmissionLink(Guid.NewGuid(), command.ProjectId, item.Id, binding.Id,
                    attempt.Id, submission.Id, submission.Id, null, finish?.Id, submission.ContentHash,
                    submission.ServerReceivedAt, reviewClock.Id, reviewClock.OriginalDueAt);
                item.Submit(attempt);
                db.Add(link); db.Entry(item).Property(row => row.CurrentAttemptId).CurrentValue = attempt.Id;
                db.Entry(item).Property(row => row.CurrentIntakeLinkId).CurrentValue = link.Id;
                db.Entry(item).Property(row => row.EffectiveIntakeSubmissionId).CurrentValue = submission.Id;
                var source = new RepairItemLifecycleEvent(Guid.NewGuid(), command.ProjectId, item.DefectId,
                    item.ObligationId, item.Id, item.Mode, "SUBMITTED", command.ActorId, command.Role,
                    "Formal H3 root intake", submission.ServerReceivedAt, binding.Id, attempt.Id,
                    submission.Id, null, null, command.ExpectedItemVersion);
                db.Add(source);
                Notify(source, "repair.work.submitted.v1", "SUBMITTED");
                Touch(package);
                AuditProducer(command.ActorId, "repair_attempt_intake", "RepairItem", item.Id,
                    "Actual H3 root formal intake; incomplete evidence does not confirm repair",
                    new
                    {
                        attemptId = attempt.Id,
                        linkId = link.Id,
                        submissionId = submission.Id,
                        submission.ContentHash,
                        reviewClockId = reviewClock.Id
                    });
                await db.SaveChangesAsync(ct);
                var view = new RepairAttemptIntakeFact(attempt.Id, link.Id, item.Id, submission.Id,
                    reviewClock.Id, reviewClock.OriginalDueAt, submission.Readiness);
                return (link.Id, new ProducingOutcome<RepairAttemptIntakeFact>(view, VersionOf(item)));
            }, cancellationToken);

    public Task<RepairWorkflowResult> CreatePackageAsync(RepairPackageCreateCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.ProjectManager, command.ProjectId, command.Key, PackageCreateOperation, command,
            ct => GuardDefectResource(command.ProjectId, command.Input.DefectId, ct), async ct =>
            {
                var defect = await FreshAnchor(command.ProjectId, command.Input.DefectId, ct);
                if (VersionOf(defect) != command.ExpectedDefectVersion || command.Input.DefectVersion != command.ExpectedDefectVersion)
                    Deny(409, "concurrency_conflict");
                if (command.Input.Obligations is null || command.Input.Obligations.Length is < 1 or > 100) Deny(400, "validation_error");
                var obligations = new List<RepairObligation>();
                foreach (var input in command.Input.Obligations)
                {
                    var physical = input.Scope;
                    if (physical is null || physical.RouteVersionId != defect.RoadSectionVersionId) Deny(409, "geometry_version_mismatch");
                    var route = await db.RoadSectionVersions.AsNoTracking().SingleAsync(row => row.Id == physical.RouteVersionId, ct);
                    var road = await db.RoadSections.AsNoTracking().SingleAsync(row => row.Id == route.RoadSectionId, ct);
                    if (road.ProjectId != command.ProjectId || physical.PhysicalRoadId != road.Id) Deny(409, "physical_road_source_mismatch");
                    var locationInput = new RepairFieldTaskData(defect.Id, command.ExpectedDefectVersion, null, "REPORTER", route.Id,
                        physical.SegmentSetId, physical.LayoutRevisionId, physical.SlabId, "POST_REPAIR", 1, "{}", null,
                        command.ActorId, clock.GetUtcNow().AddDays(1));
                    var native = await db.Set<NativeRouteVersionFacts>().AsNoTracking().SingleOrDefaultAsync(row => row.RoadSectionVersionId == route.Id, ct);
                    locationInput = locationInput with { CrsProfileRevisionId = native?.CrsProfileRevisionId };
                    var check = await FieldCore().ValidateRepairLocationPinsInTransactionAsync(command.ProjectId, locationInput, ct);
                    if (check.Code is not null) Deny(check.Status, check.Code);
                    var kind = input.Kind switch
                    {
                        "FORMAL_REPAIR" => RepairObligationKind.FormalRepair,
                        "TEMPORARY_SAFETY" => RepairObligationKind.TemporarySafety,
                        _ => (RepairObligationKind)0
                    };
                    if (kind == 0) Deny(400, "validation_error");
                    var frame = "h4-frame-v1:" + SourceHash(new
                    {
                        routeVersionId = route.Id,
                        physical.SegmentSetId,
                        physical.LayoutRevisionId,
                        physical.SlabId,
                        crsProfileRevisionId = native?.CrsProfileRevisionId
                    });
                    RepairActualScope scope;
                    try
                    {
                        scope = RepairActualScope.Create(Guid.NewGuid(), road.Id, frame, road.Code,
                        physical.FromMeters, physical.ToMeters, physical.OffsetFromMeters, physical.OffsetToMeters);
                    }
                    catch (ArgumentException) { Deny(400, "validation_error"); throw; }
                    var existing = await db.Set<RepairObligation>().AsNoTracking().Include(row => row.Scope)
                        .Where(row => row.DefectId == defect.Id && row.Kind == kind && row.EffectiveResolutionDecisionId == null).ToArrayAsync(ct);
                    if (existing.Any(row => scope.Compare(row.Scope) != RepairScopeComparison.Disjoint) ||
                        obligations.Where(row => row.Kind == kind).Any(row => scope.Compare(row.Scope) != RepairScopeComparison.Disjoint))
                        Deny(409, "active_scope_conflict");
                    obligations.Add(RepairObligation.Create(Guid.NewGuid(), command.ProjectId, defect.Id, kind, input.Mandatory, scope));
                }
                var package = RepairPackage.Create(Guid.NewGuid(), command.ProjectId, defect.Id, obligations);
                db.Add(package); db.Entry(package).Property(row => row.DefectStatusAtAnchor).CurrentValue = defect.Status;
                AuditProducer(command.ActorId, "repair_package_created", "RepairPackage", package.Id, command.Input.Reason,
                    new { package.ProjectId, package.DefectId, sourceStatus = defect.Status.ToString(), obligations = obligations.Select(row => row.Id) });
                await db.SaveChangesAsync(ct); var version = VersionOf(package);
                return (package.Id, new ProducingOutcome<RepairPackageFact>(PackageView(package, version), version));
            }, cancellationToken);

    public Task<RepairWorkflowResult> ProposeItemAsync(RepairItemProposeCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.ProjectManager, command.ProjectId, command.Key, ProposeOperation, command,
            ct => GuardPackageResource(command.ProjectId, command.PackageId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct); await FreshAnchor(command.ProjectId, package.DefectId, ct);
                if (VersionOf(package) != command.ExpectedPackageVersion) Deny(409, "concurrency_conflict");
                var obligation = package.Obligations.SingleOrDefault(row => row.Id == command.Input.ObligationId);
                if (obligation is null) Deny(404, "not_found");
                if (obligation.IsResolved || obligation.CurrentRepairItemId is not null ||
                    package.Items.Any(row => row.ObligationId == obligation.Id)) Deny(409, "explicit_successor_required");
                var mode = command.Input.Mode switch { "NORMAL" => RepairMode.Normal, "FAST_TRACK" => RepairMode.FastTrack, _ => (RepairMode)0 };
                if (mode == 0 || obligation.Kind != RepairObligationKind.FormalRepair) Deny(400, "validation_error");
                var now = clock.GetUtcNow(); var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, mode,
                    command.ActorId, command.Role, now, new(command.Input.RepairPlan, command.Input.ChecklistVersion));
                package.AddItem(item); Touch(package); await db.SaveChangesAsync(ct);
                db.Entry(obligation).Property(row => row.CurrentRepairItemId).CurrentValue = item.Id;
                var source = Lifecycle(item, command.ActorId, command.Role, "PROPOSED", command.Input.Reason, command.ExpectedPackageVersion, now);
                if (mode == RepairMode.Normal)
                {
                    db.Set<DeadlineClock>().Add(DeadlineClock.Create(Guid.NewGuid(), command.ProjectId,
                        DeadlineClockKind.SupervisorInitialApproval, item.Id, source.Id, now));
                    Notify(source, "review.supervisor_required.v1", "SUPERVISOR_REQUIRED");
                }
                AuditProducer(command.ActorId, "repair_item_proposed", "RepairItem", item.Id, command.Input.Reason,
                    new { item.ObligationId, mode = item.Mode.ToString(), item.ProposalPlanHash, item.ChecklistVersion });
                await db.SaveChangesAsync(ct); var version = VersionOf(item);
                return (item.Id, new ProducingOutcome<RepairItemFact>(ItemView(item, version), version));
            }, cancellationToken);

    public Task<RepairWorkflowResult> ApproveItemAsync(RepairItemApprovalCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.Supervisor, command.ProjectId, command.Key, ApproveOperation, command,
            ct => GuardItemResource(command.ProjectId, command.PackageId, command.ItemId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct); await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId); var obligation = package.Obligations.Single(row => row.Id == item.ObligationId);
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                if (item.Mode != RepairMode.Normal || item.State != RepairItemState.AwaitingApproval ||
                    obligation.CurrentRepairItemId != item.Id) Deny(409, "invalid_state_transition");
                var now = clock.GetUtcNow(); item.Approve(command.ActorId, command.Role, now); Touch(package);
                var source = Lifecycle(item, command.ActorId, command.Role, "APPROVED", command.Input.Reason, command.ExpectedItemVersion, now);
                var deadline = await db.Set<DeadlineClock>().SingleOrDefaultAsync(row => row.TargetId == item.Id &&
                    row.Kind == DeadlineClockKind.SupervisorInitialApproval, ct);
                deadline?.Complete(now);
                AuditProducer(command.ActorId, "repair_item_approved", "RepairItem", item.Id, command.Input.Reason,
                    new { item.ApprovedPlanHash, item.ApprovedBy, item.ApprovedAt });
                await db.SaveChangesAsync(ct); var version = VersionOf(item);
                return (source.Id, new ProducingOutcome<RepairItemFact>(ItemView(item, version), version));
            }, cancellationToken);

    public Task<RepairWorkflowResult> AssignItemAsync(RepairItemAssignCommand command, CancellationToken cancellationToken)
        => ProduceAsync(command.ActorId, command.Role, UserRoleCode.ProjectManager, command.ProjectId, command.Key, AssignOperation, command,
            ct => GuardItemResource(command.ProjectId, command.PackageId, command.ItemId, ct), async ct =>
            {
                var package = await LockedPackage(command.PackageId, ct); var defect = await FreshAnchor(command.ProjectId, package.DefectId, ct);
                var item = package.Items.Single(row => row.Id == command.ItemId); var obligation = package.Obligations.Single(row => row.Id == item.ObligationId);
                if (VersionOf(item) != command.ExpectedItemVersion) Deny(409, "concurrency_conflict");
                var input = command.Input.Task;
                if (item.State != RepairItemState.ReadyToAssign || item.CurrentBindingId is not null || obligation.CurrentRepairItemId != item.Id)
                    Deny(409, "invalid_state_transition");
                if (input.DefectId != defect.Id || input.RouteVersionId != defect.RoadSectionVersionId ||
                    input.DefectVersion != VersionOf(defect)) Deny(409, "repair_source_conflict");
                if (input.Purpose != "POST_REPAIR" || input.RequiredMeasurementType is < 1 or > 4) Deny(400, "validation_error");
                var check = await FieldCore().ValidateRepairTaskSourcesInTransactionAsync(command.ProjectId, input, defect, ct);
                if (check.Code is not null) Deny(check.Status, check.Code);
                var declaredFrame = "h4-frame-v1:" + SourceHash(new
                {
                    routeVersionId = input.RouteVersionId,
                    input.SegmentSetId,
                    input.LayoutRevisionId,
                    input.SlabId,
                    crsProfileRevisionId = input.CrsProfileRevisionId
                });
                if (declaredFrame != obligation.Scope.LocationVersion) Deny(409, "repair_scope_frame_mismatch");
                RepairPolicyRevision? policy = null;
                if (item.Mode == RepairMode.FastTrack)
                {
                    if (obligation.OriginalCrewFirstStartId is not null) Deny(409, "execution_regrant_policy_pending");
                    policy = await db.Set<RepairPolicyRevision>().Include(row => row.Measurements).Include(row => row.Revocations)
                        .SingleOrDefaultAsync(row => row.Id == command.Input.PolicyRevisionId && row.ProjectId == command.ProjectId, ct);
                    if (policy is null || policy.IsRevoked || policy.DefectTypeCode != defect.DefectTypeCode ||
                        policy.ChecklistVersion != item.ChecklistVersion || !await db.Set<RepairPolicyDraft>().AnyAsync(row =>
                            row.ProjectId == command.ProjectId && row.PublishedRevisionId == policy.Id && row.CurrentChangeId != null, ct))
                        Deny(409, "published_policy_source_required");
                }
                else if (command.Input.PolicyRevisionId is not null) Deny(400, "validation_error");
                var now = clock.GetUtcNow(); item.Assign(input.AssignedToUserId, command.ActorId, command.Role, now);
                // The existing-item UPDATE is not an EF FK dependency for a new native task INSERT.
                // Persist the actual assignment state first so the native source guard observes it;
                // all stages remain inside the same guarded serializable receipt transaction.
                Touch(package); await db.SaveChangesAsync(ct);
                var task = FieldInspectionTask.CreateRepair(Guid.NewGuid(), "REPAIR-" + Guid.NewGuid().ToString("N"), item,
                    input.SurveyId, input.SourceKind, input.RouteVersionId, input.SegmentSetId, input.LayoutRevisionId, input.SlabId,
                    FieldInspectionPurpose.PostRepair, input.RequiredMeasurementType, input.MeasurementScope, input.Instructions,
                    input.DueAt, command.ActorId, input.MapPublicationId, input.CrsProfileRevisionId);
                var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, input.AssignedToUserId,
                    command.ActorId, now, null, FieldInspectionAssignmentStatus.Active, null);
                db.AddRange(task, assignment); await db.SaveChangesAsync(ct);
                RepairExecutionAuthorization? authorization = null;
                if (policy is not null)
                {
                    authorization = RepairExecutionAuthorization.Issue(Guid.NewGuid(), command.ProjectId, defect.Id, task.Id,
                        assignment.Id, input.AssignedToUserId, command.ActorId, now, command.Input.Reason, RepairTaskMode.ConditionalFastTrack,
                        obligation.Scope.LocationVersion, policy.Id);
                    db.Add(authorization); await db.SaveChangesAsync(ct);
                }
                var policyHash = policy is null ? null : SourceHash(new
                {
                    policy.Id,
                    policy.ProjectId,
                    policy.Revision,
                    policy.DefectTypeCode,
                    policy.ChecklistVersion,
                    policy.PublishedAt,
                    policy.PublishedBy,
                    measurements = policy.Measurements.OrderBy(row => row.Code, StringComparer.Ordinal).ToArray(),
                    stopConditions = policy.StopConditions.Order(StringComparer.Ordinal).ToArray()
                });
                var binding = new RepairFieldTaskBinding(Guid.NewGuid(), command.ProjectId, defect.Id, obligation.Id, item.Id,
                    task.Id, assignment.Id, input.AssignedToUserId, item.Mode, authorization?.Id, policy?.Id, policyHash,
                    item.ProposalPlanHash!, item.ChecklistVersion!, input.RouteVersionId, input.SegmentSetId, input.LayoutRevisionId,
                    input.SlabId, input.MapPublicationId, input.CrsProfileRevisionId, obligation.Scope.LocationVersion,
                    command.ActorId, now, command.Input.Reason, Convert.ToBase64String(task.RowVersion), SourceHash(new
                    {
                        assignment.Id,
                        assignment.FieldInspectionTaskId,
                        assignment.AssignedToUserId,
                        assignment.AssignedByUserId,
                        assignment.AssignedAt
                    }));
                db.Add(binding); await db.SaveChangesAsync(ct);
                db.Entry(item).Property(row => row.CurrentBindingId).CurrentValue = binding.Id;
                var source = Lifecycle(item, command.ActorId, command.Role, "ASSIGNED", command.Input.Reason, command.ExpectedItemVersion, now, binding.Id);
                Notify(source, "repair.work.assigned.v1", "ASSIGNED");
                AuditProducer(command.ActorId, "repair_item_assigned", "RepairItem", item.Id, command.Input.Reason,
                    new
                    {
                        binding.Id,
                        binding.TaskId,
                        binding.AssignmentId,
                        binding.CrewId,
                        binding.AuthorizationId,
                        binding.PolicyRevisionId,
                        binding.PlanHash,
                        binding.ChecklistVersion,
                        binding.LocationVersion
                    });
                await db.SaveChangesAsync(ct); var version = VersionOf(item);
                var view = new RepairTaskBindingFact(item.Id, obligation.Id, task.Id, assignment.Id, task.TaskMode,
                    authorization?.Id, policy?.Id, task.RoadSectionVersionId, task.SegmentSetId, task.LayoutRevisionId,
                    task.MapPublicationId, task.CrsProfileRevisionId, task.SlabId, obligation.OriginalCrewFirstStartId);
                return (binding.Id, new ProducingOutcome<RepairTaskBindingFact>(view, version));
            }, cancellationToken);

    private FieldInspectionWorkflowRepository FieldCore() => new(db, receipts, clock);
    private string VersionOf<T>(T value) where T : class => Convert.ToBase64String(db.Entry(value).Property<byte[]>("RowVersion").CurrentValue!);
    private static string SourceHash<T>(T source) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(source, Json))).ToLowerInvariant();
    private void Touch(RepairPackage package) => db.Entry(package).Property<long>("MutationRevision").CurrentValue++;
    private static RepairItemFact ItemView(RepairItem item, string version) => new(item.Id, item.ProjectId, item.DefectId,
        item.ObligationId, item.Mode.ToString(), item.State.ToString(), ResultName(item.Presentation), item.EffectiveDecisionId,
        item.Attempts.Select(row => row.Id).Order().ToArray(), item.Decisions.Select(row => row.Id).Order().ToArray(), version);
    private static RepairPackageFact PackageView(RepairPackage package, string version) => new(package.Id, package.ProjectId,
        package.DefectId, package.IsComplete, package.Obligations.Where(row => row.Mandatory && !row.IsResolved).Select(row => row.Id).Order().ToArray(),
        package.Items.OrderBy(row => row.Id).Select(row => ItemView(row, "")).ToArray(), version);

    private RepairItemLifecycleEvent Lifecycle(RepairItem item, Guid actor, UserRoleCode role, string kind, string reason,
        string sourceVersion, DateTimeOffset at, Guid? binding = null)
    {
        var source = new RepairItemLifecycleEvent(Guid.NewGuid(), item.ProjectId, item.DefectId, item.ObligationId, item.Id,
            item.Mode, kind, actor, role, reason, at, binding, null, null, null, null, sourceVersion);
        db.Add(source); return source;
    }
    private void Notify(RepairItemLifecycleEvent source, string type, string kind)
    {
        db.OutboxMessages.Add(OutboxMessage.Create(source.Id, type, source.At, null, JsonSerializer.Serialize(
            new H6StoredEvent(1, source.Id, kind, source.ProjectId, "RepairWork", source.ItemId,
                source.Id, source.At, source.Id), Json)));
    }
    private void AuditProducer(Guid actor, string action, string entity, Guid id, string reason, object source)
        => db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, clock.GetUtcNow(), action, entity, id, null,
            JsonSerializer.Serialize(source, Json), reason, "h4.repair", id, ["source"]));

    private async Task<RepairPackage> LockedPackage(Guid id, CancellationToken token)
        => await db.Set<RepairPackage>().FromSqlInterpolated($"SELECT * FROM [RepairPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}")
            .Include(row => row.Obligations).Include(row => row.Items).SingleAsync(token);
    private async Task<Defect> FreshAnchor(Guid project, Guid id, CancellationToken token)
    {
        var defect = await db.Defects.FromSqlInterpolated($"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleAsync(token);
        if (defect.ProjectId != project || defect.RoadSectionVersionId is null ||
            defect.Status is not (DefectStatus.Open or DefectStatus.Verified)) Deny(409, "repair_anchor_not_ready");
        if (!await db.Projects.AnyAsync(row => row.Id == project && row.Status == ProjectStatus.Active, token)) Deny(409, "project_not_active");
        return defect;
    }
    private async Task CurrentProducerAuthority(Guid actor, UserRoleCode role, UserRoleCode required, Guid project, CancellationToken token)
    {
        if (role != required) Deny(403, "access_forbidden");
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token)) Deny(403, "access_forbidden");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await db.ProjectMembers.AnyAsync(row => row.ProjectId == project && row.UserId == actor && row.RoleCode == role &&
            row.Status == ProjectMemberStatus.Active && row.ValidFrom <= today && (row.ValidTo == null || row.ValidTo >= today), token))
            Deny(403, "access_forbidden");
    }
    private async Task GuardDefectResource(Guid project, Guid defect, CancellationToken token)
    {
        var found = await db.Defects.FromSqlInterpolated($"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={defect}")
            .AsNoTracking().AnyAsync(row => row.ProjectId == project, token);
        if (!found) Deny(404, "not_found");
    }
    private async Task GuardPackageResource(Guid project, Guid package, CancellationToken token)
    {
        var source = await db.Set<RepairPackage>().FromSqlInterpolated($"SELECT * FROM [RepairPackages] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={package}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (source is null || source.ProjectId != project) Deny(404, "not_found");
        await GuardDefectResource(project, source.DefectId, token);
    }
    private async Task GuardItemResource(Guid project, Guid package, Guid item, CancellationToken token)
    {
        await GuardPackageResource(project, package, token);
        if (!await db.Set<RepairItem>().FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item}")
            .AsNoTracking().AnyAsync(row => row.ProjectId == project && EF.Property<Guid?>(row, "PackageId") == package, token)) Deny(404, "not_found");
    }
    private async Task GuardReviewResource(Guid actor, Guid project, Guid package, Guid item, Guid task, CancellationToken token)
    {
        await GuardItemResource(project, package, item, token);
        await GuardDutyAssignee(actor, project, task, DeadlineClockKind.ProjectManagerReview, token);
        var bindingId = await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == item)
            .Select(row => row.CurrentBindingId).SingleAsync(token);
        if (bindingId is null || !await db.Set<RepairFieldTaskBinding>().AsNoTracking().AnyAsync(row =>
            row.Id == bindingId && row.ProjectId == project && row.ItemId == item && row.TaskId == task, token))
            Deny(403, "repair_binding_not_current");
    }
    private async Task GuardFinalResource(Guid actor, Guid project, Guid package, Guid item, UserRoleCode role, CancellationToken token)
    {
        await GuardItemResource(project, package, item, token);
        await GuardDutyAssignee(actor, project, item, DeadlineClockKind.SupervisorFinalConfirmation, token);
        var mode = await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == item)
            .Select(row => row.Mode).SingleAsync(token);
        if (role != (mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager))
            Deny(403, "access_forbidden");
    }

    private async Task GuardDutyAssignee(Guid actor, Guid project, Guid target, DeadlineClockKind kind, CancellationToken token)
    {
        if (await db.Set<DeadlineClock>().AnyAsync(x => x.ProjectId == project && x.TargetId == target && x.Kind == kind &&
            x.AppointedActorId != null && x.AppointedActorId != actor, token)) Deny(403, "current_review_assignee_required");
    }

    private async Task GuardStoredProduction(Guid actor, Guid project, string operation, Guid operationId, CancellationToken token)
    {
        if (operation == PackageCreateOperation) { await GuardPackageResource(project, operationId, token); return; }
        Guid? item = operation switch
        {
            ProposeOperation => operationId,
            ApproveOperation => await db.Set<RepairItemLifecycleEvent>().Where(row => row.Id == operationId && row.ProjectId == project &&
                row.Kind == "APPROVED" && row.Mode == RepairMode.Normal).Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            AssignOperation => await db.Set<RepairFieldTaskBinding>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            AttemptSubmitOperation => await db.Set<RepairAttemptSubmissionLink>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            AttemptSupplementOperation => await db.Set<RepairAttemptSubmissionLink>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            AttemptReviewOperation => await db.Set<RepairAttemptReview>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            FinalConfirmOperation => await db.Set<RepairDecision>().Where(row => row.Id == operationId)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            AssessmentOperation => await db.Set<RepairMeasurementAssessment>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            ExecutionStartOperation => await db.Set<RepairExecutionStart>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            ExecutionFinishOperation => await db.Set<RepairExecutionFinish>().Where(row => row.Id == operationId && row.ProjectId == project)
                .Select(row => (Guid?)row.ItemId).SingleOrDefaultAsync(token),
            _ => null
        };
        if (item is null) Deny(403, "stored_receipt_access_forbidden");
        var package = await db.Set<RepairItem>().Where(row => row.Id == item && row.ProjectId == project)
            .Select(row => EF.Property<Guid?>(row, "PackageId")).SingleOrDefaultAsync(token);
        if (package is null) Deny(403, "stored_receipt_access_forbidden");
        await GuardItemResource(project, package.Value, item.Value, token);
        if (operation == FinalConfirmOperation)
        {
            var mode = await db.Set<RepairItem>().AsNoTracking().Where(row => row.Id == item)
                .Select(row => row.Mode).SingleAsync(token);
            await GuardFinalResource(actor, project, package.Value, item.Value,
                mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager, token);
        }
        if (operation == AttemptReviewOperation)
        {
            var review = await db.Set<RepairAttemptReview>().AsNoTracking().SingleAsync(row => row.Id == operationId, token);
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == review.BindingId, token);
            await GuardReviewResource(actor, project, package.Value, item.Value, binding.TaskId, token);
        }
        if (operation is AttemptSubmitOperation or AttemptSupplementOperation)
        {
            var link = await db.Set<RepairAttemptSubmissionLink>().AsNoTracking().SingleAsync(row => row.Id == operationId, token);
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == link.BindingId, token);
            await GuardExecutionResource(actor, project, package.Value, item.Value, binding.TaskId, token);
        }
        else if (operation == AssessmentOperation)
        {
            var assessment = await db.Set<RepairMeasurementAssessment>().AsNoTracking().SingleAsync(row => row.Id == operationId, token);
            await GuardExecutionResource(actor, project, package.Value, item.Value, assessment.TaskId, token);
        }
        else if (operation == ExecutionStartOperation)
        {
            var start = await db.Set<RepairExecutionStart>().AsNoTracking().SingleAsync(row => row.Id == operationId, token);
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == start.BindingId, token);
            await GuardExecutionResource(actor, project, package.Value, item.Value, binding.TaskId, token);
        }
        else if (operation == ExecutionFinishOperation)
        {
            var finish = await db.Set<RepairExecutionFinish>().AsNoTracking().SingleAsync(row => row.Id == operationId, token);
            var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.Id == finish.BindingId, token);
            await GuardExecutionResource(actor, project, package.Value, item.Value, binding.TaskId, token);
        }
    }
    private async Task<RepairWorkflowResult> ProduceAsync<T>(Guid actor, UserRoleCode role, UserRoleCode required, Guid project,
        string key, string operation, object command, Func<CancellationToken, Task> target,
        Func<CancellationToken, Task<(Guid Id, ProducingOutcome<T> Outcome)>> apply, CancellationToken token)
    {
        try
        {
            if (offlineRepairContext is not null)
                return await ApplyOfflineProductionAsync(actor, role, required, project, operation, target, apply, token);
            var hash = SourceHash(new { schemaVersion = 1, command });
            async Task Guard(CancellationToken ct)
            {
                await CurrentProducerAuthority(actor, role, required, project, ct); await target(ct);
                if (operation == ApproveOperation && command is RepairItemApprovalCommand approval)
                    await GuardDutyAssignee(actor, project, approval.ItemId, DeadlineClockKind.SupervisorInitialApproval, ct);
                var stored = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row => row.ActorUserId == actor &&
                    row.ProjectId == project && row.Operation == operation && row.IdempotencyKey == key, ct);
                if (stored is not null) await GuardStoredProduction(actor, project, operation, stored.OperationId, ct);
            }
            var result = await receipts.ExecuteSerializableAsync(actor, project, operation, key, hash, async ct =>
            {
                await Guard(ct);
                if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == actor && row.ProjectId == project &&
                    row.Operation == operation && row.IdempotencyKey == key, ct)) throw new ExistingReceipt();
                var effect = await apply(ct); await Guard(ct);
                return (effect.Id, JsonSerializer.Serialize(effect.Outcome, Json));
            }, token, receiptAccessGuard: Guard);
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var outcome = JsonSerializer.Deserialize<ProducingOutcome<T>>(result.OutcomeJson, Json)
                ?? throw new InvalidOperationException("Durable repair producing outcome is missing.");
            return new(result.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: outcome.Value,
                Version: outcome.Version, Replayed: result.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await ProduceAsync(actor, role, required, project, key, operation, command, target, apply, token); }
        catch (Denied denial) { db.ChangeTracker.Clear(); return denial.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }
}
