using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class FieldInspectionTaskConfiguration : IEntityTypeConfiguration<FieldInspectionTask>
{
    public void Configure(EntityTypeBuilder<FieldInspectionTask> builder)
    {
        builder.ToTable("FieldInspectionTasks", table =>
        {
            table.HasTrigger("TR_FieldInspectionTasks_H3Scope");
            table.HasCheckConstraint("CK_FieldInspectionTasks_Source", "([LifecycleVersion]=1 AND [SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([LifecycleVersion]=2 AND [Purpose] IN(3,4,5) AND (([SourceKind]='SURVEY' AND [SurveyId] IS NOT NULL) OR ([SourceKind]='REPORTER' AND [SurveyId] IS NULL)))");
            table.HasCheckConstraint("CK_FieldInspectionTasks_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7, 8)");
            table.HasCheckConstraint("CK_FieldInspectionTasks_RepairMode", "([TaskMode]='MEASURE_ONLY' AND [RepairItemId] IS NULL) OR ([LifecycleVersion]=2 AND [TaskMode] IN('NORMAL','CONDITIONAL_FT') AND [RepairItemId] IS NOT NULL)");
            table.HasCheckConstraint("CK_FieldInspectionTasks_ReviewDecision", "([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL) OR ([ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status] = 7)");
            table.HasCheckConstraint("CK_FieldInspectionTasks_MeasurementScope_Json", "ISJSON([MeasurementScope]) = 1 AND LEFT(LTRIM([MeasurementScope]), 1) = '{'");
        });

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(task => task.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
        builder.Property(task => task.TaskCode).HasColumnType("nvarchar(80)").IsRequired();
        builder.Property(task => task.ProjectId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(task => task.DefectId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(task => task.SurveyId).HasColumnType("uniqueidentifier");
        builder.Property(task => task.LifecycleVersion).HasDefaultValue(1);
        builder.Property(task => task.TaskMode).HasMaxLength(40).HasDefaultValue("MEASURE_ONLY");
        builder.Property(task => task.SourceKind).HasMaxLength(20).HasDefaultValue("SURVEY");
        builder.Property(task => task.Purpose).HasConversion<byte>().HasColumnType("tinyint").HasDefaultValue(FieldInspectionPurpose.DefectVerification);
        builder.Property(task => task.SlabId).HasMaxLength(160);
        builder.HasOne<GeometryMapPublication>().WithMany().HasForeignKey(x => x.MapPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CrsProfileRevision>().WithMany().HasForeignKey(x => x.CrsProfileRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSegmentSet>().WithMany().HasForeignKey(task => task.SegmentSetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PavementLayoutRevision>().WithMany().HasForeignKey(task => task.LayoutRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(task => task.RoadSectionVersionId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(task => task.RequiredMeasurementType).HasColumnType("tinyint").IsRequired();
        builder.Property(task => task.MeasurementScope).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(task => task.Instructions).HasColumnType("nvarchar(1000)");
        builder.Property(task => task.MissingInformation).HasColumnType("nvarchar(1000)");
        builder.Property(task => task.DueAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(task => task.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(task => task.AssignedByUserId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(task => task.ReviewDecision).HasConversion<byte>().HasColumnType("tinyint");
        builder.Property(task => task.ReviewedByUserId).HasColumnType("uniqueidentifier");
        builder.Property(task => task.ReviewedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(task => task.ReviewReason).HasColumnType("nvarchar(1000)");
        builder.HasIndex(task => task.TaskCode).IsUnique().HasDatabaseName("UX_FieldInspectionTasks_TaskCode");
        builder.HasIndex(task => task.DefectId).HasDatabaseName("IX_FieldInspectionTasks_DefectId");
        builder.HasIndex(task => task.ProjectId).HasDatabaseName("IX_FieldInspectionTasks_ProjectId");
        builder.HasOne<Project>().WithMany().HasForeignKey(task => task.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Defect>().WithMany().HasForeignKey(task => task.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadGuardSystem.BusinessObjects.Repairs.RepairItem>().WithMany().HasForeignKey(task => task.RepairItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Survey>().WithMany().HasForeignKey(task => task.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(task => task.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
