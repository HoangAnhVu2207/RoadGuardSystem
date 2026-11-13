using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.Repositories.Models.Huy01;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

internal static class ReporterRepairProgress
{
    internal static async Task<Guid[]> ReadDefectsAsync(RoadGuardDbContext db, Guid report, CancellationToken ct)
        => await (from head in db.Set<HuyCandidateSourceHead>()
                  join link in db.Set<HuyDefectSourceLink>() on head.DecisionId equals link.DecisionId
                  where head.SourceKind == CandidateSourceKind.Report && head.SourceId == report &&
                      link.SourceKind == CandidateSourceKind.Report && link.SourceId == report && link.EndedAt == null
                  select link.DefectId).Distinct().OrderBy(id => id).ToArrayAsync(ct);

    internal static Task<bool> HasNoDefectConclusionAsync(RoadGuardDbContext db, Guid caseId, CancellationToken ct)
        => db.Set<CaseConclusion>().Where(row => EF.Property<Guid>(row, "CaseId") == caseId)
            .OrderByDescending(row => row.ConcludedAt).ThenByDescending(row => row.Id)
            .Select(row => row.Outcome == CaseConclusionOutcome.NoDefect).FirstOrDefaultAsync(ct);

    internal static async Task<Progress?> ResolveAsync(RoadGuardDbContext db, Guid project, Guid[] defects, CancellationToken ct)
    {
        if (defects.Length == 0 || await db.Defects.CountAsync(row => defects.Contains(row.Id) &&
            row.ProjectId == project && (row.Status == DefectStatus.Verified || row.Status == DefectStatus.Resolved ||
                row.Status == DefectStatus.Closed), ct) != defects.Length)
            return null;

        var obligations = await db.Set<RepairObligation>().AsNoTracking()
            .Where(row => defects.Contains(row.DefectId) && row.Kind == RepairObligationKind.FormalRepair && row.Mandatory)
            .Select(row => new
            {
                row.Id,
                row.DefectId,
                row.ProjectId,
                row.CurrentRepairItemId,
                row.EffectiveResolutionDecisionId,
                row.EffectiveResolutionHeadDecisionId,
                Owner = db.Set<ObligationResponsibility>().Where(owner => owner.ObligationId == row.Id)
                    .Select(owner => (Guid?)owner.CurrentProjectId).SingleOrDefault()
            }).ToArrayAsync(ct);
        if (obligations.Any(row => row.ProjectId != project || (row.Owner ?? row.ProjectId) != project) ||
            obligations.Select(row => row.DefectId).Distinct().Count() != defects.Length)
            return null;

        var obligationIds = obligations.Select(row => row.Id).ToArray();
        var rows = await db.Set<RepairItem>().AsNoTracking().Where(item => obligationIds.Contains(item.ObligationId))
            .Select(item => new
            {
                item.Id,
                item.ProjectId,
                item.DefectId,
                item.ObligationId,
                item.Mode,
                item.State,
                item.EffectiveDecisionId,
                Started = db.Set<RepairExecutionStart>().Any(start => start.Id == item.CurrentExecutionStartId &&
                    start.ProjectId == item.ProjectId && start.ItemId == item.Id),
                Accepted = db.Set<RepairDecision>().Any(decision => decision.Id == item.EffectiveDecisionId &&
                    decision.ItemId == item.Id && decision.ObligationId == item.ObligationId &&
                    decision.DefectId == item.DefectId && decision.Mode == item.Mode &&
                    decision.Result == RepairPresentationState.Confirmed &&
                    decision.Role == (item.Mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager))
            }).ToArrayAsync(ct);

        var byObligation = rows.ToLookup(row => row.ObligationId);
        var confirmed = new List<Guid>(obligations.Length);
        var started = false;
        foreach (var obligation in obligations)
        {
            var items = byObligation[obligation.Id];
            if (obligation.CurrentRepairItemId is not Guid pin)
            {
                if (items.Any() || obligation.EffectiveResolutionHeadDecisionId is not null) return null;
                continue;
            }
            var item = items.SingleOrDefault(row => row.Id == pin);
            if (item is null || item.ProjectId != project || item.DefectId != obligation.DefectId ||
                item.State == RepairItemState.Cancelled || (item.State >= RepairItemState.InProgress && !item.Started))
                return null;

            started |= item.Started;
            if (item.Started && item.State == RepairItemState.Confirmed && item.Accepted &&
                item.EffectiveDecisionId is Guid decision && decision == obligation.EffectiveResolutionDecisionId &&
                decision == obligation.EffectiveResolutionHeadDecisionId)
                confirmed.Add(decision);
        }

        return confirmed.Count == obligations.Length
            ? new("ACCEPTED", Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(confirmed.Order().ToArray()))))
            : new(started ? "IN_PROGRESS" : "NOT_STARTED", null);
    }

    internal sealed record Progress(string Status, string? AcceptanceFingerprint);
}
