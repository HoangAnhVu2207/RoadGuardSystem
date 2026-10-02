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
        builder.Property(set => set.RowVersion).IsRowVersion();
        builder.Property(set => set.GeometryHash).HasMaxLength(64);
        builder.HasAlternateKey(set => new { set.Id, set.RoadSectionVersionId });
        builder.HasIndex(set => set.RoadSectionVersionId).IsUnique().HasFilter("[Status] = 'PUBLISHED'").HasDatabaseName("UX_RoadSegmentSets_CurrentPublished");
        builder.HasIndex(set => new { set.RoadSectionVersionId, set.Status }).HasDatabaseName("IX_RoadSegmentSets_RouteVersion_Status");
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(set => set.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
