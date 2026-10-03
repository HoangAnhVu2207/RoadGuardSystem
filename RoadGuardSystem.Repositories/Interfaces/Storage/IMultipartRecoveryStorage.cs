namespace RoadGuardSystem.Repositories.Storage;

public interface IMultipartRecoveryStorage
{
    Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default);
    Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default);
    Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default);
}
