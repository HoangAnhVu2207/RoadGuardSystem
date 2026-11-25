using RoadGuardSystem.BusinessObjects.Candidates;

namespace RoadGuardSystem.Repositories.Defects;

public interface IReportMatchingLocationRepository
{
    Task SaveAsync(ReportMatchingLocation location, CancellationToken token);
    Task<bool> ReceiptExistsAsync(Guid actor, Guid project, string key, CancellationToken token);
    Task<IReadOnlyDictionary<Guid, double>> DistancesAsync(Guid project, decimal latitude, decimal longitude,
        CancellationToken token);
}
