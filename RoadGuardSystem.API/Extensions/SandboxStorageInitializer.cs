using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;

namespace RoadGuardSystem.API.Extensions;

/// <summary>Creates only the sandbox's private bucket; never rewrites data or an existing policy.</summary>
internal sealed class SandboxStorageInitializer(IOptions<MinioStorageOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var storage = options.Value;
        if (storage.Endpoint != "http://minio:9000" || storage.BucketName != "roadguard-private")
            throw new InvalidOperationException("Sandbox storage bootstrap requires the fixed local endpoint and private bucket.");
        using var client = new AmazonS3Client(new BasicAWSCredentials(storage.AccessKey, storage.SecretKey),
            new AmazonS3Config { ServiceURL = storage.Endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        try
        {
            await client.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = storage.BucketName }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = storage.BucketName }, cancellationToken);
        }
        try
        {
            var policy = await client.GetBucketPolicyAsync(new GetBucketPolicyRequest { BucketName = storage.BucketName }, cancellationToken);
            // AWSSDK.S3 4.0 returns this 404 response/body instead of throwing on the verified MinIO image.
            if (policy.HttpStatusCode == System.Net.HttpStatusCode.NotFound &&
                System.Xml.Linq.XDocument.Parse(policy.Policy).Root?.Element("Code")?.Value == "NoSuchBucketPolicy")
                return;
            if (policy.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException("Sandbox bucket policy could not be verified; startup refused.");
            if (!string.IsNullOrWhiteSpace(policy.Policy))
            {
                using var document = System.Text.Json.JsonDocument.Parse(policy.Policy);
                if (!document.RootElement.TryGetProperty("Statement", out var statements) ||
                    statements.ValueKind != System.Text.Json.JsonValueKind.Array || statements.GetArrayLength() != 0)
                    throw new InvalidOperationException("Sandbox bucket has an existing nonempty policy; review it before startup. No policy was changed.");
            }
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "NoSuchBucketPolicy") { }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
