using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.UnitTests.Exports;

public sealed class Anh02ExportArtifactStoreTests
{
    [Fact]
    public async Task Minio_seam_reads_durable_actual_bytes_and_never_overwrites_existing_object()
    {
        var state = new StorageState(); var store = Store(state); byte[] original = [1, 2, 3, 4, 5];
        await using var source = new MemoryStream(original);
        var metadata = await store.WriteAsync("anh02/exports/fixture.zip", source, original.Length, "application/zip");
        Assert.Equal(Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant(), metadata.Sha256);
        Assert.Equal(original.Length, metadata.SizeBytes); Assert.Equal(1, state.Writes);
        await using var second = new MemoryStream(original);
        var recovery = await store.WriteAsync("anh02/exports/fixture.zip", second, original.Length, "application/zip");
        Assert.Equal(metadata, recovery); Assert.Equal(1, state.Writes);
        await using var wrong = new MemoryStream(new byte[] { 5, 4, 3, 2, 1 });
        await Assert.ThrowsAsync<IOException>(() => store.WriteAsync("anh02/exports/fixture.zip", wrong, original.Length, "application/zip"));
        Assert.Equal(original, state.Bytes); Assert.Equal(1, state.Writes);
    }
    [Fact]
    public async Task Durable_readback_corruption_cannot_return_verified_metadata()
    {
        var state = new StorageState { CorruptReadback = true }; var store = Store(state); await using var source = new MemoryStream(new byte[] { 1, 2, 3 });
        await Assert.ThrowsAsync<IOException>(() => store.WriteAsync("anh02/exports/fixture.zip", source, 3, "application/zip"));
    }
    private static MinioAnh02ArtifactStore Store(StorageState state) => new(Options.Create(new MinioStorageOptions { BucketName = "private-fixture" }), () => new S3Fixture(state));
    private sealed class StorageState
    {
        public byte[]? Bytes { get; set; } public string MediaType { get; set; } = ""; public string Hash { get; set; } = ""; public string Size { get; set; } = ""; public int Writes { get; set; } public bool CorruptReadback { get; init; }
    }
    private sealed class S3Fixture(StorageState state) : AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config { ServiceURL = "https://storage.invalid", ForcePathStyle = true })
    {
        public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            Assert.Equal("private-fixture", request.BucketName); Assert.Equal("*", request.IfNoneMatch);
            if (state.Bytes is not null) throw new AmazonS3Exception("Object already exists.") { StatusCode = System.Net.HttpStatusCode.PreconditionFailed };
            using var memory = new MemoryStream(); await request.InputStream.CopyToAsync(memory, cancellationToken); state.Bytes = memory.ToArray(); state.MediaType = request.ContentType; state.Hash = request.Metadata["anh02-sha256"]; state.Size = request.Metadata["anh02-size"]; state.Writes++; return new();
        }
        public override Task<GetObjectResponse> GetObjectAsync(string bucketName, string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); Assert.Equal("private-fixture", bucketName); Assert.Equal("anh02/exports/fixture.zip", key);
            if (state.Bytes is null) throw new AmazonS3Exception("Object absent.") { StatusCode = System.Net.HttpStatusCode.NotFound };
            var response = new GetObjectResponse { ResponseStream = new MemoryStream(state.CorruptReadback ? [9, 9, 9] : state.Bytes), ContentLength = state.Bytes.Length };
            response.Headers.ContentType = state.MediaType; response.Metadata["anh02-sha256"] = state.Hash; response.Metadata["anh02-size"] = state.Size; return Task.FromResult(response);
        }
    }
}
