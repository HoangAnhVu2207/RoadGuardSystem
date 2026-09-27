namespace RoadGuardSystem.BusinessObjects.Identity;

public sealed class PasswordRecoveryRequest
{
    public Guid Id { get; set; }

    public Guid? TargetUserId { get; set; }

    public DateTimeOffset RequestedAtUtc { get; set; }

    public Guid? CorrelationId { get; set; }
}
