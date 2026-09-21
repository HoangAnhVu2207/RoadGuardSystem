namespace RoadGuardSystem.Repositories.Storage;

public interface IFileContentStore
{
    Task<StoredContent> StoreAsync(
        Stream content,
        string originalName,
        string declaredMimeType,
        string expectedChecksum,
        long? declaredSizeBytes,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageUri, CancellationToken cancellationToken = default);
}

internal interface IFileContentCleanup
{
    Task DeleteAsync(string storageUri, CancellationToken cancellationToken = default);
}
