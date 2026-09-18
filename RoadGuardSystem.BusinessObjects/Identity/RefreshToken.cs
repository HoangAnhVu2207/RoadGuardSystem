using System;
using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class RefreshToken : IHasRowVersion
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Derived states
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;

    public bool IsRevoked => RevokedAt != null;

    public bool IsExpired => RevokedAt == null && ExpiresAt <= DateTimeOffset.UtcNow;

    // Navigation
    public virtual UserSession Session { get; set; } = null!;
}
