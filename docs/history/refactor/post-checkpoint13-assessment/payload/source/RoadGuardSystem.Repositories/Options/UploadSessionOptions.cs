namespace RoadGuardSystem.Repositories.Options;

public sealed class UploadSessionOptions
{
    public const string SectionName = "UploadSession";

    public int PartSizeBytes { get; set; } = 8 * 1024 * 1024;
    public int SessionLifetimeHours { get; set; } = 24;
    public int SignedPutUrlLifetimeMinutes { get; set; } = 15;
}
