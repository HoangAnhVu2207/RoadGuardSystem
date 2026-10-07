using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository(RoadGuardDbContext db, IdempotencyOperationService receipts,
    TimeProvider clock) : IRepairWorkflowRepository
{
    private const string Operation = "h4.repair.correct.v1";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class Denied(int status, string code) : Exception
    { public RepairWorkflowResult Result { get; } = new(status, code); }
    private sealed class ExistingReceipt : Exception { }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Deny(int status, string code) => throw new Denied(status, code);

    public async Task<RepairWorkflowResult> CorrectAsync(RepairCorrectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            { schemaVersion = 1, command.ProjectId, command.PackageId, command.ItemId, command.Input, command.ExpectedVersion }, Json))).ToLowerInvariant();
            async Task Guard(CancellationToken token) => _ = await GuardCommandAsync(command, token);
            var outcome = await receipts.ExecuteSerializableAsync(command.ActorId, command.ProjectId, Operation,
                command.Key, fingerprint, async token =>
                {
                    var member = await GuardCommandAsync(command, token);
                    // Another same-key producer may have committed after the service's initial miss,
                    // while this handler waited for current-authority locks. Re-enter guarded receipt lookup.
                    if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == command.ActorId &&
                        row.ProjectId == command.ProjectId && row.Operation == Operation && row.IdempotencyKey == command.Key, token))
                        throw new ExistingReceipt();
                    var item = await db.Set<RepairItem>().Include(row => row.Decisions).Include(row => row.Attempts)
                        .SingleAsync(row => row.Id == command.ItemId, token);
                    var version = Convert.ToBase64String(db.Entry(item).Property<byte[]>("RowVersion").CurrentValue!);
                    if (version != command.ExpectedVersion) Deny(409, "concurrency_conflict");
                    if (item.SupersededByItemId is not null || item.State is not (RepairItemState.Confirmed or RepairItemState.CorrectionRequired) ||
                        item.EffectiveDecisionId != command.Input.SupersedesDecisionId) Deny(409, "decision_head_conflict");
                    var package = await db.Set<RepairPackage>().Include(row => row.Obligations)
                        .ThenInclude(row => row.ResolutionHistory).SingleAsync(row => row.Id == command.PackageId, token);
                    var obligation = package.Obligations.SingleOrDefault(row => row.Id == item.ObligationId);
                    if (obligation is null) Deny(409, "repair_scope_conflict");
                    if (obligation.EffectiveResolutionHeadDecisionId != command.Input.SupersedesDecisionId)
                        Deny(409, "decision_head_conflict");
                    var defect = await db.Defects.FromSqlInterpolated($"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item.DefectId}")
                        .SingleAsync(token);
                    if (defect.ProjectId != item.ProjectId) Deny(409, "repair_scope_conflict");
                    var result = ParseResult(command.Input.Result);
                    var basis = RepairCorrectionBasis.Create(command.Input.Basis.Text,
                        await EvidenceAsync(command, item, token));
                    var now = clock.GetUtcNow();
                    if (now < item.EffectiveDecision!.At) Deny(409, "decision_time_source_conflict");
                    var prior = item.EffectiveDecision;
                    var priorStatus = defect.Status;
                    var authority = new RepairCorrectionAuthority(member, item.Id, command.ActorId, command.Role, command.ProjectId == item.ProjectId ? "CURRENT_PROJECT_MEMBERSHIP" : "CURRENT_RECEIVING_PROJECT_MEMBERSHIP");
                    var decision = RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), prior.Id,
                        command.ActorId, command.Role, command.Input.Reason, now, authority, result, basis);

                    // The historical decision and all its owned evidence must exist before the two head FKs move.
                    // Existing tracked domain mutations are detected only after this append-only stage.
                    var detect = db.ChangeTracker.AutoDetectChangesEnabled;
                    try
                    {
                        db.ChangeTracker.AutoDetectChangesEnabled = false;
                        db.Add(decision);
                        await db.SaveChangesAsync(token);
                    }
                    finally { db.ChangeTracker.AutoDetectChangesEnabled = detect; }
                    db.ChangeTracker.DetectChanges();
                    var revision = db.Entry(package).Property<long>("MutationRevision"); revision.CurrentValue++;
                    var invalidated = obligation.Mandatory && !obligation.IsResolved && priorStatus is DefectStatus.Resolved or DefectStatus.Closed;
                    if (invalidated)
                    {
                        // Legacy Resolved has no established closure provenance. Retain UNKNOWN basis in the audit;
                        // removing Resolved does not assert existence verification or reopen another aggregate.
                        db.Entry(defect).Property(row => row.Status).CurrentValue = DefectStatus.Open;
                    }
                    var before = JsonSerializer.Serialize(new
                    {
                        effectiveDecisionId = prior.Id,
                        result = ResultName(prior.Result),
                        obligationResolved = prior.Accepted,
                        defectStatus = priorStatus.ToString()
                    }, Json);
                    var after = JsonSerializer.Serialize(new
                    {
                        effectiveDecisionId = decision.Id,
                        result = ResultName(decision.Result),
                        obligationResolved = obligation.IsResolved,
                        defectStatus = defect.Status.ToString(),
                        closureBasis = invalidated ? "UNKNOWN_LEGACY_SOURCE" : "UNCHANGED",
                        projectMembershipId = member
                    }, Json);
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, "repair_decision_corrected",
                        "RepairItem", item.Id, before, after, command.Input.Reason, "h4.repair", decision.Id,
                        ["effectiveDecisionId", "result", "obligationResolved", "defectStatus", "closureBasis", "projectMembershipId"]));
                    db.OutboxMessages.Add(OutboxMessage.Create(decision.Id, "repair.decision.corrected.v1", now, null,
                        JsonSerializer.Serialize(new RepairCorrectionOutboxEvent(1, decision.Id, decision.Id,
                            item.ProjectId, "CORRECTED", "RepairWork", item.Id, decision.Id, now,
                            obligation.Id, prior.Id, ResultName(decision.Result)), Json)));
                    await db.SaveChangesAsync(token);
                    await Guard(token);
                    var view = new RepairCorrectionFact(decision.Id, item.Id, obligation.Id, prior.Id,
                        ResultName(decision.Result), decision.Reason, basis.Text, basis.Evidence.Select(row => row.SourceId).ToArray(),
                        decision.ActorId, decision.At, obligation.IsResolved, item.State.ToString(),
                        Convert.ToBase64String(db.Entry(item).Property<byte[]>("RowVersion").CurrentValue!));
                    return (decision.Id, JsonSerializer.Serialize(view, Json));
                }, cancellationToken, receiptAccessGuard: Guard);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var value = JsonSerializer.Deserialize<RepairCorrectionFact>(outcome.OutcomeJson, Json)
                ?? throw new InvalidOperationException("Durable repair correction outcome is missing.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: value,
                Version: value.Version, Replayed: outcome.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await CorrectAsync(command, cancellationToken); }
        catch (Denied denial) { db.ChangeTracker.Clear(); return denial.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }

    private async Task<Guid> GuardCommandAsync(RepairCorrectionCommand command, CancellationToken token)
    {
        var member = await GuardAsync(command.ActorId, command.Role, command.ProjectId,
            command.PackageId, command.ItemId, true, token);
        var receipt = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row =>
            row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId &&
            row.Operation == Operation && row.IdempotencyKey == command.Key, token);
        if (receipt is not null)
        {
            // Protected replay/conflict/recovery is authorized against the persisted producer identity,
            // never against a replacement resource supplied in the new request or its stored JSON alone.
            var source = await (from decision in db.Set<RepairDecision>().AsNoTracking()
                                join item in db.Set<RepairItem>().AsNoTracking() on decision.ItemId equals item.Id
                                where decision.Id == receipt.OperationId &&
                                    decision.ObligationId == item.ObligationId && decision.DefectId == item.DefectId && decision.Mode == item.Mode
                                select new { ItemId = item.Id, PackageId = EF.Property<Guid?>(item, "PackageId") }).SingleOrDefaultAsync(token);
            if (source is null || source.PackageId is null) Deny(403, "stored_receipt_access_forbidden");
            await GuardAsync(command.ActorId, command.Role, command.ProjectId, source.PackageId.Value,
                source.ItemId, true, token);
        }
        return member;
    }

    public async Task<RepairWorkflowResult> ReadItemAsync(Guid actorId, UserRoleCode role, Guid projectId,
        Guid packageId, Guid itemId, bool history, CancellationToken cancellationToken)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                await GuardAsync(actorId, role, projectId, packageId, itemId, false, cancellationToken);
                var item = await db.Set<RepairItem>().AsNoTracking().Include(row => row.Decisions)
                    .Include(row => row.Attempts).Include(row => row.ReviewRequests).SingleAsync(row => row.Id == itemId, cancellationToken);
                var version = await db.Set<RepairItem>().Where(row => row.Id == itemId)
                    .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync(cancellationToken);
                var view = new RepairItemFact(item.Id, item.ProjectId, item.DefectId, item.ObligationId, item.Mode.ToString(),
                    item.State.ToString(), ResultName(item.Presentation), item.EffectiveDecisionId,
                    item.Attempts.Select(row => row.Id).Order().ToArray(), item.Decisions.Select(row => row.Id).Order().ToArray(),
                    Convert.ToBase64String(version));
                object value = history ? HistoryView(item, view) : view;
                // History includes immutable child appends that intentionally do not mutate the command item head.
                // Its finite canonical representation has a separate token, never accepted as a command rowversion.
                var representationVersion = history ? "h4-history-v1-" + Convert.ToHexString(SHA256.HashData(
                    JsonSerializer.SerializeToUtf8Bytes(value, Json))).ToLowerInvariant() : view.Version;
                await transaction.CommitAsync(cancellationToken); return new RepairWorkflowResult(200, Value: value, Version: representationVersion);
            });
        }
        catch (Denied denial) { db.ChangeTracker.Clear(); return denial.Result; }
    }

    private static RepairHistoryFact HistoryView(RepairItem item, RepairItemFact view) => new(view,
        item.Decisions.OrderBy(row => row.At).ThenBy(row => row.Id).Select(row => new RepairDecisionHistoryFact(
            row.Id, row.ItemId, row.ObligationId, row.DefectId,
            row.Mode switch { RepairMode.Normal => "Normal", RepairMode.FastTrack => "FastTrack", _ => "UNKNOWN" },
            row.ActorId, HistoryRole(row.Role), row.Reason, row.At, row.SupersedesDecisionId, ResultName(row.Result),
            row.Basis is null ? null : new RepairCorrectionBasisHistoryFact(row.Basis.Text, row.Basis.Evidence.Select(HistoryEvidence).ToArray()))).ToArray(),
        item.Attempts.OrderBy(row => row.ServerReceivedAt).ThenBy(row => row.Id).Select(row => new RepairAttemptHistoryFact(
            row.Id, row.OriginId, row.PayloadHash, row.ItemId, row.ObligationId, row.ProjectId, row.DefectId, row.CrewId,
            row.TaskId, row.AssignmentId, row.AuthorizationId, row.LocationVersion, row.PolicyRevisionId, row.Performed,
            row.UnperformedReason, row.StartedAt, row.FinishedAt, row.ServerReceivedAt,
            row.TimeProvenance switch { RepairTimeProvenance.VerifiedOnline => "VERIFIED_ONLINE", RepairTimeProvenance.VerifiedOffline => "VERIFIED_OFFLINE", RepairTimeProvenance.Uncertain => "UNCERTAIN", _ => "UNKNOWN" },
            row.Evidence.Select(HistoryEvidence).ToArray())).ToArray(),
        item.ReviewRequests.OrderBy(row => row.At).ThenBy(row => row.Id).Select(row => new RepairReviewRequestHistoryFact(
            row.Id, row.ItemId, row.DecisionId, row.ActorId, HistoryRole(row.Role), row.Reason, row.At)).ToArray());

    private static string HistoryRole(UserRoleCode role) => role switch
    { UserRoleCode.ProjectManager => "ProjectManager", UserRoleCode.Supervisor => "Supervisor", UserRoleCode.RepairCrew => "RepairCrew", _ => "UNKNOWN" };
    private static RepairEvidenceHistoryFact HistoryEvidence(RepairEvidenceReference row) => new(row.FileId,
        row.FileVersion, row.Hash, row.Purpose switch { RepairEvidencePurpose.Before => "BEFORE", RepairEvidencePurpose.After => "AFTER", RepairEvidencePurpose.Safety => "SAFETY", _ => "UNKNOWN" },
        row.Verified, row.SourceKind, row.SourceId, row.CapturedAt, row.ReuseDecisionId, row.ReusedSource);

    private async Task<Guid> GuardAsync(Guid actor, UserRoleCode role, Guid project, Guid package, Guid item,
        bool correction, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) Deny(403, "access_forbidden");
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token)) Deny(403, "access_forbidden");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var member = await db.ProjectMembers.AsNoTracking().Where(row => row.ProjectId == project && row.UserId == actor &&
            row.RoleCode == role && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= today &&
            (row.ValidTo == null || row.ValidTo >= today)).OrderBy(row => row.Id).Select(row => (Guid?)row.Id).FirstOrDefaultAsync(token);
        if (member is null) Deny(403, "access_forbidden");
        var scope = await db.Set<RepairItem>().FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item}")
            .AsNoTracking().Select(row => new { row.ProjectId, row.ObligationId, row.Mode, PackageId = EF.Property<Guid?>(row, "PackageId") }).SingleOrDefaultAsync(token);
        if (scope is null || scope.PackageId != package ||
            !await db.Set<RepairPackage>().AnyAsync(row => row.Id == package && row.ProjectId == scope.ProjectId, token)) Deny(404, "not_found");
        if (await ObligationResponsibilityScope.ResolveAsync(db, scope.ObligationId, scope.ProjectId, token) != project) Deny(404, "not_found");
        if (correction && role != (scope.Mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager)) Deny(403, "access_forbidden");
        // No ProjectActive or DefectOpen predicate: current correction authority is independent of the invalidated close basis.
        return member.Value;
    }

    private async Task<IReadOnlyList<RepairEvidenceReference>> EvidenceAsync(RepairCorrectionCommand command, RepairItem item, CancellationToken token)
    {
        var result = new List<RepairEvidenceReference>();
        foreach (var id in command.Input.Basis.EvidenceReferenceIds.Order())
        {
            var link = await db.Set<FieldInspectionEvidenceLink>().FromSqlInterpolated($"SELECT * FROM [FieldInspectionEvidenceLinks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}")
                .AsNoTracking().SingleOrDefaultAsync(token);
            var fileId = link?.FileId ?? Guid.Empty;
            if (link is null || link.ProjectId != item.ProjectId || fileId == Guid.Empty ||
                !await db.FieldInspectionTasks.AnyAsync(row => row.Id == link.TaskId && row.ProjectId == item.ProjectId && row.DefectId == item.DefectId, token))
                Deny(403, "correction_evidence_access_forbidden");
            await db.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={link.FileId}").AsNoTracking().ToListAsync(token);
            await db.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={link.FileId}").AsNoTracking().ToListAsync(token);
            var file = await new AnhHuyFactsRepository(db).GetFileAsync(fileId, token);
            var scope = await db.FileScopes.AsNoTracking().SingleOrDefaultAsync(row => row.FileId == fileId, token);
            if (file is null || scope is null || file.State != "VERIFIED" || file.Checksum != link.DeclaredChecksum ||
                scope.ProjectId != item.ProjectId || scope.TargetId != link.TaskId || scope.Purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT"))
                Deny(409, "correction_evidence_source_not_ready");
            result.Add(new(fileId, file.FileVersion, file.Checksum,
                link.Purpose == "AFTER" ? RepairEvidencePurpose.After : RepairEvidencePurpose.Before,
                true, true, "FIELD", link.Id, null, null, false));
        }
        return result;
    }

    private static RepairPresentationState ParseResult(string value) => value switch
    {
        "UNREPAIRED" => RepairPresentationState.Unrepaired,
        "REPORTED_AWAITING_REVIEW" => RepairPresentationState.ReportedAwaitingReview,
        "CONFIRMED" => RepairPresentationState.Confirmed,
        _ => throw new ArgumentException("Unknown repair correction result.", nameof(value))
    };
    private static string ResultName(RepairPresentationState value) => value switch
    {
        RepairPresentationState.Unrepaired => "UNREPAIRED",
        RepairPresentationState.ReportedAwaitingReview => "REPORTED_AWAITING_REVIEW",
        RepairPresentationState.Confirmed => "CONFIRMED",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}
