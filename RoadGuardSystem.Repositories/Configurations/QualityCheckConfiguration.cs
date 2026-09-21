using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Surveys;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class QualityCheckConfiguration : IEntityTypeConfiguration<QualityCheck>
{
    public void Configure(EntityTypeBuilder<QualityCheck> builder)
    {
        builder.ToTable("QualityChecks", table =>
        {
            table.HasCheckConstraint("CK_QualityChecks_Scope", "[Scope] IN (1, 2)");
            table.HasCheckConstraint("CK_QualityChecks_ExecutionStage", "[ExecutionStage] IN (1, 2)");
            table.HasCheckConstraint("CK_QualityChecks_CheckType", "[CheckType] IN (1, 2, 3, 4, 5, 6, 7, 8, 9)");
            table.HasCheckConstraint("CK_QualityChecks_Status", "[Status] IN (1, 2, 3, 4)");
            table.HasCheckConstraint("CK_QualityChecks_CheckedBy", "[CheckedBy] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_QualityChecks_ExactlyOneTarget",
                "([Scope] = 1 AND [SurveyFileId] IS NOT NULL AND [SurveyDataVersionId] IS NULL) " +
                "OR ([Scope] = 2 AND [SurveyFileId] IS NULL AND [SurveyDataVersionId] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_QualityChecks_StageActor",
                "([ExecutionStage] = 1 AND [CheckedBy] = 1) " +
                "OR ([ExecutionStage] = 2 AND [CheckedBy] = 2 AND [InitiatedByUserId] IS NULL)");
            table.HasCheckConstraint("CK_QualityChecks_MeasuredValue_Json", "[MeasuredValue] IS NULL OR ISJSON([MeasuredValue]) = 1");
            table.HasCheckConstraint("CK_QualityChecks_Threshold_Json", "[Threshold] IS NULL OR ISJSON([Threshold]) = 1");
        });

        builder.HasKey(check => check.Id);
        builder.Property(check => check.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(check => check.Scope).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(check => check.ExecutionStage).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(check => check.SurveyFileId).HasColumnType("uniqueidentifier");
        builder.Property(check => check.SurveyDataVersionId).HasColumnType("uniqueidentifier");
        builder.Property(check => check.CheckType).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(check => check.Status).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(check => check.MeasuredValue).HasColumnType("nvarchar(max)");
        builder.Property(check => check.Threshold).HasColumnType("nvarchar(max)");
        builder.Property(check => check.Message).HasColumnType("nvarchar(max)");
        builder.Property(check => check.CheckedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(check => check.CheckedBy).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(check => check.InitiatedByUserId).HasColumnType("uniqueidentifier");
        builder.HasIndex(check => check.SurveyFileId).HasDatabaseName("IX_QualityChecks_SurveyFileId");
        builder.HasIndex(check => check.SurveyDataVersionId).HasDatabaseName("IX_QualityChecks_SurveyDataVersionId");
        builder.HasOne<SurveyFile>().WithMany().HasForeignKey(check => check.SurveyFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SurveyDataVersion>().WithMany().HasForeignKey(check => check.SurveyDataVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(check => check.InitiatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
