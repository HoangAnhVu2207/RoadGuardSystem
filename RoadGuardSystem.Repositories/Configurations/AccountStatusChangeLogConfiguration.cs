using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class AccountStatusChangeLogConfiguration : IEntityTypeConfiguration<AccountStatusChangeLog>
{
    public void Configure(EntityTypeBuilder<AccountStatusChangeLog> builder)
    {
        builder.ToTable("AccountStatusChangeLogs", table =>
        {
            table.HasTrigger("TR_AccountStatusChangeLogs_AppendOnly");
            table.HasCheckConstraint(
                "CK_AccountStatusChangeLogs_FromToStatus_Diff",
                "[FromStatus] <> [ToStatus]");
            table.HasCheckConstraint(
                "CK_AccountStatusChangeLogs_FromStatus",
                "[FromStatus] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_AccountStatusChangeLogs_ToStatus",
                "[ToStatus] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_AccountStatusChangeLogs_Reason_SafeCode",
                "[Reason] IN ('ADMINISTRATOR_INITIATED', 'SELF_SERVICE_ACCOUNT_RECOVERY', 'REGISTRATION_APPROVED', 'SAFETY_POLICY_VIOLATION', 'NO_STATUS_CHANGE', 'ADMINISTRATIVE_LOCK', 'SECURITY_INCIDENT', 'ACCOUNT_REACTIVATED')");
            table.HasCheckConstraint(
                "CK_AccountStatusChangeLogs_Source_SafeCode",
                "[Source] IN ('ADMIN_API', 'SELF_SERVICE', 'IDENTITY_SERVICE', 'COMPLIANCE_REVIEW', 'SYSTEM')");
        });

        builder.HasKey(log => log.Id);

        builder.Property(log => log.TargetUserId)
            .IsRequired();

        builder.HasOne(log => log.TargetUser)
            .WithMany(user => user.TargetAccountStatusChangeLogs)
            .HasForeignKey(log => log.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(log => log.ChangedByUserId);

        builder.HasOne(log => log.ChangedByUser)
            .WithMany()
            .HasForeignKey(log => log.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(log => log.OccurredAt)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();

        builder.Property(log => log.FromStatus)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(log => log.ToStatus)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(log => log.Reason)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(log => log.Source)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(log => log.CorrelationId);

        builder.Property(log => log.HandoverReference);

        builder.HasIndex(log => log.TargetUserId)
            .HasDatabaseName("IX_AccountStatusChangeLogs_TargetUserId");
        builder.HasIndex(log => log.OccurredAt)
            .HasDatabaseName("IX_AccountStatusChangeLogs_OccurredAt");
    }
}
