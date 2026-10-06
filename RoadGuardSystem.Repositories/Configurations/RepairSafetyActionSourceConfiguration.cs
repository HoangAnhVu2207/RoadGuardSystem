using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RepairSafetyActionSourceConfiguration : IEntityTypeConfiguration<RepairSafetyActionSource>
{
    public void Configure(EntityTypeBuilder<RepairSafetyActionSource> builder)
    {
        builder.ToTable("RepairSafetyActionSources", table => table.HasTrigger("TR_RepairSafetyActionSources_Immutable"));
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.Kind).HasMaxLength(32);
        builder.Property(row => row.Reason).HasMaxLength(2000);
        builder.HasOne<TemporarySafetyMeasure>().WithMany().HasForeignKey(row => row.MeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairItem>().WithMany().HasForeignKey(row => row.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RepairFieldTaskBinding>().WithMany().HasForeignKey(row => row.BindingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(row => new { row.MeasureId, row.Kind, row.OriginId }).IsUnique();
    }
}
