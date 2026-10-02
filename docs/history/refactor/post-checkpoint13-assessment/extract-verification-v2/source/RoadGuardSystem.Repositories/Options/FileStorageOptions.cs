namespace RoadGuardSystem.Repositories.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = string.Empty;

    public long MaximumSizeBytes { get; set; }
}
