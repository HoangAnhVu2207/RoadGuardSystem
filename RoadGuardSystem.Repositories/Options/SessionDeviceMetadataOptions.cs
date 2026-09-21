namespace RoadGuardSystem.Repositories.Options;

public sealed class SessionDeviceMetadataOptions
{
    public const string SectionName = "SessionDeviceMetadata";

    public int MaxDeviceIdLength { get; set; } = 100;

    public int MaxPlatformLength { get; set; } = 50;

    public int MaxAppVersionLength { get; set; } = 50;

    public IReadOnlyCollection<string> SensitiveKeywords { get; set; } =
    [
        "password",
        "secret",
        "bearer",
        "access_token",
        "refresh_token"
    ];
}
