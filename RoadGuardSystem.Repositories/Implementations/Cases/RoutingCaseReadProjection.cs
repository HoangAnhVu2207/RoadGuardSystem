using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Routing;
using RoadGuardSystem.Repositories.Integration;

namespace RoadGuardSystem.Repositories.Implementations.Cases;

internal sealed class RoutingCaseReadProjection(RoadGuardDbContext db, IAnhHuyFactsRepository files)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly RoutingSourceVersions versions = new(db, files);
    internal async Task<(RoutingDispositionView? Disposition, RoutingTriageSourceView? Source)> ReadAsync(Guid caseId, CancellationToken token)
    {
        var latest = await db.Set<CaseRoutingEvent>().AsNoTracking().Where(x => x.CaseId == caseId).OrderByDescending(x => x.Sequence).FirstOrDefaultAsync(token);
        if (latest is null) return (null, null);
        if (latest.Kind == "TRIAGE")
            return (null, latest.TriageSourceJson is null ? null : new(latest.Id,
                JsonSerializer.Deserialize<RoutingTriagePin>(latest.TriageSourceJson, Json)!, latest.CaseSourceVersion, latest.At));
        var assigned = await db.IncidentCases.AsNoTracking().Where(c => c.Id == caseId).Select(c => c.ProjectId != null).SingleAsync(token);
        if (assigned) return (null, null);
        var source = await versions.CaseSourceVersionAsync(caseId, token);
        var registry = await versions.RegistryVersionAsync(token);
        var effective = latest.CaseSourceVersion == source && latest.RegistryVersion == registry;
        return (new(latest.Id, effective ? latest.Outcome : null, effective ? "EFFECTIVE" : "REVIEW_REQUIRED", latest.At), null);
    }
}
