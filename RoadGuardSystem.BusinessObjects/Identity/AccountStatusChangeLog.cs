using System;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class AccountStatusChangeLog
{
    public Guid Id { get; set; }

    public Guid TargetUserId { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public UserStatus FromStatus { get; set; }

    public UserStatus ToStatus { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public Guid? CorrelationId { get; set; }

    public Guid? HandoverReference { get; set; }

    // Navigations
    public virtual ApplicationUser TargetUser { get; set; } = null!;

    public virtual ApplicationUser? ChangedByUser { get; set; }
}
