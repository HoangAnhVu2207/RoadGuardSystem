using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Messaging;

// Immutable recovery facts. Existing per-clock calendar history is retained separately.
public sealed class WeeklyReviewRecoveryPeriod
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public DateTimeOffset RecoveredAtUtc { get; set; }
    public bool IsLatestAtRecovery { get; set; }
}
public sealed class WeeklyReviewDigest
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RecoveryPeriodId { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public DateTimeOffset RecoveredAtUtc { get; set; }
    public string RecipientKey { get; set; } = "";
    public Guid? RecipientId { get; set; }
    public UserRoleCode RecipientRole { get; set; }
    public ICollection<WeeklyReviewDigestDuty> Duties { get; set; } = [];
}
public sealed class WeeklyReviewDigestDuty
{
    public Guid DigestId { get; set; }
    public Guid ClockId { get; set; }
    public Guid OriginEventId { get; set; }
    public Guid TargetId { get; set; }
    public string Kind { get; set; } = "";
    public DateTimeOffset OriginAtUtc { get; set; }
    public DateTimeOffset DueAtRecoveryUtc { get; set; }
}
