namespace RoadGuardSystem.Repositories.Options;

public sealed class MinioStorageOptions
{
    public const string SectionName = "MinioStorage";

    public string Endpoint { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
}
