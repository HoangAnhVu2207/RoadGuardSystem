using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.Repositories.Concurrency;

namespace RoadGuardSystem.Repositories;

/// <summary>
/// Root Entity Framework Core DbContext for RoadGuard.
/// Loads entity configurations dynamically via ApplyConfigurationsFromAssembly.
/// </summary>
public class RoadGuardDbContext : DbContext
{
    public RoadGuardDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ConsumerEffectReceipt> ConsumerEffectReceipts => Set<ConsumerEffectReceipt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoadGuardDbContext).Assembly);
        RowVersionConvention.Apply(modelBuilder);
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

    private void ValidatePersistenceInvariants()
    {
        if (ChangeTracker.Entries<AuditLog>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "AuditLogs are append-only; corrections require a new audit event.");
        }

        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(entry => entry.State == EntityState.Added))
        {
            if (entry.Entity.BeforeSnapshot is not null)
            {
                entry.Property(audit => audit.BeforeSnapshot).CurrentValue =
                    SensitiveJsonSanitizer.Redact(entry.Entity.BeforeSnapshot);
            }

            if (entry.Entity.AfterSnapshot is not null)
            {
                entry.Property(audit => audit.AfterSnapshot).CurrentValue =
                    SensitiveJsonSanitizer.Redact(entry.Entity.AfterSnapshot);
            }
        }

        foreach (var entry in ChangeTracker.Entries<OutboxMessage>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            entry.Property(message => message.PayloadJson).CurrentValue =
                SensitiveJsonSanitizer.Redact(entry.Entity.PayloadJson);
        }
    }
}
