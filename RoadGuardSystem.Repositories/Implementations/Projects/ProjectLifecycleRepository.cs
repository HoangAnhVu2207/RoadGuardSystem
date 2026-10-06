using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Implementations.Reporting;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Projects;

public sealed class ProjectLifecycleRepository(RoadGuardDbContext db, TimeProvider clock) : IProjectLifecycleRepository
{
    private const string RenewOperation = "h6.project.renewed-scope.v1";
    private sealed class Rejected(int status, string code) : Exception
    { public ProjectLifecycleWriteResult Result { get; } = new(status, code); }
    private sealed class ReceiptRace : Exception { }
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ProjectLifecycleWriteResult> RenewAsync(ProjectRenewedHandlingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command); cancellationToken.ThrowIfCancellationRequested();
        if (command.ActorId == Guid.Empty || command.ProjectId == Guid.Empty || command.Input is null ||
            command.Input.OperationalClosureId == Guid.Empty || string.IsNullOrWhiteSpace(command.Key) || command.Key.Length > 200 ||
            string.IsNullOrWhiteSpace(command.Input.Reason) || command.Input.Reason.Length > 2000 ||
            string.IsNullOrWhiteSpace(command.Input.Basis) || command.Input.Basis.Length > 2000 ||
            string.IsNullOrWhiteSpace(command.Input.HandlingScope) || command.Input.HandlingScope.Length > 4000 ||
            command.ExpectedProjectionVersion?.Length != 64 || command.ExpectedProjectionVersion.Any(value => !Uri.IsHexDigit(value)))
            return new(400, "validation_failed");
        try
        {
            async Task Guard(CancellationToken token)
            {
                if (command.Role != UserRoleCode.Supervisor) throw new Rejected(403, "access_forbidden");
                await Anh02ReceiptAuthority.LockAsync(db, command.ActorId, command.ProjectId, token);
                if (!await new AnhHuyFactsRepository(db).IsCurrentActorAsync(command.ActorId, command.Role, token))
                    throw new Rejected(403, "access_forbidden");
                await ReadCore(command.ActorId, command.ProjectId, token);
                var stored = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row =>
                    row.ActorUserId == command.ActorId && row.ProjectId == command.ProjectId && row.Operation == RenewOperation && row.IdempotencyKey == command.Key, token);
                if (stored is not null && !await db.Set<ProjectLifecycleHistoryRecord>().AsNoTracking().AnyAsync(row =>
                    row.Id == stored.OperationId && row.ProjectId == command.ProjectId && row.ActorId == command.ActorId &&
                    row.Kind == ProjectLifecycleFactKind.RenewedHandlingScope && row.SourceDisposition == "TARGET_CONFIRMED" &&
                    db.Set<ProjectLifecycleHistoryRecord>().Any(source => source.Id == row.OperationalClosureId && source.ProjectId == row.ProjectId &&
                        source.Kind == ProjectLifecycleFactKind.OperationalClosure && source.SourceDisposition == "TARGET_CONFIRMED" && source.AuthoritySourceReference.Trim() != ""), token))
                    throw new Rejected(403, "stored_receipt_access_forbidden");
            }
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            { schemaVersion = 1, command.ProjectId, command.Input, command.ExpectedProjectionVersion }, Json))).ToLowerInvariant();
            var result = await new IdempotencyOperationService(db).ExecuteSerializableAsync(command.ActorId, command.ProjectId,
                RenewOperation, command.Key, fingerprint, async token =>
                {
                    await Guard(token);
                    if (await db.IdempotencyRecords.AsNoTracking().AnyAsync(row => row.ActorUserId == command.ActorId &&
                        row.ProjectId == command.ProjectId && row.Operation == RenewOperation && row.IdempotencyKey == command.Key, token)) throw new ReceiptRace();
                    var view = await ReadCore(command.ActorId, command.ProjectId, token);
                    if (view is null) throw new Rejected(404, "not_found");
                    if (view.Version != command.ExpectedProjectionVersion) throw new Rejected(409, "concurrency_conflict");
                    var source = await db.Set<ProjectLifecycleHistoryRecord>().AsNoTracking().SingleOrDefaultAsync(row =>
                        row.Id == command.Input.OperationalClosureId && row.ProjectId == command.ProjectId &&
                        row.Kind == ProjectLifecycleFactKind.OperationalClosure && row.SourceDisposition == "TARGET_CONFIRMED" &&
                        row.AuthoritySourceReference.Trim() != "", token);
                    var now = clock.GetUtcNow();
                    if (source is null || source.RecordedAtUtc > now) throw new Rejected(409, "operational_closure_source_unavailable");
                    // A recorded closure source remains history even when new intake or a correction
                    // invalidates its current basis. Renewing scope never closes those obligations.
                    var record = ProjectLifecycleHistoryRecord.RecordRenewed(Guid.NewGuid(), source, command.ActorId, command.Role,
                        true, true, now, command.Input.Reason, command.Input.Basis, command.Input.HandlingScope);
                    db.Add(record);
                    db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), command.ActorId, now, "project_renewed_handling_scope",
                        "Project", command.ProjectId, JsonSerializer.Serialize(new { operationalClosureId = source.Id, version = view.Version }, Json),
                        JsonSerializer.Serialize(new { renewedScopeId = record.Id, record.OperationalClosureId, handlingScope = command.Input.HandlingScope }, Json),
                        command.Input.Reason, "h6.project.lifecycle", record.Id, ["operationalClosureId", "handlingScope", "renewedScopeId", "version"]));
                    await db.SaveChangesAsync(token); await Guard(token);
                    var updated = await ReadCore(command.ActorId, command.ProjectId, token)
                        ?? throw new InvalidOperationException("Renewed project source disappeared.");
                    return (record.Id, JsonSerializer.Serialize(updated, Json));
                }, cancellationToken, receiptAccessGuard: Guard);
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(result.Status == IdempotencyOperationStatus.Replayed ? 200 : 201, Value:
                JsonSerializer.Deserialize<ProjectLifecycleFacts>(result.OutcomeJson, Json)
                ?? throw new InvalidOperationException("Durable renewed handling outcome is missing."));
        }
        catch (ReceiptRace) { db.ChangeTracker.Clear(); return await RenewAsync(command, cancellationToken); }
        catch (Rejected rejected) { db.ChangeTracker.Clear(); return rejected.Result; }
        catch (UnauthorizedAccessException) { db.ChangeTracker.Clear(); return new(403, "access_forbidden"); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return new(409, "concurrency_conflict"); }
    }
    public Task<ProjectLifecycleFacts?> ReadAsync(Guid actor, Guid project, CancellationToken cancellationToken)
        => new ReportingRepository(db).ReadConsistentlyAsync(token => ReadCore(actor, project, token), cancellationToken);

    private async Task<ProjectLifecycleFacts?> ReadCore(Guid actor, Guid project, CancellationToken token)
    {
        var day = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var role = await db.Users.AsNoTracking().Where(row => row.Id == actor && row.Status == UserStatus.Active && !row.MustChangePassword &&
            (row.RoleCode == UserRoleCode.ProjectManager || row.RoleCode == UserRoleCode.Supervisor) &&
            db.Roles.Any(value => value.Code == row.RoleCode && value.IsActive)).Select(row => (UserRoleCode?)row.RoleCode).SingleOrDefaultAsync(token);
        if (actor == Guid.Empty || project == Guid.Empty || role is null || !await db.ProjectMembers.AsNoTracking().AnyAsync(row =>
            row.ProjectId == project && row.UserId == actor && row.RoleCode == role && row.Status == ProjectMemberStatus.Active &&
            row.ValidFrom <= day && (row.ValidTo == null || row.ValidTo >= day), token))
            throw new UnauthorizedAccessException("Current project lifecycle source authority is required.");
        var projectVersion = await db.Projects.AsNoTracking().Where(row => row.Id == project)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleOrDefaultAsync(token);
        if (projectVersion is null) return null;
        var history = await db.Set<ProjectLifecycleHistoryRecord>().AsNoTracking().Where(row => row.ProjectId == project)
            .OrderBy(row => row.RecordedAtUtc).ThenBy(row => row.Id).ToArrayAsync(token);
        var obligations = await db.Set<RepairObligation>().AsNoTracking().Where(row => row.ProjectId == project)
            .Select(row => new
            {
                row.Id,
                row.DefectId,
                row.Mandatory,
                row.EffectiveResolutionDecisionId,
                Version = EF.Property<byte[]>(row, "RowVersion")
            }).ToArrayAsync(token);
        var defects = await db.Defects.AsNoTracking().Where(row => row.ProjectId == project).Select(row => row.Id).ToArrayAsync(token);
        var proved = history.Where(row => row.SourceDisposition == "TARGET_CONFIRMED" && !string.IsNullOrWhiteSpace(row.AuthoritySourceReference)).ToArray();
        // An empty query is not a completeness proof. A future adopted lifecycle
        // producer must retain an exact inventory observation; candidates cannot do so.
        var inventory = proved.Where(row => row.Kind == ProjectLifecycleFactKind.InventoryObservation).Any(row =>
            MatchesInventory(row.FactsJson, defects, obligations.Select(value => value.Id).ToArray())) &&
            defects.All(id => obligations.Any(row => row.DefectId == id));
        var facts = obligations.Select(row => new ProjectLifecycleObligationFact(row.Id, row.Mandatory,
            row.EffectiveResolutionDecisionId.HasValue, Transfer(row.Id))).ToArray();
        ProjectObligationTransferFact? Transfer(Guid obligation)
        {
            var accepted = proved.LastOrDefault(row => row.Kind == ProjectLifecycleFactKind.ObligationTransferAcceptance && row.ObligationId == obligation);
            var grant = accepted is null ? null : proved.SingleOrDefault(row => row.Id == accepted.GrantId && row.Kind == ProjectLifecycleFactKind.ObligationTransferGrant);
            return accepted is not null && grant is not null && grant.ObligationId == obligation && grant.ReceiverId == accepted.ReceiverId &&
                accepted.ActorId == accepted.ReceiverId && accepted.RecordedAtUtc >= grant.RecordedAtUtc
                ? new(grant.Id, accepted.ReceiverId!.Value, true, true, true) : null;
        }
        var completion = proved.Any(row => row.Kind == ProjectLifecycleFactKind.ConstructionCompletion);
        var closed = proved.Any(row => row.Kind == ProjectLifecycleFactKind.OperationalClosure);
        var warranty = await db.Warranties.AsNoTracking().AnyAsync(row => row.ProjectId == project, token);
        var projection = ProjectLifecycleProjection.Evaluate(completion, closed, warranty, facts);
        var missing = new List<string>();
        if (!completion || !closed) missing.Add("LIFECYCLE_AUTHORITY_SOURCE_NOT_VERIFIED");
        if (!inventory) missing.Add("OBLIGATION_INVENTORY_NOT_VERIFIED");
        var output = history.Select(row => new ProjectLifecycleHistoryFact(row.Id, row.Kind.ToString(), row.ActorId, row.RecordedAtUtc,
            row.ObligationId, row.GrantId, row.ReceiverId, row.OperationalClosureId, row.Reason, row.BasisReference, row.AuthoritySourceReference, row.SourceDisposition, row.DefectId, row.LinkedDefectId, HandlingScope(row))).ToArray();
        var revision = JsonSerializer.Serialize(new
        {
            project,
            projectVersion,
            history = output,
            obligations = obligations.OrderBy(row => row.Id),
            defects = defects.Order(),
            warranty,
            inventory
        });
        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(revision))).ToLowerInvariant();
        return new(project, completion ? "CONFIRMED" : "UNKNOWN", closed && inventory ?
            (projection.OperationallyClosed ? "CONFIRMED" : "BASIS_INVALIDATED") : "UNKNOWN", warranty, true,
            inventory ? "VERIFIED" : "UNKNOWN", inventory ? (projection.CanOperationallyClose ? "ELIGIBLE" : "BLOCKED") : "UNKNOWN",
            projection.OutstandingMandatoryObligationIds.ToArray(), missing.ToArray(), output, version);
    }

    private static string? HandlingScope(ProjectLifecycleHistoryRecord record)
    {
        if (record.Kind != ProjectLifecycleFactKind.RenewedHandlingScope) return null;
        try
        {
            using var json = JsonDocument.Parse(record.FactsJson);
            if (json.RootElement.TryGetProperty("handlingScope", out var value) && value.ValueKind == JsonValueKind.String &&
                value.GetString() is { Length: > 0 and <= 4000 } scope) return scope;
        }
        catch (JsonException) { }
        return null;
    }

    private static bool MatchesInventory(string json, Guid[] defects, Guid[] obligations)
    {
        try
        {
            using var document = JsonDocument.Parse(json); var root = document.RootElement;
            return root.TryGetProperty("completeInventory", out var complete) && complete.ValueKind == JsonValueKind.True &&
                Same("defectIds", defects) && Same("obligationIds", obligations);
            bool Same(string property, Guid[] actual) => root.TryGetProperty(property, out var values) && values.ValueKind == JsonValueKind.Array &&
                values.EnumerateArray().Select(value => value.GetGuid()).Order().SequenceEqual(actual.Order());
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException) { return false; }
    }
}
