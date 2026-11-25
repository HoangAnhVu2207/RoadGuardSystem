using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Options;
using Amazon.S3;
using Amazon.S3.Model;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal sealed class OwnedWebMinio(IContainer container, string access, string secret) : IAsyncDisposable
{
    public static async Task<OwnedWebMinio> StartAsync(bool createBucket = false)
    {
        var image = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE")
            ?? throw new InvalidOperationException("RG_MINIO_IMAGE must identify the approved disposable MinIO image.");
        var access = "sbx" + Guid.NewGuid().ToString("N");
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var container = new ContainerBuilder(image).WithLabel("roadguard.sandbox.mode", "web-pm-test")
            .WithEnvironment("MINIO_ROOT_USER", access).WithEnvironment("MINIO_ROOT_PASSWORD", secret)
            .WithCommand("server", "/data").WithPortBinding(9000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                request => request.ForPort(9000).ForPath("/minio/health/ready"),
                strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))).Build();
        try
        {
            await container.StartAsync();
            using var s3 = new AmazonS3Client(access, secret, new AmazonS3Config
            {
                ServiceURL = $"http://{container.Hostname}:{container.GetMappedPublicPort(9000)}",
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1"
            });
            if (createBucket)
                await s3.PutBucketAsync(new PutBucketRequest { BucketName = "web-pm-" + access });
            return new(container, access, secret);
        }
        catch { await container.DisposeAsync(); throw; }
    }

    public void Configure(IServiceCollection services) => services.PostConfigure<MinioStorageOptions>(options =>
    {
        options.Endpoint = $"http://{container.Hostname}:{container.GetMappedPublicPort(9000)}";
        options.PublicEndpoint = options.Endpoint;
        options.BucketName = "web-pm-" + access;
        options.AccessKey = access;
        options.SecretKey = secret;
        options.UseSsl = false;
    });

    public ValueTask DisposeAsync() => container.DisposeAsync();

    public static async Task WriteVerifiedObject(IServiceProvider services, string key, string mediaType, byte[] bytes)
    {
        await using var scope = services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<RoadGuardSystem.Repositories.Storage.IUploadObjectStorage>();
        var multipart = await storage.InitiateAsync(key, mediaType);
        var url = (await storage.PresignPartsAsync(key, multipart, [1], DateTimeOffset.UtcNow.AddMinutes(5)))[0];
        using var client = new HttpClient(); using var body = new ByteArrayContent(bytes);
        using var uploaded = await client.PutAsync(url.Url, body); uploaded.EnsureSuccessStatusCode();
        var verified = await storage.CompleteAndVerifyAsync(key, multipart,
            [new(1, uploaded.Headers.ETag!.Tag)]);
        Xunit.Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), verified.ChecksumSha256);
        Xunit.Assert.Equal(bytes.Length, verified.SizeBytes);
    }
}
