using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairWorkflowRepository
{
    private const string ReviewRequestOperation = "h4.repair.request-review.v1";
    public async Task<RepairWorkflowResult> RequestReviewAsync(RepairReviewRequestCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            { schemaVersion = 1, command.ProjectId, command.PackageId, command.ItemId, command.Input, command.ExpectedVersion }, Json))).ToLowerInvariant();
            async Task Guard(CancellationToken token) => await GuardReviewRequestAsync(command, token);
            var outcome = await receipts.ExecuteSerializableAsync(command.ActorId, command.ProjectId, ReviewRequestOperation,
                command.Key, fingerprint, async token =>
                {
                    await Guard(token);
                    if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == command.ActorId &&
                        row.ProjectId == command.ProjectId && row.Operation == ReviewRequestOperation && row.IdempotencyKey == command.Key, token))
                        throw new ExistingReceipt();
                    var item = await db.Set<RepairItem>().Include(row => row.Decisions).Include(row => row.ReviewRequests)
                        .SingleAsync(row => row.Id == command.ItemId, token);
                    var version = Convert.ToBase64String(db.Entry(item).Property<byte[]>("RowVersion").CurrentValue!);
                    if (version != command.ExpectedVersion) Deny(409, "concurrency_conflict");
                    if (item.EffectiveDecisionId is null || item.EffectiveDecision is null ||
                        item.State is not (RepairItemState.Confirmed or RepairItemState.CorrectionRequired))
                        Deny(409, "decision_head_conflict");
                    var now = clock.GetUtcNow();
                    if (now < item.EffectiveDecision.At) Deny(409, "decision_time_source_conflict");
                    var request = item.RequestReview(Guid.NewGuid(), command.ActorId, command.Role, command.Input.Reason, now);
                    db.Add(request);
                    var state = JsonSerializer.Serialize(new { effectiveDecisionId = item.EffectiveDecisionId,
                        result = ResultName(item.EffectiveDecision.Result), itemState = item.State.ToString() }, Json);
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, "repair_review_requested",
                        "RepairItem", item.Id, state, state, request.Reason, "h4.repair", request.Id,
                        ["effectiveDecisionId", "result", "itemState"]));
                    await db.SaveChangesAsync(token);
                    await Guard(token);
                    var view = new RepairReviewRequestFact(request.Id, item.Id, request.DecisionId, request.ActorId,
                        request.Role.ToString(), request.Reason, request.At, version);
                    return (request.Id, JsonSerializer.Serialize(view, Json));
                }, cancellationToken, receiptAccessGuard: Guard);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            var value = JsonSerializer.Deserialize<RepairReviewRequestFact>(outcome.OutcomeJson, Json)
                ?? throw new InvalidOperationException("Durable repair review request outcome is missing.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value: value,
                Version: value.Version, Replayed: outcome.Status == IdempotencyOperationStatus.Replayed);
        }
        catch (ExistingReceipt) { db.ChangeTracker.Clear(); return await RequestReviewAsync(command, cancellationToken); }
        catch (Denied denial) { db.ChangeTracker.Clear(); return denial.Result; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }

    private async Task GuardReviewRequestAsync(RepairReviewRequestCommand command, CancellationToken token)
    {
        await GuardReviewRequestResourceAsync(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId, token);
        var receipt = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row => row.ActorUserId == command.ActorId &&
            row.ProjectId == command.ProjectId && row.Operation == ReviewRequestOperation && row.IdempotencyKey == command.Key, token);
        if (receipt is null) return;
        var source = await (from request in db.Set<RepairReviewRequest>().AsNoTracking()
            join item in db.Set<RepairItem>().AsNoTracking() on request.ItemId equals item.Id
            join decision in db.Set<RepairDecision>().AsNoTracking() on request.DecisionId equals decision.Id
            where request.Id == receipt.OperationId && request.ActorId == command.ActorId && request.ProjectId == command.ProjectId &&
                item.ProjectId == request.ProjectId && decision.ItemId == item.Id && decision.ObligationId == item.ObligationId &&
                decision.DefectId == item.DefectId && decision.Mode == item.Mode
            select new { ItemId = item.Id, PackageId = EF.Property<Guid?>(item, "PackageId") }).SingleOrDefaultAsync(token);
        if (source is null || source.PackageId is null) Deny(403, "stored_receipt_access_forbidden");
        await GuardReviewRequestResourceAsync(command.ActorId, command.Role, command.ProjectId, source.PackageId.Value, source.ItemId, token);
    }

    private async Task GuardReviewRequestResourceAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.RepairCrew)) Deny(403, "access_forbidden");
        await Anh02ReceiptAuthority.LockAsync(db, actor, project, token);
        if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(actor, role, token)) Deny(403, "access_forbidden");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (!await db.ProjectMembers.AsNoTracking().AnyAsync(row => row.ProjectId == project && row.UserId == actor &&
            row.RoleCode == role && row.Status == ProjectMemberStatus.Active && row.ValidFrom <= today &&
            (row.ValidTo == null || row.ValidTo >= today), token)) Deny(403, "access_forbidden");
        var scope = await db.Set<RepairItem>().FromSqlInterpolated($"SELECT * FROM [RepairItems] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={item}")
            .AsNoTracking().Select(row => new { row.ProjectId, row.CrewId, PackageId = EF.Property<Guid?>(row, "PackageId") }).SingleOrDefaultAsync(token);
        if (scope is null || scope.ProjectId != project || scope.PackageId != package ||
            !await db.Set<RepairPackage>().AnyAsync(row => row.Id == package && row.ProjectId == project, token)) Deny(404, "not_found");
        if (role == UserRoleCode.RepairCrew && scope.CrewId != actor) Deny(403, "access_forbidden");
    }
}
