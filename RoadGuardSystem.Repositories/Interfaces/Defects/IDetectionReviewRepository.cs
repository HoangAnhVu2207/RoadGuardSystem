using RoadGuardSystem.Repositories.Defects;

namespace RoadGuardSystem.Repositories.Interfaces.Defects;

public interface IDetectionReviewRepository
{
    Task<DetectionReviewPersistenceResult> PersistAsync(
        DetectionReviewPersistenceRequest request,
        CancellationToken cancellationToken = default);
}
