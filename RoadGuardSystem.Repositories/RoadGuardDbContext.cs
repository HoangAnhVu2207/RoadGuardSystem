using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Spatial;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.Repositories.Concurrency;

using Microsoft.Extensions.Options;

namespace RoadGuardSystem.Repositories;

/// <summary>
/// Root Entity Framework Core DbContext for RoadGuard.
/// Loads entity configurations dynamically via ApplyConfigurationsFromAssembly.
/// </summary>
public class RoadGuardDbContext : DbContext
{
    private readonly SessionDeviceMetadataOptions _sessionMetadataOptions;

    public RoadGuardDbContext(
        DbContextOptions options,
        IOptions<SessionDeviceMetadataOptions>? sessionMetadataOptions = null)
        : base(options)
    {
        _sessionMetadataOptions = sessionMetadataOptions?.Value ?? SessionDeviceMetadataValidator.DefaultOptions;
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ConsumerEffectReceipt> ConsumerEffectReceipts => Set<ConsumerEffectReceipt>();

    public DbSet<Notification> Notifications => Set<Notification>();

    // Identity and session aggregates (P2-10)
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();

    public DbSet<UserSession> Sessions => Set<UserSession>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetLog> PasswordResetLogs => Set<PasswordResetLog>();

    public DbSet<AccountStatusChangeLog> AccountStatusChangeLogs => Set<AccountStatusChangeLog>();

    public DbSet<StoredFile> Files => Set<StoredFile>();

    public DbSet<DefectType> DefectTypes => Set<DefectType>();

    public DbSet<CauseCategory> CauseCategories => Set<CauseCategory>();

    public DbSet<SeverityRuleVersion> SeverityRuleVersions => Set<SeverityRuleVersion>();

    public DbSet<Defect> Defects => Set<Defect>();

    public DbSet<DroneDevice> DroneDevices => Set<DroneDevice>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<RoadSection> RoadSections => Set<RoadSection>();

    public DbSet<RoadSectionVersion> RoadSectionVersions => Set<RoadSectionVersion>();

    public DbSet<Warranty> Warranties => Set<Warranty>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<HandoverDocument> HandoverDocuments => Set<HandoverDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoadGuardDbContext).Assembly);
        RowVersionConvention.Apply(modelBuilder);

        // Deferred FK from P2-02 AuditLogs to P2-10 Users
        modelBuilder.Entity<AuditLog>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(audit => audit.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override int SaveChanges()
    {
        ValidatePersistenceInvariants();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidatePersistenceInvariants();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidatePersistenceInvariants();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidatePersistenceInvariants();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private readonly AsyncLocal<bool> _roleMutationPermitted = new();

    /// <summary>
    /// Grants permission for authoritative atomic role mutation within the current async execution context.
    /// Direct changes outside this scope throw an InvalidOperationException.
    /// </summary>
    internal IDisposable PermitRoleMutationScope()
    {
        _roleMutationPermitted.Value = true;
        return new RoleMutationScope(this);
    }

    private sealed class RoleMutationScope : IDisposable
    {
        private readonly RoadGuardDbContext _context;
        public RoleMutationScope(RoadGuardDbContext context) => _context = context;
        public void Dispose() => _context._roleMutationPermitted.Value = false;
    }

    private static void ValidateSafeSecurityLogValue(
        string? value,
        string entityName,
        string fieldName,
        bool optional,
        Func<string, bool> isAllowed)
    {
        if (value is null && optional)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value) || !isAllowed(value))
        {
            throw new InvalidOperationException(
                $"{entityName}.{fieldName} contains forbidden sensitive data or is not an approved safe code.");
        }
    }

    private void ValidatePersistenceInvariants()
    {
        foreach (var entry in ChangeTracker.Entries<Project>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified &&
                entry.Entity.EngineeringUtmSrid is int srid &&
                !SpatialConstants.IsAllowedProjectUtmSrid(srid))
            {
                throw new InvalidOperationException("Project EngineeringUtmSrid must be 32648 or 32649 when configured.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<RoadSectionVersion>())
        {
            if (entry.State == EntityState.Modified &&
                entry.Properties.Any(property => property.IsModified && property.Metadata.Name != nameof(RoadSectionVersion.IsCurrent)))
            {
                throw new InvalidOperationException(
                    "RoadSectionVersion is immutable; only the current-version marker may change through its transaction boundary.");
            }
        }

        var storedFileChanges = ChangeTracker.Entries<StoredFile>();
        if (storedFileChanges.Any(entry => entry.State == EntityState.Modified))
        {
            throw new InvalidOperationException(
                "Stored file metadata and content identity are immutable; create a new file identity instead.");
        }

        if (storedFileChanges.Any(entry => entry.State == EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Stored files cannot be deleted through the generic persistence path; retention policy must authorize deletion.");
        }

        // 1. AuditLog append-only
        if (ChangeTracker.Entries<AuditLog>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "AuditLogs are append-only; corrections require a new audit event.");
        }

        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(entry => entry.State == EntityState.Added))
        {
            entry.Entity.SanitizeSnapshotsForPersistence();
        }

        // 2. OutboxMessage sanitization
        foreach (var entry in ChangeTracker.Entries<OutboxMessage>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            entry.Property(message => message.PayloadJson).CurrentValue =
                SensitiveJsonSanitizer.Redact(entry.Entity.PayloadJson);
        }

        // 3. UserSession write-once invariants
        foreach (var entry in ChangeTracker.Entries<UserSession>())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Property(s => s.UserId).IsModified)
                {
                    throw new InvalidOperationException("UserSession.UserId is write-once and cannot be modified.");
                }
                if (entry.Property(s => s.IssuedAt).IsModified)
                {
                    throw new InvalidOperationException("UserSession.IssuedAt is write-once and cannot be modified.");
                }
                if (entry.Property(s => s.DeviceMetadataJson).IsModified)
                {
                    throw new InvalidOperationException("UserSession.DeviceMetadataJson is write-once and cannot be modified.");
                }
            }
            else if (entry.State == EntityState.Added)
            {
                var validation = SessionDeviceMetadataValidator.Validate(entry.Entity.DeviceMetadataJson, _sessionMetadataOptions);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException($"Invalid Session DeviceMetadataJson: {validation.ErrorMessage}");
                }

                if (entry.Entity.ExpiresAt <= entry.Entity.IssuedAt)
                {
                    throw new InvalidOperationException("Session ExpiresAt must be after IssuedAt.");
                }

                if (entry.Entity.RevokedAt != null && entry.Entity.RevokedAt < entry.Entity.IssuedAt)
                {
                    throw new InvalidOperationException("Session RevokedAt cannot be before IssuedAt.");
                }
            }
        }

        // 4. RefreshToken validation
        foreach (var entry in ChangeTracker.Entries<RefreshToken>().Where(e => e.State == EntityState.Added))
        {
            if (string.IsNullOrWhiteSpace(entry.Entity.TokenHash) || entry.Entity.TokenHash.Trim().Length < 32)
            {
                throw new InvalidOperationException("RefreshToken.TokenHash must be a valid non-empty cryptographic hash (at least 32 characters).");
            }

            if (entry.Entity.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("RefreshToken.ExpiresAt must be in the future when created.");
            }
        }

        // 5. PasswordResetLog append-only & sensitive data check
        if (ChangeTracker.Entries<PasswordResetLog>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "PasswordResetLogs are append-only; updates and deletions are forbidden.");
        }

        foreach (var entry in ChangeTracker.Entries<PasswordResetLog>().Where(e => e.State == EntityState.Added))
        {
            ValidateSafeSecurityLogValue(
                entry.Entity.Reason,
                nameof(PasswordResetLog),
                nameof(PasswordResetLog.Reason),
                optional: true,
                SecurityLogSafeValueCodes.IsAllowedReason);
            ValidateSafeSecurityLogValue(
                entry.Entity.Source,
                nameof(PasswordResetLog),
                nameof(PasswordResetLog.Source),
                optional: false,
                SecurityLogSafeValueCodes.IsAllowedSource);
        }

        // 6. AccountStatusChangeLog append-only, state & sensitive data validation
        if (ChangeTracker.Entries<AccountStatusChangeLog>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "AccountStatusChangeLogs are append-only; updates and deletions are forbidden.");
        }

        foreach (var entry in ChangeTracker.Entries<AccountStatusChangeLog>().Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity.FromStatus == entry.Entity.ToStatus)
            {
                throw new InvalidOperationException("AccountStatusChangeLog FromStatus and ToStatus must be different.");
            }

            if (string.IsNullOrWhiteSpace(entry.Entity.Reason))
            {
                throw new InvalidOperationException("AccountStatusChangeLog Reason is required.");
            }

            ValidateSafeSecurityLogValue(
                entry.Entity.Reason,
                nameof(AccountStatusChangeLog),
                nameof(AccountStatusChangeLog.Reason),
                optional: false,
                SecurityLogSafeValueCodes.IsAllowedReason);
            ValidateSafeSecurityLogValue(
                entry.Entity.Source,
                nameof(AccountStatusChangeLog),
                nameof(AccountStatusChangeLog.Source),
                optional: false,
                SecurityLogSafeValueCodes.IsAllowedSource);
        }

        // 7. ApplicationUser validation & atomic role change boundary guard
        foreach (var entry in ChangeTracker.Entries<ApplicationUser>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.RoleCode == UserRoleCode.Unknown)
                {
                    throw new InvalidOperationException("ApplicationUser RoleCode cannot be Unknown.");
                }
                if (entry.Entity.Status == UserStatus.Unknown)
                {
                    throw new InvalidOperationException("ApplicationUser Status cannot be Unknown.");
                }
            }

            var canonicalNormalized = entry.Entity.UserName?.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(entry.Entity.NormalizedUserName) &&
                !string.Equals(entry.Entity.NormalizedUserName, canonicalNormalized, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"ApplicationUser.NormalizedUserName ('{entry.Entity.NormalizedUserName}') must match the canonical upper-invariant normalization of UserName ('{canonicalNormalized}').");
            }
            entry.Entity.NormalizedUserName = canonicalNormalized!;

            if (entry.State == EntityState.Modified && entry.Property(u => u.RoleCode).IsModified && !_roleMutationPermitted.Value)
            {
                throw new InvalidOperationException("Direct modification of ApplicationUser.RoleCode is forbidden. Role mutations must execute through the atomic role change boundary (IIdentityRepository.ChangeUserRoleAtomicAsync).");
            }
        }
    }
}
