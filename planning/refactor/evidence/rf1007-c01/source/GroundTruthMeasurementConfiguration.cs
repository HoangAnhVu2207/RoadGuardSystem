using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class GroundTruthMeasurementConfiguration : IEntityTypeConfiguration<GroundTruthMeasurement>
{
    public void Configure(EntityTypeBuilder<GroundTruthMeasurement> builder)
    {
        builder.ToTable("GroundTruthMeasurements", table =>
        {
            table.HasTrigger("TR_GroundTruthMeasurements_Immutable");
            table.HasCheckConstraint("CK_GroundTruthMeasurements_MeasurementType", "[MeasurementType] IN (1, 2, 3)");
            table.HasCheckConstraint("CK_GroundTruthMeasurements_Value", "[Value] >= 0");
            table.HasCheckConstraint("CK_GroundTruthMeasurements_Unit", "LOWER([Unit]) IN ('mm', 'cm', 'm')");
            table.HasCheckConstraint("CK_GroundTruthMeasurements_Location", "[Location].STSrid = 4326 AND [Location].STIsEmpty() = 0");
            table.HasCheckConstraint("CK_GroundTruthMeasurements_EvidenceOrReason", "[EvidenceFileId] IS NOT NULL OR LEN(LTRIM(RTRIM([Notes]))) > 0");
        });

        builder.HasKey(measurement => measurement.Id);
        builder.Property(measurement => measurement.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(measurement => measurement.FieldInspectionSessionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(measurement => measurement.SampleId).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(measurement => measurement.RoadSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(measurement => measurement.SurveyId).HasColumnType("uniqueidentifier");
        builder.Property(measurement => measurement.DefectId).HasColumnType("uniqueidentifier");
        builder.Property(measurement => measurement.MeasurementType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(measurement => measurement.Value).HasColumnType("decimal(19,6)").IsRequired();
        builder.Property(measurement => measurement.Unit).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(measurement => measurement.Location).HasColumnType("geography").IsRequired();
        builder.Property(measurement => measurement.InstrumentName).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(measurement => measurement.InstrumentReference).HasColumnType("nvarchar(150)");
        builder.Property(measurement => measurement.MeasurementMethod).HasColumnType("nvarchar(500)").IsRequired();
        builder.Property(measurement => measurement.MeasuredBy).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(measurement => measurement.MeasuredAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(measurement => measurement.EvidenceFileId).HasColumnType("uniqueidentifier");
        builder.Property(measurement => measurement.Notes).HasColumnType("nvarchar(max)");
        builder.HasIndex(measurement => new { measurement.FieldInspectionSessionId, measurement.SampleId }).IsUnique().HasDatabaseName("UX_GroundTruthMeasurements_SessionSample");
        builder.HasIndex(measurement => new { measurement.RoadSectionVersionId, measurement.MeasurementType }).HasDatabaseName("IX_GroundTruthMeasurements_RoadVersionType");
        builder.HasOne<FieldInspectionSession>().WithMany().HasForeignKey(measurement => measurement.FieldInspectionSessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(measurement => measurement.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Survey>().WithMany().HasForeignKey(measurement => measurement.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(measurement => measurement.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(measurement => measurement.EvidenceFileId).OnDelete(DeleteBehavior.Restrict);
    }
}
