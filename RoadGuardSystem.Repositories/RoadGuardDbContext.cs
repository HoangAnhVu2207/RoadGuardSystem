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
        ValidateAppendOnlyAudit();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateAppendOnlyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateAppendOnlyAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateAppendOnlyAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateAppendOnlyAudit()
    {
        if (ChangeTracker.Entries<AuditLog>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "AuditLogs are append-only; corrections require a new audit event.");
        }
    }
}
