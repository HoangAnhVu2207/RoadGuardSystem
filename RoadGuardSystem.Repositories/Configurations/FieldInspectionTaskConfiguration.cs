using Microsoft.EntityFrameworkCore;
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
            table.HasCheckConstraint("CK_FieldInspectionTasks_Status", "[Status] IN (1, 2, 3, 4, 5, 6, 7)");
            table.HasCheckConstraint("CK_FieldInspectionTasks_ReviewDecision", "([ReviewDecision] IS NULL AND [ReviewedByUserId] IS NULL AND [ReviewedAt] IS NULL) OR ([ReviewDecision] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL AND [ReviewedAt] IS NOT NULL AND [Status] = 7)");
            table.HasCheckConstraint("CK_FieldInspectionTasks_MeasurementScope_Json", "ISJSON([MeasurementScope]) = 1 AND LEFT(LTRIM([MeasurementScope]), 1) = '{'");
        });

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(task => task.TaskCode).HasColumnType("nvarchar(80)").IsRequired();
        builder.Property(task => task.ProjectId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(task => task.DefectId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(task => task.SurveyId).HasColumnType("uniqueidentifier").IsRequired();
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
        builder.HasOne<Survey>().WithMany().HasForeignKey(task => task.SurveyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoadSectionVersion>().WithMany().HasForeignKey(task => task.RoadSectionVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(task => task.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
