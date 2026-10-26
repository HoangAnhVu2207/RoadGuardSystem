using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.UnitTests.Files;

public sealed class HandoffPresignTests
{
    [Theory]
    [InlineData("http://localhost:19001", "localhost", 19001)]
    [InlineData("", "minio", 9000)]
    public async Task PresignUsesConfiguredPublicAddressWithoutChangingInternalEndpoint(string publicEndpoint, string host, int port)
    {
        var options = new MinioStorageOptions
        {
            Endpoint = "http://minio:9000",
            PublicEndpoint = publicEndpoint,
            AccessKey = "synthetic",
            SecretKey = "synthetic-secret-for-offline-signing",
            BucketName = "private"
        };
        var adapter = new MinioUploadObjectStorage(Options.Create(options));
        var result = await adapter.PresignPartsAsync("object", "attempt", new[] { 1 }, DateTimeOffset.UtcNow.AddMinutes(5));
        var address = new Uri(Assert.Single(result).Url);
        Assert.Equal(host, address.Host);
        Assert.Equal(port, address.Port);
        Assert.Equal("http://minio:9000", options.Endpoint);
    }
}
