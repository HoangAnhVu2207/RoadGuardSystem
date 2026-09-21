namespace RoadGuardSystem.Repositories.Files;

public interface IFileRepository
{
    Task<FileStoreResult> StoreAsync(
        StoreFileRequest request,
        CancellationToken cancellationToken = default);
}
