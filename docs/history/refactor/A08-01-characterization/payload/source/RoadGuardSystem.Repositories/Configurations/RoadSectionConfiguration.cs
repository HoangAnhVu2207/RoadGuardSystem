using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RoadSectionConfiguration : IEntityTypeConfiguration<RoadSection>
{
    public void Configure(EntityTypeBuilder<RoadSection> builder)
    {
        builder.ToTable("RoadSections");
        builder.HasKey(section => section.Id);
        builder.Property(section => section.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(section => section.ProjectId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(section => section.Code)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(section => section.Name)
            .HasMaxLength(255);
        builder.HasIndex(section => new { section.ProjectId, section.Code })
            .IsUnique()
            .HasDatabaseName("UX_RoadSections_ProjectId_Code");
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(section => section.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
