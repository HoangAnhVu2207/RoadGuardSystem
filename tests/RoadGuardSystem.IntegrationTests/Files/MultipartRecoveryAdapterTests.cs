using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("Package", "HUY-01")]
public sealed class MultipartRecoveryAdapterTests
{
    [Fact]
    public async Task ExactKeyListing_ExhaustsBothMarkers_ExcludesPrefixNeighbors_AndChecksPartPagination()
    {
        var client = new RecoveryClient();
        var storage = new MinioUploadObjectStorage(Options.Create(new MinioStorageOptions { BucketName = "fixture-only" }), () => client);
        (await storage.ListMultipartIdsAsync("owned")).Should().Equal("a", "b");
        client.ListCalls.Should().Be(2); client.Marker.Should().Be("owned/a");
        (await storage.HasPartsAsync("owned", "a")).Should().BeTrue(); client.PartCalls.Should().Be(2);
        await storage.AbortMultipartAsync("owned", "a"); client.AbortCalls.Should().Be(1);
    }

    [Theory]
    [InlineData("stalled")]
    [InlineData("unavailable")]
    [InlineData("abort")]
    public async Task StorageFailures_AreUnavailable_WithoutPretendingCleanupSucceeded(string fault)
    {
        var client = new RecoveryClient { Fault = fault };
        var storage = new MinioUploadObjectStorage(Options.Create(new MinioStorageOptions()), () => client);
        Func<Task> call = fault == "abort" ? () => storage.AbortMultipartAsync("owned", "a") : async () => { await storage.ListMultipartIdsAsync("owned"); };
        await call.Should().ThrowAsync<FileStorageException>();
    }

    private sealed class RecoveryClient() : AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config { ServiceURL = "https://storage.invalid", ForcePathStyle = true })
    {
        public string? Fault { get; init; }
        public int ListCalls { get; private set; }
        public int PartCalls { get; private set; }
        public int AbortCalls { get; private set; }
        public string? Marker { get; private set; }
        public override Task<ListMultipartUploadsResponse> ListMultipartUploadsAsync(ListMultipartUploadsRequest request, CancellationToken cancellationToken = default)
        {
            ListCalls++; request.Prefix.Should().Be("owned");
            if (Fault == "unavailable") throw new AmazonS3Exception("Synthetic provider outage");
            if (Fault == "stalled") return Task.FromResult(new ListMultipartUploadsResponse { IsTruncated = true });
            Marker = $"{request.KeyMarker}/{request.UploadIdMarker}";
            return Task.FromResult(new ListMultipartUploadsResponse { IsTruncated = ListCalls == 1, NextKeyMarker = "owned", NextUploadIdMarker = "a",
                MultipartUploads = ListCalls == 1 ? [new() { Key = "owned", UploadId = "a" }, new() { Key = "owned-neighbor", UploadId = "foreign" }] : [new() { Key = "owned", UploadId = "b" }] });
        }
        public override Task<ListPartsResponse> ListPartsAsync(ListPartsRequest request, CancellationToken cancellationToken = default)
        { PartCalls++; if (PartCalls == 2) request.PartNumberMarker.Should().Be("5"); return Task.FromResult(new ListPartsResponse { IsTruncated = PartCalls == 1, NextPartNumberMarker = 5, Parts = PartCalls == 1 ? [] : [new() { PartNumber = 6 }] }); }
        public override Task<AbortMultipartUploadResponse> AbortMultipartUploadAsync(AbortMultipartUploadRequest request, CancellationToken cancellationToken = default)
        { AbortCalls++; throw new AmazonS3Exception("Synthetic abort") { ErrorCode = Fault == "abort" ? "Unavailable" : "NoSuchUpload" }; }
    }
}
