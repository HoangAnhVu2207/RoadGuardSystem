using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Reporting;

namespace RoadGuardSystem.Services.Reporting;

public sealed class CurrentRepairFactsReader(ICurrentRepairFactsRepository repository) : ICurrentRepairFactsReader
{
    public async Task<CurrentRepairFacts> CaptureAsync(Guid actor, Guid project, ReportingFiltersDto filters,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(filters);
        var inventory = await repository.CaptureAsync(actor, project,
            new(filters.RouteVersionId, filters.SegmentSetId, filters.SegmentIds), token);
        return new(inventory.ProjectId, inventory.Items.Select(item => new CurrentRepairItemFact(
            item.Id, item.ProjectId, item.ObligationId, item.Version, item.ObligationVersion, item.Presentation,
            item.DecisionId, item.DecisionVersion)).ToArray(), inventory.MissingReasons);
    }
}
