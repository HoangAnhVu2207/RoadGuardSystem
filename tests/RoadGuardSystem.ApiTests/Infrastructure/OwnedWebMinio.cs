using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using RoadGuardSystem.Repositories.Options;

namespace RoadGuardSystem.ApiTests.Infrastructure;

internal sealed class OwnedWebMinio(IContainer container, string access, string secret) : IAsyncDisposable
{
    public static async Task<OwnedWebMinio> StartAsync()
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
        try { await container.StartAsync(); return new(container, access, secret); }
        catch { await container.DisposeAsync(); throw; }
    }

    public void Configure(IServiceCollection services) => services.PostConfigure<MinioStorageOptions>(options =>
    {
        options.Endpoint = $"http://{container.Hostname}:{container.GetMappedPublicPort(9000)}";
        options.BucketName = "web-pm-" + access;
        options.AccessKey = access;
        options.SecretKey = secret;
        options.UseSsl = false;
    });

    public ValueTask DisposeAsync() => container.DisposeAsync();
}
