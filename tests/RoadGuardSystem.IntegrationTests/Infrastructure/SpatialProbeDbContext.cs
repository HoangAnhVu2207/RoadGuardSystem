using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Integration test DbContext inheriting from RoadGuardDbContext.
/// Used to verify database schema creation and spatial round-trips without polluting production models.
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
            b.ToTable("SpatialProbeRecords");
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
}
