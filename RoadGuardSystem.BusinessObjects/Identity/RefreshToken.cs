using System;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Derived states
    public bool IsActiveAt(DateTimeOffset now) => RevokedAt == null && ExpiresAt > now.ToUniversalTime();

    public bool IsRevoked => RevokedAt != null;

    public bool IsExpiredAt(DateTimeOffset now) => RevokedAt == null && ExpiresAt <= now.ToUniversalTime();

    // Navigation
    public virtual UserSession Session { get; set; } = null!;
}
