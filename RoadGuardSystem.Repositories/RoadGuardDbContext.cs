using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoadGuardDbContext).Assembly);
    }
}
