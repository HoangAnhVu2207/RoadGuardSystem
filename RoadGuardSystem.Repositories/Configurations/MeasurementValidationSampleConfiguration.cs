using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class MeasurementValidationSampleConfiguration : IEntityTypeConfiguration<MeasurementValidationSample>
{
    public void Configure(EntityTypeBuilder<MeasurementValidationSample> builder)
    {
        builder.ToTable("MeasurementValidationSamples", table =>
        {
            table.HasTrigger("TR_MeasurementValidationSamples_Immutable");
            table.HasCheckConstraint("CK_MeasurementValidationSamples_InclusionStatus", "[InclusionStatus] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_MeasurementValidationSamples_Reason", "[InclusionStatus] = 1 OR LEN(LTRIM(RTRIM([ExclusionReason]))) > 0");
        });

        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(value => value.ValidationRunId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(value => value.GroundTruthMeasurementId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(value => value.DerivedMeasurementId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(value => value.SignedError).HasColumnType("decimal(19,6)").IsRequired();
        builder.Property(value => value.AbsoluteError).HasColumnType("decimal(19,6)").IsRequired();
        builder.Property(value => value.InclusionStatus).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(value => value.ExclusionReason).HasColumnType("nvarchar(500)");
        builder.HasIndex(value => new { value.ValidationRunId, value.GroundTruthMeasurementId, value.DerivedMeasurementId }).IsUnique().HasDatabaseName("UX_MeasurementValidationSamples_RunPair");
        builder.HasOne<ValidationRun>().WithMany().HasForeignKey(value => value.ValidationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<GroundTruthMeasurement>().WithMany().HasForeignKey(value => value.GroundTruthMeasurementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DerivedMeasurement>().WithMany().HasForeignKey(value => value.DerivedMeasurementId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.ValidationRunId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.GroundTruthMeasurementId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.DerivedMeasurementId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.SignedError).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.AbsoluteError).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.InclusionStatus).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(value => value.ExclusionReason).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
