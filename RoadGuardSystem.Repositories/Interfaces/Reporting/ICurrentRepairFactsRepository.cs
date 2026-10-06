namespace RoadGuardSystem.Repositories.Reporting;

// Internal current-stock query: period allocation is owned by the reporting consumer.
public sealed record CurrentRepairFactsQuery(Guid? RouteVersionId = null, Guid? SegmentSetId = null,
    Guid[]? SegmentIds = null);
public sealed record CurrentRepairInventoryItem(Guid Id, Guid ProjectId, Guid ObligationId, string Version,
    string ObligationVersion, string Presentation, Guid? DecisionId, string? DecisionVersion);
public sealed record CurrentRepairInventory(Guid ProjectId, CurrentRepairInventoryItem[] Items, string[] MissingReasons);

public interface ICurrentRepairFactsRepository
{
    Task<CurrentRepairInventory> CaptureAsync(Guid actor, Guid project, CurrentRepairFactsQuery filters,
        CancellationToken token);
}
