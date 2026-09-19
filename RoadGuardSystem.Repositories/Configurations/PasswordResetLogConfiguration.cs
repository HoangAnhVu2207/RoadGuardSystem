using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class PasswordResetLogConfiguration : IEntityTypeConfiguration<PasswordResetLog>
{
    public void Configure(EntityTypeBuilder<PasswordResetLog> builder)
    {
        builder.ToTable("PasswordResetLogs", table =>
        {
            table.HasTrigger("TR_PasswordResetLogs_AppendOnly");
            table.HasCheckConstraint(
                "CK_PasswordResetLogs_Result",
                "[Result] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_PasswordResetLogs_Reason_SafeCode",
                "[Reason] IS NULL OR [Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')");
            table.HasCheckConstraint(
                "CK_PasswordResetLogs_Source_SafeCode",
                "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')");
        });

        builder.HasKey(log => log.Id);

        builder.Property(log => log.TargetUserId)
            .IsRequired();

        builder.HasOne(log => log.TargetUser)
            .WithMany(user => user.TargetPasswordResetLogs)
            .HasForeignKey(log => log.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(log => log.PerformedByUserId);

        builder.HasOne(log => log.PerformedByUser)
            .WithMany()
            .HasForeignKey(log => log.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(log => log.OccurredAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();

        builder.Property(log => log.Reason)
            .HasColumnType("nvarchar(max)");

        builder.Property(log => log.Result)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(log => log.Source)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(log => log.CorrelationId);

        builder.HasIndex(log => log.TargetUserId)
            .HasDatabaseName("IX_PasswordResetLogs_TargetUserId");
        builder.HasIndex(log => log.OccurredAt)
            .HasDatabaseName("IX_PasswordResetLogs_OccurredAt");
    }
}
