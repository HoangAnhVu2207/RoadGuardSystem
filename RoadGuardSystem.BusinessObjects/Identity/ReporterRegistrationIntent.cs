namespace RoadGuardSystem.BusinessObjects.Identity;

using RoadGuardSystem.aBusinessObjects.Commons;

public sealed class ReporterRegistrationIntent
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string NormalizedEmail { get; set; } = string.Empty;

    public ReporterType ReporterType { get; set; }

    public string OtpHash { get; set; } = string.Empty;

    public int OtpGeneration { get; set; }

    public int FailedAttempts { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset ResendAvailableAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset? EmailConfirmedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
