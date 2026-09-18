using System.Collections.Generic;

namespace RoadGuardSystem.BusinessObjects.Identity;

/// <summary>
/// Configurable options/schema boundaries for session device metadata validation.
/// Ensures limits and sensitive keywords are not hard-coded constants per Data Dictionary Section 3.1.
/// </summary>
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
