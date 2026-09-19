using System;
using System.Collections.Generic;
using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class UserSession : IHasRowVersion
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public string? DeviceMetadataJson { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Derived states - not stored in database
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;

    public bool IsRevoked => RevokedAt != null;

    public bool IsExpired => RevokedAt == null && ExpiresAt <= DateTimeOffset.UtcNow;

    // Navigations
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
