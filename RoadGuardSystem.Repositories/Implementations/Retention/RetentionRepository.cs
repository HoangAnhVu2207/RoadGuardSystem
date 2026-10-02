using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.Repositories.Retention;

public sealed class RetentionRepository(RoadGuardDbContext context, IdempotencyOperationService idempotency,
    IRetentionInventoryRepository inventory, TimeProvider clock) : IRetentionRepository
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private Guid? _principalActor;
    private UserRoleCode? _principalRole;
    public async Task AuthorizePrincipalAsync(Guid actor, UserRoleCode role, CancellationToken token)
    {
        if (!await context.Users.AsNoTracking().AnyAsync(x => x.Id == actor && x.RoleCode == role && x.Status == UserStatus.Active && !x.MustChangePassword, token)) Reject(403, "access_forbidden");
        _principalActor = actor; _principalRole = role;
    }
    public async Task AuthorizeAsync(Guid actor, Guid? project, bool supervisorOnly, CancellationToken token)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor, token);
        if (user is null || user.Status != UserStatus.Active || user.MustChangePassword || (_principalActor == actor && _principalRole != user.RoleCode) || (supervisorOnly && user.RoleCode != UserRoleCode.Supervisor)) Reject(403, "access_forbidden");
        if (user!.RoleCode == UserRoleCode.Supervisor)
        {
            if (project.HasValue && !await context.Projects.AsNoTracking().AnyAsync(x => x.Id == project, token)) Reject(404, "not_found");
            return;
        }
        if (user.RoleCode != UserRoleCode.ProjectManager || project is null) Reject(403, "access_forbidden");
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var membership = await new RoadGuardSystem.Repositories.Projects.ProjectMembershipReadModel(context).FindByUserAndProjectAsync(actor, project!.Value, token);
        if (membership is null || membership.RoleCode != user.RoleCode || membership.Status != ProjectMemberStatus.Active || membership.ValidFrom > today || membership.ValidTo < today) Reject(403, "access_forbidden");
    }
    private async Task<bool> SupervisorAsync(Guid actor, CancellationToken token)
    {
        if (_principalActor == actor && _principalRole != UserRoleCode.Supervisor) return false;
        return await context.Users.AsNoTracking().AnyAsync(x => x.Id == actor && x.RoleCode == UserRoleCode.Supervisor && x.Status == UserStatus.Active && !x.MustChangePassword, token);
    }
    private async Task<RetentionInventory> ScopedInventoryAsync(Guid actor, Guid project, Guid file, CancellationToken token)
    {
        var current = await inventory.ReadAsync(file, token);
        if (current is null || !current.References.Any(x => x.ProjectId == project)) Reject(404, "not_found");
        if (!await SupervisorAsync(actor, token) && !current!.PublicProjectIds.Contains(project)) Reject(404, "not_found");
        return current!;
    }
    public async Task<RetentionFileView> GetFileAsync(Guid actor, Guid project, Guid file, CancellationToken token)
    {
        await AuthorizeAsync(actor, project, false, token);
        var current = await ScopedInventoryAsync(actor, project, file, token);
        var control = await CaptureAsync(current, clock.GetUtcNow(), token);
        var supervisor = await SupervisorAsync(actor, token);
        var reasons = current.ReasonCodes.ToList();
        if (!supervisor && current.References.Any(x => x.ProjectId != null && x.ProjectId != project)) reasons.Add("OTHER_SCOPE_OBLIGATIONS");
        return new(file, control.Head?.RevisionId, control.Item.BasisVersion, current.Version, current.Complete, current.Classification,
            current.References.Where(x => supervisor || x.ProjectId == project).ToArray(), current.Warranties.Where(x => supervisor || x.ProjectId == project).ToArray(), reasons,
            control.Holds.Count(x => x.State == "ACTIVE"), control.Item);
    }
    public async Task<RetentionBasisView> ConfirmBasisAsync(Guid actor, Guid project, Guid file, ConfirmRetentionBasisRequest request, string key, string expected, CancellationToken token)
    {
        await AuthorizeAsync(actor, project, true, token);
        await ScopedInventoryAsync(actor, project, file, token);
        return await CommandAsync<RetentionBasisView>(actor, project, "Anh02.RetentionBasis", key, new { project, file, warrantyIds = request.WarrantyIds.Order().ToArray(), request.ExpectedReferenceInventoryVersion, reason = request.Reason.Trim(), expected }, async ct =>
        {
            await AuthorizeAsync(actor, project, true, ct);
            var current = await ScopedInventoryAsync(actor, project, file, ct);
            var head = await context.Set<RetentionBasisHead>().SingleOrDefaultAsync(x => x.FileId == file, ct);
            if (expected != (head is null ? "none" : RetentionInventoryRepository.Version(head.RowVersion))) Reject(412, "concurrency_conflict");
            if (request.ExpectedReferenceInventoryVersion != current.Version) Reject(409, "retention_inventory_stale");
            if (!current.Complete) Reject(409, "retention_inventory_incomplete");
            if (!request.WarrantyIds.Order().SequenceEqual(current.Warranties.Select(x => x.Id).Order())) Reject(422, "basis_invalid");
            var revision = new RetentionBasisRevision { Id = Guid.NewGuid(), FileId = file, Revision = (head?.Revision ?? 0) + 1, InventoryVersion = current.Version, InventoryComplete = true, Classification = current.Classification,
                WarrantyReferencesJson = JsonSerializer.Serialize(current.Warranties, Json), ConfirmedBy = actor, ConfirmedAt = clock.GetUtcNow(), Reason = request.Reason.Trim(), SupersedesId = head?.RevisionId };
            context.Add(revision);
            if (head is null) { head = new RetentionBasisHead { FileId = file }; context.Add(head); }
            head.RevisionId = revision.Id; head.Revision = revision.Revision;
            Audit(actor, revision.Id, "retention_basis_confirmed", revision.Reason);
            await context.SaveChangesAsync(ct);
            return (revision.Id, new RetentionBasisView(revision.Id, file, revision.Revision, revision.PolicyVersion, revision.Classification, current.Version, true, current.Warranties, actor, revision.ConfirmedAt, revision.Reason, revision.SupersedesId, RetentionInventoryRepository.Version(head.RowVersion)));
        }, token);
    }
    public async Task<RetentionHoldView> CreateHoldAsync(Guid actor, CreateRetentionHoldRequest request, string key, CancellationToken token)
    {
        await AuthorizeAsync(actor, null, true, token);
        await RequireScopeAsync(request.ScopeType, request.ScopeId, token);
        return await CommandAsync<RetentionHoldView>(actor, null, "Anh02.RetentionHoldCreate", key, new { request.ScopeType, request.ScopeId, reason = request.Reason.Trim() }, async ct =>
        {
            await AuthorizeAsync(actor, null, true, ct); await RequireScopeAsync(request.ScopeType, request.ScopeId, ct);
            var hold = new RetentionHold { Id = Guid.NewGuid(), ScopeType = request.ScopeType, ScopeId = request.ScopeId, CreatedBy = actor, CreatedAt = clock.GetUtcNow(), Reason = request.Reason.Trim() };
            context.Add(hold); History(hold, actor, hold.Reason); Audit(actor, hold.Id, "retention_hold_created", hold.Reason); await context.SaveChangesAsync(ct);
            return (hold.Id, View(hold));
        }, token);
    }
    public async Task<RetentionHoldView> GetHoldAsync(Guid actor, Guid holdId, CancellationToken token)
    {
        var hold = await context.Set<RetentionHold>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == holdId, token);
        if (hold is null) Reject(404, "not_found");
        if (await SupervisorAsync(actor, token)) return View(hold!);
        if (hold!.ScopeType == "PROJECT") await AuthorizeAsync(actor, hold.ScopeId, false, token);
        else
        {
            var current = await inventory.ReadAsync(hold.ScopeId, token);
            if (current is null || current.PublicProjectIds.Count == 0) Reject(404, "not_found");
            var permitted = false;
            foreach (var project in current!.PublicProjectIds)
            {
                try { await AuthorizeAsync(actor, project, false, token); permitted = true; break; }
                catch (RetentionRequestException ex) when (ex.Status == 403) { }
            }
            if (!permitted) Reject(404, "not_found");
        }
        return View(hold);
    }
    public async Task<RetentionHoldView> ReleaseHoldAsync(Guid actor, Guid holdId, ReleaseRetentionHoldRequest request, string key, string expected, CancellationToken token)
    {
        await AuthorizeAsync(actor, null, true, token);
        if (!await context.Set<RetentionHold>().AsNoTracking().AnyAsync(x => x.Id == holdId, token)) Reject(404, "not_found");
        return await CommandAsync<RetentionHoldView>(actor, null, "Anh02.RetentionHoldRelease", key, new { holdId, reason = request.Reason.Trim(), expected }, async ct =>
        {
            await AuthorizeAsync(actor, null, true, ct);
            var hold = await context.Set<RetentionHold>().SingleOrDefaultAsync(x => x.Id == holdId, ct);
            if (hold is null) Reject(404, "not_found");
            if (expected != RetentionInventoryRepository.Version(hold!.RowVersion)) Reject(412, "concurrency_conflict");
            if (hold.State != "ACTIVE") Reject(409, "invalid_transition");
            hold.State = "RELEASED"; hold.ReleasedBy = actor; hold.ReleasedAt = clock.GetUtcNow();
            History(hold, actor, request.Reason.Trim()); Audit(actor, hold.Id, "retention_hold_released", request.Reason.Trim()); await context.SaveChangesAsync(ct);
            return (hold.Id, View(hold));
        }, token);
    }
    public async Task<RetentionEvaluationView> AdmitEvaluationAsync(Guid actor, Guid project, CreateRetentionEvaluationRequest request, string key, CancellationToken token)
    {
        await AuthorizeAsync(actor, project, false, token);
        if (request.FileIds is { } files) foreach (var file in files) await ScopedInventoryAsync(actor, project, file, token);
        return await CommandAsync<RetentionEvaluationView>(actor, project, "Anh02.RetentionEvaluate", key, new { project, fileIds = request.FileIds?.Order().ToArray() }, async ct =>
        {
            await AuthorizeAsync(actor, project, false, ct);
            if (request.FileIds is { } ids) foreach (var file in ids) await ScopedInventoryAsync(actor, project, file, ct);
            var job = new RetentionEvaluation { Id = Guid.NewGuid(), ProjectId = project, RequestedBy = actor, CreatedAt = clock.GetUtcNow(), SelectionJson = request.FileIds is null ? null : JsonSerializer.Serialize(request.FileIds.Order().ToArray(), Json) };
            context.Add(job); Audit(actor, job.Id, "retention_evaluation_requested", "Retention dry-run requested"); await context.SaveChangesAsync(ct);
            return (job.Id, EvaluationView(job, [], null));
        }, token);
    }
    public async Task<RetentionEvaluationView> GetEvaluationAsync(Guid actor, Guid project, Guid id, Guid? afterFile, int pageSize, CancellationToken token)
    {
        await AuthorizeAsync(actor, project, false, token);
        var job = await context.Set<RetentionEvaluation>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ProjectId == project, token);
        if (job is null) Reject(404, "not_found");
        var items = new List<RetentionItemView>();
        // Page after applying current visibility; a private file ID must never become a cursor.
        // Bounded batches avoid holding a live SQL reader across other same-context queries.
        var scanAfter = afterFile;
        while (items.Count <= pageSize)
        {
            var batchQuery = context.Set<RetentionEvaluationItem>().AsNoTracking().Where(x => x.EvaluationId == id);
            if (scanAfter.HasValue) batchQuery = batchQuery.Where(x => x.FileId.CompareTo(scanAfter.Value) > 0);
            var rows = await batchQuery.OrderBy(x => x.FileId).Take(pageSize + 1).ToListAsync(token);
            if (rows.Count == 0) break;
            foreach (var row in rows)
            {
                scanAfter = row.FileId;
                RetentionInventory current;
                try { current = await ScopedInventoryAsync(actor, project, row.FileId, token); }
                catch (RetentionRequestException ex) when (ex.Status == 404) { continue; }
                var control = await CaptureAsync(current, clock.GetUtcNow(), token);
                items.Add(new(row.FileId, row.Eligibility, Deserialize<string[]>(row.ReasonCodesJson), row.EligibleAfter, row.BasisVersion, row.InventoryVersion, row.HoldVersion,
                    row.BasisVersion == control.Item.BasisVersion && row.InventoryVersion == control.Item.InventoryVersion && row.HoldVersion == control.Item.HoldVersion));
                if (items.Count > pageSize) break;
            }
            if (rows.Count < pageSize + 1) break;
        }
        return EvaluationView(job!, items.Take(pageSize).ToArray(), items.Count > pageSize ? items[pageSize - 1].FileId.ToString() : null);
    }
    public async Task<bool> ProcessOneAsync(CancellationToken token)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await LockAsync(token);
            var job = await context.Set<RetentionEvaluation>().Where(x => x.Status == "QUEUED").OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync(token);
            if (job is null) { await tx.CommitAsync(token); return false; }
            var at = clock.GetUtcNow();
            var files = job.SelectionJson is null ? await inventory.KnownProjectFilesAsync(job.ProjectId, token) : Deserialize<Guid[]>(job.SelectionJson);
            foreach (var file in files)
            {
                var current = await inventory.ReadAsync(file, token);
                if (current is null || !current.References.Any(x => x.ProjectId == job.ProjectId)) continue;
                var control = await CaptureAsync(current, at, token);
                var item = control.Item;
                context.Add(new RetentionEvaluationItem { Id = Guid.NewGuid(), EvaluationId = job.Id, FileId = file, Eligibility = item.Eligibility, ReasonCodesJson = JsonSerializer.Serialize(item.ReasonCodes, Json), EligibleAfter = item.EligibleAfter,
                    BasisVersion = item.BasisVersion, InventoryVersion = item.InventoryVersion, HoldVersion = item.HoldVersion,
                    ControlSnapshotJson = JsonSerializer.Serialize(new { PolicyVersion = job.PolicyVersion, EvaluatedAt = at, Inventory = current, Basis = control.Basis, Holds = control.Holds.Select(View).ToArray() }, Json) });
            }
            job.Status = "COMPLETE"; job.EvaluatedAt = at;
            await context.SaveChangesAsync(token); await tx.CommitAsync(token); return true;
        });
    }
    private async Task<(RetentionItemView Item, RetentionBasisHead? Head, RetentionBasisRevision? Basis, List<RetentionHold> Holds)> CaptureAsync(RetentionInventory current, DateTimeOffset at, CancellationToken token)
    {
        var head = await context.Set<RetentionBasisHead>().AsNoTracking().SingleOrDefaultAsync(x => x.FileId == current.FileId, token);
        var basis = head is null ? null : await context.Set<RetentionBasisRevision>().AsNoTracking().SingleAsync(x => x.Id == head.RevisionId, token);
        var projects = current.References.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId!.Value).Distinct().ToArray();
        var holds = await context.Set<RetentionHold>().AsNoTracking().Where(x => (x.ScopeType == "FILE" && x.ScopeId == current.FileId) || (x.ScopeType == "PROJECT" && projects.Contains(x.ScopeId))).OrderBy(x => x.Id).ToListAsync(token);
        var boundaries = current.Warranties.Select(x => Boundary(x.WarrantyEndDate)).Concat(current.AdditionalEligibleAfter ?? []).ToArray();
        var result = RetentionEligibilityEvaluator.Evaluate(new(current.Complete, basis is not null, basis?.InventoryVersion == current.Version, holds.Count(x => x.State == "ACTIVE"), boundaries), at);
        var reasons = result.ReasonCodes.Concat(current.ReasonCodes).Distinct().ToArray();
        return (new(current.FileId, result.Eligibility, reasons, result.EligibleAfter, head is null ? "none" : RetentionInventoryRepository.Version(head.RowVersion), current.Version,
            RetentionInventoryRepository.Hash(holds.Select(x => new { x.Id, x.State, Version = RetentionInventoryRepository.Version(x.RowVersion) }).ToArray()), true), head, basis, holds);
    }
    private async Task<T> CommandAsync<T>(Guid actor, Guid? project, string operation, string key, object payload, Func<CancellationToken, Task<(Guid Id, T Value)>> action, CancellationToken token)
    {
        try
        {
            var outcome = await idempotency.ExecuteSerializableAsync(actor, project, operation, key.Trim(), RetentionInventoryRepository.Hash(payload), async ct =>
            {
                await LockAsync(ct);
                await AuthorizeAsync(actor, project, operation != "Anh02.RetentionEvaluate", ct);
                // A concurrent same-key command may have committed while this handler waited
                // for control admission. Prefer its receipt before checking a now-stale head.
                var durable = await context.Set<IdempotencyRecord>().AsNoTracking().SingleOrDefaultAsync(x => x.ActorUserId == actor && x.ProjectId == project && x.Operation == operation && x.IdempotencyKey == key.Trim(), ct);
                if (durable is not null)
                {
                    if (durable.RequestFingerprint != RetentionInventoryRepository.Hash(payload)) Reject(409, "duplicate_request");
                    return (durable.OperationId, durable.OutcomeJson);
                }
                var (id, value) = await action(ct);
                return (id, JsonSerializer.Serialize(value, Json));
            }, token);
            if (outcome.Status == IdempotencyOperationStatus.Conflict) Reject(409, "duplicate_request");
            return Deserialize<T>(outcome.OutcomeJson);
        }
        catch (DbUpdateConcurrencyException) { context.ChangeTracker.Clear(); throw new RetentionRequestException(412, "concurrency_conflict"); }
    }
    private Task<int> LockAsync(CancellationToken token) => context.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'Anh02.Retention.Control', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000; IF @result < 0 THROW 51000, 'Retention control lock unavailable', 1;", token);
    private async Task RequireScopeAsync(string type, Guid id, CancellationToken token)
    {
        var exists = type == "PROJECT" ? await context.Projects.AsNoTracking().AnyAsync(x => x.Id == id, token) : type == "FILE" && await context.Files.AsNoTracking().AnyAsync(x => x.Id == id, token);
        if (!exists) Reject(404, "not_found");
    }
    private void History(RetentionHold hold, Guid actor, string reason) => context.Add(new RetentionHoldHistory { Id = Guid.NewGuid(), HoldId = hold.Id, State = hold.State, ActorId = actor, OccurredAt = clock.GetUtcNow(), Reason = reason });
    private void Audit(Guid actor, Guid id, string action, string reason) => context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, clock.GetUtcNow(), action, "Retention", id, null, null, reason, "ANH02", null));
    private static RetentionHoldView View(RetentionHold x) => new(x.Id, x.ScopeType, x.ScopeId, x.State, x.CreatedBy, x.CreatedAt, x.Reason, x.ReleasedBy, x.ReleasedAt, RetentionInventoryRepository.Version(x.RowVersion));
    private static RetentionEvaluationView EvaluationView(RetentionEvaluation job, IReadOnlyList<RetentionItemView> items, string? cursor) => new(job.Id, job.ProjectId, job.Status, job.CreatedAt, job.EvaluatedAt, job.PolicyVersion, items, cursor, RetentionInventoryRepository.Version(job.RowVersion));
    private static DateTimeOffset? Boundary(DateOnly end) { try { return end == default ? null : RetentionEligibilityEvaluator.WarrantyEligibleAfter(end); } catch (ArgumentOutOfRangeException) { return null; } }
    private static T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, Json) ?? throw new InvalidOperationException("Invalid durable retention outcome.");
    private static void Reject(int status, string code) => throw new RetentionRequestException(status, code);
}
