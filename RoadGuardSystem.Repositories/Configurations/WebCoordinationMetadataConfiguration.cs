using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class WebCoordinationMetadataConfiguration : IEntityTypeConfiguration<Project>, IEntityTypeConfiguration<ApplicationUser>,
    IEntityTypeConfiguration<RepairPackage>, IEntityTypeConfiguration<RepairItem>
{
    public void Configure(EntityTypeBuilder<Project> builder) => builder.Property(x => x.Locality).HasMaxLength(500);
    public void Configure(EntityTypeBuilder<ApplicationUser> builder) => builder.Property(x => x.Organization).HasMaxLength(255);
    public void Configure(EntityTypeBuilder<RepairPackage> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.PlannedDueAt).HasColumnType("datetimeoffset(7)");
    }
    public void Configure(EntityTypeBuilder<RepairItem> builder)
    {
        builder.Property(x => x.PlannedQuantity).HasPrecision(18, 6);
        builder.Property(x => x.PlannedUnit).HasMaxLength(32);
        builder.Property(x => x.PlannedMaterials).HasMaxLength(4000);
        builder.ToTable("RepairItems", table => table.HasCheckConstraint("CK_RepairItems_PlannedQuantity",
            "([PlannedQuantity] IS NULL AND [PlannedUnit] IS NULL) OR ([PlannedQuantity] IS NOT NULL AND [PlannedQuantity] > 0 AND [PlannedUnit] IS NOT NULL)"));
    }
}
