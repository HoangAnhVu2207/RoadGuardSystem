using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Implementations.Repairs;

public sealed partial class RepairSafetyRepository
{
    private const string ResolveOperation = "h4.repair.safety.resolve.v1";

    public Task<SafetyResult> ResolveAsync(SafetyResolveCommand command, CancellationToken token)
        => Produce(command.ActorId, command.Role, command.ProjectId, command.PackageId, command.ItemId,
            command.MeasureId, ResolveOperation, command.Key, command, async ct =>
            {
                if (command.Role != UserRoleCode.ProjectManager) Deny(403, "access_forbidden");
                var monitoring = await Monitoring(command.MeasureId, ct);
                var safety = await db.Set<RepairObligation>().FromSqlInterpolated(
                    $"SELECT * FROM RepairObligations WITH (UPDLOCK,HOLDLOCK) WHERE Id={monitoring.SafetyObligationId}")
                    .SingleAsync(ct);
                if (safety.IsResolved) Deny(409, "safety_already_resolved");
                if (!await db.Projects.AnyAsync(p => p.Id == command.ProjectId && p.Status == ProjectStatus.Active, ct))
                    Deny(409, "project_not_active");
                if (Version(monitoring) != command.ExpectedVersion ||
                    Convert.ToBase64String(db.Entry(safety).Property<byte[]>("RowVersion").CurrentValue!) != command.ObligationVersion)
                    Deny(409, "concurrency_conflict");
                if (safety.Kind != RepairObligationKind.TemporarySafety ||
                    monitoring.Measure.InstalledAt is null || command.CheckId != monitoring.CurrentCheckId)
                    Deny(409, "safety_resolution_not_ready");
                var check = monitoring.Checks.SingleOrDefault(c => c.Id == command.CheckId);
                if (check is null || check.Result != RepairSafetyCheckResult.Safe || check.Evidence.Count == 0 ||
                    monitoring.Warnings.Any(w => !monitoring.Acknowledgements.Any(a => a.WarningId == w.Id)))
                    Deny(409, "safety_resolution_not_ready");
                if (!command.ConditionSatisfied || command.Outcome is not ("REMOVED" or "REPLACED") ||
                    string.IsNullOrWhiteSpace(command.ChecklistConfirmation) || string.IsNullOrWhiteSpace(command.Reason))
                    Deny(400, "validation_error");
                if (command.EvidenceIds.Length == 0 || command.EvidenceIds.Distinct().Count() != command.EvidenceIds.Length ||
                    command.EvidenceIds.Any(id => !check.EvidenceIds.Contains(id)))
                    Deny(409, "safety_resolution_evidence_conflict");
                var evidence = check.Evidence.Where(e => command.EvidenceIds.Contains(e.SourceId)).ToArray();
                if (evidence.Length != command.EvidenceIds.Length) Deny(409, "safety_resolution_evidence_conflict");
                foreach (var snapshot in evidence)
                {
                    var link = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking().SingleOrDefaultAsync(e => e.Id == snapshot.SourceId, ct);
                    var file = await new AnhHuyFactsRepository(db).GetFileAsync(snapshot.FileId, ct);
                    if (link is null || link.ProjectId != safety.ProjectId || link.FileId != snapshot.FileId ||
                        link.Purpose != "MEASUREMENT" || file is null || file.ProjectId != safety.ProjectId ||
                        file.Purpose != "MEASUREMENT" || file.State != "VERIFIED" ||
                        file.FileVersion != snapshot.FileVersion || file.Checksum != snapshot.Hash || file.OwnerId != snapshot.ActualUploaderId ||
                        !snapshot.Verified || !snapshot.Related || snapshot.SourceKind != "FIELD_MEASUREMENT" ||
                        !await (from binding in db.Set<RepairFieldTaskBinding>().AsNoTracking()
                                where binding.ItemId == command.ItemId && binding.ProjectId == safety.ProjectId &&
                                    binding.ObligationId == monitoring.FormalObligationId && binding.DefectId == safety.DefectId &&
                                    binding.TaskId == link.TaskId && binding.AssignmentId == link.AssignmentId
                                select binding.Id).AnyAsync(ct)) Deny(409, "safety_resolution_evidence_conflict");
                }
                var now = clock.GetUtcNow(); var id = Guid.NewGuid();
                var condition = command.Outcome == "REMOVED" ? monitoring.Measure.RemovalCondition : monitoring.Measure.ReplacementCondition;
                var resolution = new SafetyResolutionFact(id, command.ActorId, now, command.Outcome, check.Id,
                    command.Reason, condition, command.ChecklistConfirmation, command.ExpectedVersion, command.ObligationVersion, evidence);
                db.AuditLogs.Add(AuditLog.Create(id, command.ActorId, now, "repair_safety_resolved", "RepairObligation",
                    safety.Id, null, JsonSerializer.Serialize(resolution, Json), command.Reason, ResolveOperation,
                    command.MeasureId, ["id", "actorId", "at", "outcome", "checkId", "reason", "condition", "checklistConfirmation",
                        "measureVersion", "obligationVersion", "evidence", "fileId", "fileVersion", "hash", "purpose",
                        "verified", "related", "sourceKind", "sourceId", "capturedAt", "reuseDecisionId", "reusedSource", "actualUploaderId"]));
                await db.SaveChangesAsync(ct);
                safety.ResolveSafety(id);
                var package = await db.RepairPackages.SingleAsync(p => p.Id == command.PackageId, ct);
                var packageRevision = db.Entry(package).Property<long>("MutationRevision");
                packageRevision.CurrentValue = checked(packageRevision.CurrentValue + 1);
                // Touch the existing versioned monitoring resource without changing its check.
                db.Entry(monitoring).Property(m => m.CurrentCheckId).IsModified = true;
                await db.SaveChangesAsync(ct);
                return (id, View(monitoring) with { Resolution = resolution });
            }, token);

    private async Task<SafetyResolutionFact?> Resolution(RepairSafetyMonitoring monitoring, CancellationToken token)
    {
        var id = await db.Set<RepairObligation>().Where(o => o.Id == monitoring.SafetyObligationId)
            .Select(o => o.SafetyResolutionId).SingleAsync(token);
        if (id is null) return null;
        var snapshot = await db.AuditLogs.AsNoTracking().Where(a => a.Id == id && a.EntityId == monitoring.SafetyObligationId &&
            a.EntityType == "RepairObligation" && a.EventType == "repair_safety_resolved" && a.CorrelationId == monitoring.MeasureId)
            .Select(a => a.AfterSnapshot).SingleAsync(token);
        var resolution = JsonSerializer.Deserialize<SafetyResolutionFact>(snapshot!, Json);
        if (resolution is null || resolution.Id != id || resolution.ActorId == Guid.Empty || resolution.CheckId == Guid.Empty ||
            resolution.Evidence is null || resolution.Evidence.Length == 0 ||
            resolution.Evidence.Any(e => e.FileId == Guid.Empty || e.SourceId == Guid.Empty ||
                string.IsNullOrWhiteSpace(e.FileVersion) || e.Hash is not { Length: 64 } ||
                !e.Verified || !e.Related || e.SourceKind != "FIELD_MEASUREMENT" || e.ActualUploaderId is null))
            throw new InvalidOperationException("Stored safety resolution is incomplete.");
        return resolution;
    }
}
