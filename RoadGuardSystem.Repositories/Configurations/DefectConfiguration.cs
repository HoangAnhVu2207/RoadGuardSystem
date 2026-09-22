using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DefectConfiguration : IEntityTypeConfiguration<Defect>
{
    public void Configure(EntityTypeBuilder<Defect> builder)
    {
        builder.ToTable("Defects", table =>
        {
            table.HasCheckConstraint("CK_Defects_Severity", "[Severity] IS NULL OR [Severity] IN (0, 1, 2, 3, 4)");
            table.HasCheckConstraint("CK_Defects_Status", "[Status] IS NULL OR [Status] IN (0, 1, 2, 3, 4)");
        });
        builder.HasKey(defect => defect.Id);
        builder.Property(defect => defect.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(defect => defect.DefectTypeCode)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(defect => defect.CauseCategoryCode)
            .HasMaxLength(80)
            .IsUnicode(false);
        builder.Property(defect => defect.ProjectId).HasColumnType("uniqueidentifier");
        builder.Property(defect => defect.RoadSectionVersionId).HasColumnType("uniqueidentifier");
        builder.Property(defect => defect.SourceAIDetectionId).HasColumnType("uniqueidentifier");
        builder.Property(defect => defect.Severity).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(defect => defect.Status).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(defect => defect.Geometry).HasColumnType("geometry");
        builder.Property(defect => defect.ReportedAt).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(defect => defect.SourceAIDetectionId)
            .IsUnique()
            .HasFilter("[SourceAIDetectionId] IS NOT NULL")
            .HasDatabaseName("UX_Defects_SourceAIDetectionId");

        builder.HasOne<DefectType>()
            .WithMany()
            .HasForeignKey(defect => defect.DefectTypeCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CauseCategory>()
            .WithMany()
            .HasForeignKey(defect => defect.CauseCategoryCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(defect => defect.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>()
            .WithMany()
            .HasForeignKey(defect => defect.RoadSectionVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIDetection>()
            .WithMany()
            .HasForeignKey(defect => defect.SourceAIDetectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
