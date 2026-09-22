using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class AIDetectionConfiguration : IEntityTypeConfiguration<AIDetection>
{
    public void Configure(EntityTypeBuilder<AIDetection> builder)
    {
        builder.ToTable("AIDetections", table =>
        {
            table.HasTrigger("TR_AIDetections_Immutable");
            table.HasCheckConstraint("CK_AIDetections_Confidence", "[Confidence] >= 0 AND [Confidence] <= 1");
            table.HasCheckConstraint("CK_AIDetections_EstimatedDimensions", "([EstimatedWidth] IS NULL OR [EstimatedWidth] >= 0) AND ([EstimatedLength] IS NULL OR [EstimatedLength] >= 0)");
            table.HasCheckConstraint("CK_AIDetections_RawPayload_Json", "ISJSON([RawPayload]) = 1");
        });

        builder.HasKey(detection => detection.Id);
        builder.Property(detection => detection.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(detection => detection.ProcessingJobId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(detection => detection.ModelVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(detection => detection.RoadSectionVersionId).HasColumnType("uniqueidentifier");
        builder.Property(detection => detection.Geometry).HasColumnType("geometry");
        builder.Property(detection => detection.DefectTypeCode).HasMaxLength(80).IsUnicode(false);
        builder.Property(detection => detection.Confidence).HasColumnType("decimal(6,5)").IsRequired();
        builder.Property(detection => detection.EstimatedWidth).HasColumnType("decimal(12,3)");
        builder.Property(detection => detection.EstimatedLength).HasColumnType("decimal(12,3)");
        builder.Property(detection => detection.RawPayload).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(detection => detection.ProcessingJobId).HasDatabaseName("IX_AIDetections_ProcessingJobId");
        builder.HasIndex(detection => detection.ModelVersionId).HasDatabaseName("IX_AIDetections_ModelVersionId");
        builder.HasOne<ProcessingJob>().WithMany().HasForeignKey(detection => detection.ProcessingJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIModelVersion>().WithMany().HasForeignKey(detection => detection.ModelVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(detection => detection.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DefectType>().WithMany().HasForeignKey(detection => detection.DefectTypeCode).OnDelete(DeleteBehavior.Restrict);

        builder.Property(detection => detection.ProcessingJobId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.ModelVersionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.RoadSectionVersionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.Geometry).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.DefectTypeCode).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.Confidence).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.EstimatedWidth).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.EstimatedLength).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(detection => detection.RawPayload).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
