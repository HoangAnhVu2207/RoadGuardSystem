using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

public sealed class Anh01StorageRecoveryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedObject_TransientReadFailuresRemainStorageUnavailable(bool fallback)
    {
        var client = new StorageClient(fallback);
        var storage = new MinioUploadObjectStorage(Options.Create(new MinioStorageOptions()), () => client);
        var act = () => storage.CompleteAndVerifyAsync("object", "multipart", [new(1, "etag")]);
        await act.Should().ThrowAsync<FileStorageException>();
    }

    private sealed class StorageClient(bool fallback) : AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config { ServiceURL = "https://storage.invalid", ForcePathStyle = true })
    {
        public override Task<CompleteMultipartUploadResponse> CompleteMultipartUploadAsync(CompleteMultipartUploadRequest request, CancellationToken cancellationToken = default)
            => fallback ? Task.FromException<CompleteMultipartUploadResponse>(new AmazonS3Exception("complete already committed") { ErrorCode = "NoSuchUpload" }) : Task.FromResult(new CompleteMultipartUploadResponse());
        public override Task<GetObjectMetadataResponse> GetObjectMetadataAsync(GetObjectMetadataRequest request, CancellationToken cancellationToken = default)
            => Task.FromException<GetObjectMetadataResponse>(new AmazonS3Exception("temporary read outage"));
        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default)
            => Task.FromException<GetObjectResponse>(new AmazonS3Exception("temporary read outage"));
    }
}
