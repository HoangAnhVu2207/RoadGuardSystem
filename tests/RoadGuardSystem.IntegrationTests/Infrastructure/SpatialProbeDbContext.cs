using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories.Spatial;
using RoadGuardSystem.Repositories;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Integration test DbContext inheriting from RoadGuardDbContext.
/// Configures spatial probe mappings and check constraints.
/// Enforces spatial domain validation on SaveChanges.
/// </summary>
public sealed class SpatialProbeDbContext : RoadGuardDbContext
{
    public SpatialProbeDbContext(DbContextOptions<SpatialProbeDbContext> options) : base(options)
    {
    }

    public DbSet<SpatialProbeRecord> SpatialProbes => Set<SpatialProbeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SpatialProbeRecord>(b =>
        {
            b.ToTable("SpatialProbeRecords", t =>
            {
                t.HasCheckConstraint("CK_SpatialProbeRecords_ProjectUtmSrid", "[ProjectUtmSrid] IN (32648, 32649)");
                t.HasCheckConstraint("CK_SpatialProbeRecords_GpsLocation_Srid", "[GpsLocation].[STSrid] = 4326");
                t.HasCheckConstraint("CK_SpatialProbeRecords_EngineeringGeometry_Srid", "[EngineeringGeometry].[STSrid] = [ProjectUtmSrid]");
            });

            b.HasKey(x => x.Id);

            b.Property(x => x.GpsLocation)
                .HasColumnType("geography")
                .IsRequired();

            b.Property(x => x.EngineeringGeometry)
                .HasColumnType("geometry")
                .IsRequired();

            b.Property(x => x.ProjectUtmSrid)
                .IsRequired();

            b.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            b.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetimeoffset(7)")
                .IsRequired();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateSpatialEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ValidateSpatialEntities();
        return base.SaveChanges();
    }

    private void ValidateSpatialEntities()
    {
        foreach (var entry in ChangeTracker.Entries<SpatialProbeRecord>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                SpatialValidation.EnsureGpsGeography(entry.Entity.GpsLocation);
                SpatialValidation.EnsureProjectEngineeringGeometry(
                    entry.Entity.EngineeringGeometry,
                    entry.Entity.ProjectUtmSrid);
            }
        }
    }
}
