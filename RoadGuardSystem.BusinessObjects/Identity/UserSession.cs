using System;
using System.Collections.Generic;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public string? DeviceMetadataJson { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Derived states - not stored in database
    public bool IsActiveAt(DateTimeOffset now) => RevokedAt == null && ExpiresAt > now.ToUniversalTime();

    public bool IsRevoked => RevokedAt != null;

    public bool IsExpiredAt(DateTimeOffset now) => RevokedAt == null && ExpiresAt <= now.ToUniversalTime();

    // Navigations
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
