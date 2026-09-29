using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DerivedMeasurementConfiguration : IEntityTypeConfiguration<DerivedMeasurement>
{
    public void Configure(EntityTypeBuilder<DerivedMeasurement> builder)
    {
        builder.ToTable("DerivedMeasurements", table =>
        {
            table.HasTrigger("TR_DerivedMeasurements_Immutable");
            table.HasCheckConstraint("CK_DerivedMeasurements_MeasurementType", "[MeasurementType] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_DerivedMeasurements_Value", "[Value] >= 0");
            table.HasCheckConstraint("CK_DerivedMeasurements_UncertaintyEstimate", "[UncertaintyEstimate] IS NULL OR [UncertaintyEstimate] >= 0");
            table.HasCheckConstraint("CK_DerivedMeasurements_Unit", "LOWER([Unit]) IN ('mm', 'cm', 'm')");
            table.HasCheckConstraint("CK_DerivedMeasurements_SourceType", "[SourceType] IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_DerivedMeasurements_Status", "[Status] IN (1, 2, 3)");
        });

        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(value => value.SurveyDataVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(value => value.RoadSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(value => value.SampleId).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(value => value.MeasurementType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(value => value.Value).HasColumnType("decimal(19,6)").IsRequired();
        builder.Property(value => value.Unit).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(value => value.UncertaintyEstimate).HasColumnType("decimal(19,6)");
        builder.Property(value => value.SourceType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(value => value.AlgorithmVersion).HasColumnType("nvarchar(100)");
        builder.Property(value => value.ComputedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(value => value.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.HasIndex(value => new { value.SurveyDataVersionId, value.SampleId, value.MeasurementType, value.Status }).HasDatabaseName("IX_DerivedMeasurements_DataSampleTypeStatus");
        builder.HasOne<SurveyDataVersion>().WithMany().HasForeignKey(value => value.SurveyDataVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(value => value.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.SurveyDataVersionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.RoadSectionVersionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.SampleId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.MeasurementType).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.Value).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.Unit).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.UncertaintyEstimate).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.SourceType).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.AlgorithmVersion).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.ComputedAt).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.Status).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
