using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Devices;

public sealed class DroneDevice
{
    private DroneDevice()
    {
    }

    public Guid Id { get; private set; }

    public string SerialNo { get; private set; } = string.Empty;

    public string? Model { get; private set; }

    public DroneDeviceStatus Status { get; private set; }

    public string? ChecklistVersion { get; private set; }

    public static DroneDevice Create(
        Guid id,
        string serialNo,
        DroneDeviceStatus status,
        string? model = null,
        string? checklistVersion = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Drone device id must not be empty.", nameof(id));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Drone device status is invalid.");
        }

        return new DroneDevice
        {
            Id = id,
            SerialNo = ValidateRequired(serialNo, nameof(serialNo), 120),
            Status = status,
            Model = NormalizeOptional(model, nameof(model), 120),
            ChecklistVersion = NormalizeOptional(checklistVersion, nameof(checklistVersion), 50)
        };
    }

    private static string ValidateRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }
}
