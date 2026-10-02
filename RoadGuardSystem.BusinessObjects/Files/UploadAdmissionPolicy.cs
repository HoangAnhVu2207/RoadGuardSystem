namespace RoadGuardSystem.BusinessObjects.Files;

public static class UploadAdmissionPolicy
{
    public const long PhotoMaxBytes = 20L * 1024 * 1024;
    public const long VideoMaxBytes = 8L * 1024 * 1024 * 1024;
    public const long TelemetryMaxBytes = 10L * 1024 * 1024;
    public const long DatasetMaxBytes = 32L * 1024 * 1024 * 1024;

    public static long? MaximumBytes(string purpose, string mediaType)
    {
        var mime = mediaType.Trim().ToLowerInvariant();
        return purpose.Trim().ToUpperInvariant() switch
        {
            "REPORT_PHOTO" or "BEFORE" or "AFTER" or "MEASUREMENT" when mime is "image/jpeg" or "image/png" => PhotoMaxBytes,
            "SURVEY_VIDEO" when mime == "video/mp4" => VideoMaxBytes,
            "TELEMETRY" when mime == "application/x-subrip" => TelemetryMaxBytes,
            // These purposes retain the existing int ceiling; PR-38 assigns no new limit.
            "DOCUMENT" or "ROUTE_SOURCE" => int.MaxValue,
            _ => null
        };
    }
}
