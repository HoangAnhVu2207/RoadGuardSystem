using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Cases;

namespace RoadGuardSystem.Repositories.Projects;

public sealed partial class ProjectLifecycleRepository
{
    public async Task ValidateRecurrenceAsync(Guid project, Guid predecessor, Guid decision, Guid[] evidence, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Recurrence source admission requires an authority transaction.");
        await db.Projects.FromSqlInterpolated($"SELECT * FROM Projects WITH (UPDLOCK,HOLDLOCK) WHERE Id={project}").AsNoTracking().LoadAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM RepairObligations WITH (UPDLOCK,HOLDLOCK) WHERE ProjectId={project} AND DefectId={predecessor}", cancellationToken);
        var valid = await db.Set<RepairObligation>().AnyAsync(x => x.ProjectId == project && x.DefectId == predecessor &&
            x.EffectiveResolutionDecisionId == decision && db.Set<RepairDecision>().Any(d => d.Id == decision &&
                d.Result == RepairPresentationState.Confirmed && db.Set<RepairItem>().Any(i => i.Id == d.ItemId &&
                    i.EffectiveDecisionId == decision && i.State == RepairItemState.Confirmed)), cancellationToken);
        if (!valid) throw new CaseWorkflowException(409, "genuine_recurrence_source_required");
        try { _ = await Evidence(project, evidence, clock.GetUtcNow(), cancellationToken); }
        catch (Rejected) { throw new CaseWorkflowException(409, "verified_project_evidence_required"); }
    }

    public async Task LinkRecurrenceAsync(Guid actor, Guid project, Guid newDefect, Guid predecessor, Guid decision,
        Guid[] evidence, string reason, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Recurrence must commit with confirmed new-Defect creation.");
        await ValidateRecurrenceAsync(project, predecessor, decision, evidence, cancellationToken);
        if (newDefect == predecessor || !await db.Defects.AnyAsync(x => x.Id == newDefect && x.ProjectId == project, cancellationToken))
            throw new CaseWorkflowException(409, "genuine_recurrence_source_required");
        var now = clock.GetUtcNow(); var id = Guid.NewGuid();
        var action = LD06LifecycleAction.Create(id, project, actor, LD06ActionKind.LinkRecurrence, now, reason,
            JsonSerializer.Serialize(new { previousDefectId = predecessor, newDefectId = newDefect, priorRepairDecisionId = decision, evidenceFileIds = evidence }, Json),
            defect: predecessor, linked: newDefect, decision: decision);
        db.Add(action);
        foreach (var file in await Evidence(project, evidence, now, cancellationToken)) db.Add(LD06ActionEvidence.Pin(id, file.Id, file.Checksum));
        db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, now, "ld06_recurrence", "Defect", newDefect,
            null, action.FactsJson, reason, "ld06.lifecycle", id, ["previousDefectId", "newDefectId", "priorRepairDecisionId", "evidenceFileIds"]));
        db.OutboxMessages.Add(OutboxMessage.Create(id, "project.lifecycle.changed.v1", now, null,
            JsonSerializer.Serialize(new { schemaVersion = 1, actionId = id, projectId = project, kind = "LinkRecurrence", at = now }, Json)));
        await db.SaveChangesAsync(cancellationToken);
    }
}
