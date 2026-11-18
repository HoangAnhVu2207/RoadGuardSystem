namespace RoadGuardSystem.Repositories.Storage;

public interface IRangedUploadObjectStorage
{
    Task<Stream> OpenRangeAsync(string objectKey, long first, long last, CancellationToken token = default);
}
