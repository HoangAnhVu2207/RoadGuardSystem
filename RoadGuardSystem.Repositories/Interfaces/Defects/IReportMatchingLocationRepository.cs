using RoadGuardSystem.BusinessObjects.Candidates;

namespace RoadGuardSystem.Repositories.Defects;

public sealed record CandidateSpatialDistance(double? Meters, string Status, string? Method = null);
public interface IReportMatchingLocationRepository
{
    Task SaveAsync(ReportMatchingLocation location, CancellationToken token);
    Task<bool> ReceiptExistsAsync(Guid actor, Guid project, string key, CancellationToken token);
    Task<IReadOnlyDictionary<Guid, CandidateSpatialDistance>> DistancesAsync(Guid project, decimal latitude, decimal longitude,
        CancellationToken token);
}
