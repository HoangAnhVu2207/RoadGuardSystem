namespace RoadGuardSystem.Repositories.Storage;

public sealed record Anh02ArtifactMetadata(string Key, long SizeBytes, string Sha256, string MediaType);
public sealed record Anh02ArtifactRead(Stream Content, Anh02ArtifactMetadata Metadata) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
public interface IAnh02ArtifactStore
{
    // Return only after reading the durable object back and hashing actual bytes.
    Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default);
    Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default);
}
