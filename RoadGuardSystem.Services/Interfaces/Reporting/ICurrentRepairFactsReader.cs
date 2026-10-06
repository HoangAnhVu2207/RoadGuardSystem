using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.Services.Reporting;

// Internal capture contract: no public schema/receipt changes or private evidence bytes.
public sealed record CurrentRepairItemFact(Guid Id, Guid ProjectId, Guid ObligationId, string Version,
    string ObligationVersion, string Presentation, Guid? DecisionId, string? DecisionVersion);
public sealed record CurrentRepairFacts(Guid ProjectId, CurrentRepairItemFact[] Items, string[] MissingReasons);
public interface ICurrentRepairFactsReader
{
    Task<CurrentRepairFacts> CaptureAsync(Guid actor, Guid project, ReportingFiltersDto filters, CancellationToken token);
}
