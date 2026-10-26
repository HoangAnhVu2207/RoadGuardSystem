using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Models.Huy01;
namespace RoadGuardSystem.Repositories.Implementations.Cases;

public sealed class PublicPublicationRepository(RoadGuardDbContext db, TimeProvider clock) : IPublicPublicationRepository
{
    public async Task GuardAsync(Guid actor, UserRoleCode role, Guid project, Guid target, bool withdraw,
        Func<Guid, CancellationToken, Task<bool>> access, CancellationToken ct)
    {
        if (role != UserRoleCode.ProjectManager) throw new CaseWorkflowException(403, "access_forbidden");
        await new CaseWorkflowRepository(db).GuardAsync(actor, role, [], project, access, ct);
        if (withdraw)
        {
            var row = await db.Set<PublicPublication>().FromSqlInterpolated($"SELECT * FROM [PublicPublications] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={target}").AsNoTracking().SingleOrDefaultAsync(ct);
            if (row is null || row.ProjectId != project) throw new CaseWorkflowException(404, "not_found");
        }
        else await EligibleAsync(project, target, ct);
    }
    private async Task<(IncidentCase Incident, CaseConclusion Conclusion, string Code)> EligibleAsync(Guid project, Guid defect, CancellationToken ct)
    {
        var d = await db.Defects.FromSqlInterpolated($"SELECT * FROM [Defects] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={defect}").AsNoTracking().SingleOrDefaultAsync(ct);
        if (d is null || d.ProjectId != project) throw new CaseWorkflowException(404, "not_found");
        var catalog = await db.DefectTypes.FromSqlInterpolated($"SELECT * FROM [DefectTypes] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={d.DefectTypeCode}").AsNoTracking().SingleOrDefaultAsync(ct);
        if (d.Status != DefectStatus.Verified || PublicPublicationPolicy.Category(d.DefectTypeCode, catalog?.IsActive == true) is null ||
            !await db.DefectVerificationLogs.AnyAsync(x => x.DefectId == defect && x.Action == DefectVerificationAction.Confirm, ct))
            throw new CaseWorkflowException(409, "source_not_ready");
        var cases = await (from link in db.Set<HuyConclusionDefect>()
                           join conclusion in db.Set<CaseConclusion>() on link.ConclusionId equals conclusion.Id
                           where link.DefectId == defect && conclusion.Outcome == CaseConclusionOutcome.Confirmed
                           select EF.Property<Guid>(conclusion, "CaseId")).Distinct().OrderBy(x => x).ToArrayAsync(ct);
        foreach (var id in cases)
        {
            await db.Database.SqlQuery<int>($"SELECT CAST(COUNT(*) AS int) AS [Value] FROM [IncidentCases] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").SingleAsync(ct);
            var incident = await db.IncidentCases.AsNoTracking().Include(x => x.Conclusions).SingleAsync(x => x.Id == id, ct);
            var conclusion = incident.Conclusions.OrderBy(x => x.ConcludedAt).ThenBy(x => x.Id).LastOrDefault();
            if (incident.ProjectId != project || incident.Status != IncidentCaseStatus.Concluded || conclusion is null ||
                conclusion.Outcome != CaseConclusionOutcome.Confirmed || !conclusion.DefectIds.Contains(defect) || conclusion.EvidenceIds.Count == 0) continue;
            await new CaseWorkflowRepository(db).ValidatePublicSourceAsync(incident, defect, conclusion.EvidenceIds, ct);
            var relational = await db.Set<HuyConclusionEvidence>().Where(x => x.ConclusionId == conclusion.Id).Select(x => x.EvidenceId).ToArrayAsync(ct);
            if (!relational.Order().SequenceEqual(conclusion.EvidenceIds.Order())) throw new CaseWorkflowException(409, "source_not_ready");
            return (incident, conclusion, d.DefectTypeCode);
        }
        throw new CaseWorkflowException(409, "source_not_ready");
    }
    public async Task<PublicPublication> CreateAsync(Guid actor, Guid project, Guid defect, CancellationToken ct)
    {
        var source = await EligibleAsync(project, defect, ct);
        var row = PublicPublication.Create(Guid.NewGuid(), project, defect, source.Incident.Id, source.Conclusion.Id, actor, source.Code, true, clock.GetUtcNow());
        db.Set<PublicPublication>().Add(row); Audit(actor, row, "public_publication_created"); await db.SaveChangesAsync(ct); return row;
    }
    public async Task<PublicPublication> WithdrawAsync(Guid actor, Guid project, Guid id, CancellationToken ct)
    {
        var row = await db.Set<PublicPublication>().SingleAsync(x => x.Id == id && x.ProjectId == project, ct);
        if (row.Withdraw(actor, clock.GetUtcNow())) { Audit(actor, row, "public_publication_withdrawn"); await db.SaveChangesAsync(ct); }
        return row;
    }
    public Task<PublicPublication?> ReadAsync(Guid id, CancellationToken ct)
        => db.Set<PublicPublication>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.WithdrawnAt == null, ct);
    private void Audit(Guid actor, PublicPublication row, string action)
        => db.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actor, clock.GetUtcNow(), action, "PublicPublication", row.Id, null,
            System.Text.Json.JsonSerializer.Serialize(new { publicId = row.Id }), null, "c1.publication", null, ["publicId"]));
}
