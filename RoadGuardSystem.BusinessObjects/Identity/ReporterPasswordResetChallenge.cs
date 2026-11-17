namespace RoadGuardSystem.BusinessObjects.Identity;

public sealed class ReporterPasswordResetChallenge
{
    public Guid UserId { get; set; }
    public string OtpHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset NextSendAt { get; set; }
    public DateTimeOffset WindowStartedAt { get; set; }
    public int SendsInWindow { get; set; }
    public int FailedAttempts { get; set; }
    public bool Consumed { get; set; }
}
