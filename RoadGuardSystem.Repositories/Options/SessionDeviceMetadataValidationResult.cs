namespace RoadGuardSystem.Repositories.Options;

public sealed record SessionDeviceMetadataValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static SessionDeviceMetadataValidationResult Success() => new(true);

    public static SessionDeviceMetadataValidationResult Fail(string message) => new(false, message);
}
