using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RoadSegmentConfiguration : IEntityTypeConfiguration<RoadSegment>
{
    public void Configure(EntityTypeBuilder<RoadSegment> builder)
    {
        builder.ToTable("RoadSegments", table => table.HasCheckConstraint("CK_RoadSegments_Sequence_Positive", "[Sequence] > 0"));
        builder.HasKey(segment => segment.Id);
        builder.Property(segment => segment.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(segment => segment.SegmentSetId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(segment => segment.RoadSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(segment => segment.Sequence).HasColumnType("int").IsRequired();
        builder.HasIndex(segment => new { segment.SegmentSetId, segment.Sequence }).IsUnique().HasDatabaseName("UX_RoadSegments_Set_Sequence");
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(segment => segment.SegmentSetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(segment => segment.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
