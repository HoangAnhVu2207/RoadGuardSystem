using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class DefectVerificationLogConfiguration : IEntityTypeConfiguration<DefectVerificationLog>
{
    public void Configure(EntityTypeBuilder<DefectVerificationLog> builder)
    {
        builder.ToTable("DefectVerificationLogs", table =>
        {
            table.HasTrigger("TR_DefectVerificationLogs_AppendOnly");
            table.HasCheckConstraint("CK_DefectVerificationLogs_ExactlyOneTarget", "([DefectId] IS NOT NULL AND [AIDetectionId] IS NULL) OR ([DefectId] IS NULL AND [AIDetectionId] IS NOT NULL)");
            table.HasCheckConstraint("CK_DefectVerificationLogs_Action", "[Action] IN (1, 2, 3, 4, 5)");
            table.HasCheckConstraint("CK_DefectVerificationLogs_BeforeSnapshot_Json", "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1");
            table.HasCheckConstraint("CK_DefectVerificationLogs_AfterSnapshot_Json", "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1");
        });

        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasColumnType("uniqueidentifier").ValueGeneratedNever();
        builder.Property(log => log.DefectId).HasColumnType("uniqueidentifier");
        builder.Property(log => log.AIDetectionId).HasColumnType("uniqueidentifier");
        builder.Property(log => log.Action).HasConversion<byte>().HasColumnType("tinyint").IsRequired();
        builder.Property(log => log.BeforeSnapshot).HasColumnType("nvarchar(max)");
        builder.Property(log => log.AfterSnapshot).HasColumnType("nvarchar(max)");
        builder.Property(log => log.SeverityRuleVersionId).HasColumnType("uniqueidentifier");
        builder.Property(log => log.FieldInspectionTaskId).HasColumnType("uniqueidentifier");
        builder.Property(log => log.VerifiedByUserId).HasColumnType("uniqueidentifier").IsRequired();
        builder.Property(log => log.Reason).HasColumnType("nvarchar(1000)").IsRequired();
        builder.HasIndex(log => log.DefectId).HasDatabaseName("IX_DefectVerificationLogs_DefectId");
        builder.HasIndex(log => log.AIDetectionId).HasDatabaseName("IX_DefectVerificationLogs_AIDetectionId");
        builder.HasOne<Defect>().WithMany().HasForeignKey(log => log.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AIDetection>().WithMany().HasForeignKey(log => log.AIDetectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SeverityRuleVersion>().WithMany().HasForeignKey(log => log.SeverityRuleVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldInspectionTask>().WithMany().HasForeignKey(log => log.FieldInspectionTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(log => log.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(log => log.DefectId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.AIDetectionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.Action).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.BeforeSnapshot).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.AfterSnapshot).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.SeverityRuleVersionId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.FieldInspectionTaskId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.VerifiedByUserId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(log => log.Reason).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
