using System;
using System.Collections.Generic;

namespace RoadGuardSystem.BusinessObjects.Identity;

public class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public string? DeviceMetadataJson { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public SessionLifecycle Lifecycle { get; set; } = SessionLifecycle.LegacyBounded;

    public RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode? IssuedRole { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public SessionTransport Transport { get; set; } = SessionTransport.LegacyBearer;

    public DateTimeOffset? LastActivityAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Derived states - not stored in database
    public bool IsActiveAt(DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        if (RevokedAt != null) return false;
        if (Lifecycle == SessionLifecycle.PersistentRenewable) return ExpiresAt is null && IssuedRole is { } role &&
            role != RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Unknown && Enum.IsDefined(role);
        if (Lifecycle != SessionLifecycle.LegacyBounded || ExpiresAt is null || ExpiresAt <= utcNow) return false;
        return Transport != SessionTransport.Web || (LastActivityAt ?? IssuedAt).AddMinutes(30) > utcNow;
    }

    public bool IsRevoked => RevokedAt != null;

    public bool IsExpiredAt(DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        return RevokedAt == null && !IsActiveAt(utcNow);
    }

    public void Touch(DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        if (!IsActiveAt(utcNow)) throw new InvalidOperationException("An inactive session cannot be touched.");
        if (Transport == SessionTransport.Web) LastActivityAt = utcNow;
    }

    // Navigations
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
