using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Concurrency;

namespace RoadGuardSystem.BusinessObjects.Identity;

/// <summary>
/// Represents an application user, extending ASP.NET Core Identity.
/// Mapped to the User concept in the Data Dictionary section 3.1.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IHasRowVersion
{
    public string DisplayName { get; set; } = string.Empty;

    public UserRoleCode RoleCode { get; set; } = UserRoleCode.Unknown;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? SuspendedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public byte[] RowVersion { get; set; } = [];

    // Navigations
    public virtual ApplicationRole? Role { get; set; }

    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    public virtual ICollection<PasswordResetLog> TargetPasswordResetLogs { get; set; } = new List<PasswordResetLog>();

    public virtual ICollection<AccountStatusChangeLog> TargetAccountStatusChangeLogs { get; set; } = new List<AccountStatusChangeLog>();
}
