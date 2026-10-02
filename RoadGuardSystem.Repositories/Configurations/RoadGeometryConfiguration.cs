using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;
namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RoadGeometryDraftConfiguration : IEntityTypeConfiguration<RoadGeometryDraft>
{
    public void Configure(EntityTypeBuilder<RoadGeometryDraft> builder)
    {
        var b = builder;
        b.ToTable("RoadGeometryDrafts"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever(); b.Property(x => x.RoadCode).HasMaxLength(80);
        b.Property(x => x.RoadName).HasMaxLength(255); b.Property(x => x.Status).HasMaxLength(16);
        b.Property(x => x.SourceChecksum).HasMaxLength(64); b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RoadSection>().WithMany().HasForeignKey(x => x.RoadSectionId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class RoadGeometryMetadataConfiguration : IEntityTypeConfiguration<RoadGeometryMetadata>
{
    public void Configure(EntityTypeBuilder<RoadGeometryMetadata> builder)
    {
        var b = builder;
        b.ToTable("RoadGeometryMetadata"); b.HasKey(x => x.RoadSectionVersionId);
        b.Property(x => x.GeometryHash).HasMaxLength(64);
        b.HasOne<RoadSectionVersion>().WithOne().HasForeignKey<RoadGeometryMetadata>(x => x.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<RoadGeometryDraft>().WithMany().HasForeignKey(x => x.SourceDraftId).OnDelete(DeleteBehavior.Restrict);
    }
}
