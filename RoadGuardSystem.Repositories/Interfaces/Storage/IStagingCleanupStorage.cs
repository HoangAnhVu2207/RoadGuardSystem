namespace RoadGuardSystem.Repositories.Storage;

public interface IStagingCleanupStorage : IMultipartRecoveryStorage
{
    Task DeleteStagedObjectAsync(string objectKey, CancellationToken cancellationToken = default);
}
