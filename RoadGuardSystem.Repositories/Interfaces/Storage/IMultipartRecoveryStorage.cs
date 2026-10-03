namespace RoadGuardSystem.Repositories.Storage;

// Optional additive capability: callers that only read/verify existing uploads remain compatible.
public interface IMultipartRecoveryStorage
{
    Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default);
    Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default);
    Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default);
}
