namespace RoadGuardSystem.Repositories.Options;

public sealed class UploadSessionOptions
{
    public const string SectionName = "UploadSession";

    public int PartSizeBytes { get; set; } = 8 * 1024 * 1024;
    public int SessionLifetimeHours { get; set; } = 24;
    public int SignedPutUrlLifetimeMinutes { get; set; } = 15;
    public bool RecoveryEnabled { get; set; }
    public int RecoveryDeadlineSeconds { get; set; } = 120;
    public int RecoveryRetrySeconds { get; set; } = 30;
    public int RecoveryBatchSize { get; set; } = 20;
}
