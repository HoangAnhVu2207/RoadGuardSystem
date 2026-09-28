using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RoadSegmentSetConfiguration : IEntityTypeConfiguration<RoadSegmentSet>
{
    public void Configure(EntityTypeBuilder<RoadSegmentSet> builder)
    {
        builder.ToTable("RoadSegmentSets", table => table.HasCheckConstraint("CK_RoadSegmentSets_Status", "[Status] IN ('DRAFT','PUBLISHED','SUPERSEDED')"));
        builder.HasKey(set => set.Id);
        builder.Property(set => set.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(set => set.RoadSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(set => set.Status).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.HasIndex(set => new { set.RoadSectionVersionId, set.Status }).HasDatabaseName("IX_RoadSegmentSets_RouteVersion_Status");
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(set => set.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
