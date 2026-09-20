using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class RoadSectionVersionConfiguration : IEntityTypeConfiguration<RoadSectionVersion>
{
    public void Configure(EntityTypeBuilder<RoadSectionVersion> builder)
    {
        builder.ToTable("RoadSectionVersions", table =>
        {
            table.HasTrigger("TR_RoadSectionVersions_Immutable");
            table.HasCheckConstraint("CK_RoadSectionVersions_VersionNo_Positive", "[VersionNo] > 0");
            table.HasCheckConstraint(
                "CK_RoadSectionVersions_Geometry_LineString",
                "[Geometry].STGeometryType() = 'LineString'");
            table.HasCheckConstraint(
                "CK_RoadSectionVersions_Geometry_AllowedSrid",
                "[Geometry].STSrid IN (32648, 32649)");
        });

        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(version => version.RoadSectionId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(version => version.VersionNo)
            .HasColumnType("int")
            .IsRequired();
        builder.Property(version => version.IsCurrent)
            .HasColumnType("bit")
            .IsRequired();
        builder.Property(version => version.Geometry)
            .HasColumnType("geometry")
            .IsRequired();
        builder.Property(version => version.EffectiveFrom)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(version => version.ChangeReason)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.HasIndex(version => new { version.RoadSectionId, version.VersionNo })
            .IsUnique()
            .HasDatabaseName("UX_RoadSectionVersions_RoadSectionId_VersionNo");
        builder.HasIndex(version => version.RoadSectionId)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1")
            .HasDatabaseName("UX_RoadSectionVersions_Current");
        builder.HasOne<RoadSection>()
            .WithMany()
            .HasForeignKey(version => version.RoadSectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(version => version.RoadSectionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(version => version.VersionNo).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(version => version.Geometry).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(version => version.EffectiveFrom).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(version => version.ChangeReason).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
