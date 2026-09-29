using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

public sealed class MinioSmokeFactAttribute : FactAttribute
{
    public MinioSmokeFactAttribute()
    {
        if (!IsConfigured())
        {
            Skip = "Set ROADGUARD_MINIO_SMOKE=1 and MinioStorage__* variables to run the MinIO provider smoke test.";
        }
    }

    public static bool IsConfigured()
        => string.Equals(Environment.GetEnvironmentVariable("ROADGUARD_MINIO_SMOKE"), "1", StringComparison.Ordinal) &&
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MinioStorage__Endpoint")) &&
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MinioStorage__BucketName")) &&
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MinioStorage__AccessKey")) &&
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("MinioStorage__SecretKey"));
}
