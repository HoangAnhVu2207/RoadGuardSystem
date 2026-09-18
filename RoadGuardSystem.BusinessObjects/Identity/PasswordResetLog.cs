using System;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class PasswordResetLog
{
    public Guid Id { get; set; }

    public Guid TargetUserId { get; set; }

    public Guid? PerformedByUserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string? Reason { get; set; }

    public PasswordResetResult Result { get; set; }

    public string Source { get; set; } = string.Empty;

    public Guid? CorrelationId { get; set; }

    // Navigations
    public virtual ApplicationUser TargetUser { get; set; } = null!;

    public virtual ApplicationUser? PerformedByUser { get; set; }
}
