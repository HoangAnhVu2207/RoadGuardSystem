using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Auditing;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", table =>
        {
            table.HasTrigger("TR_AuditLogs_AppendOnly");
            table.HasCheckConstraint(
                "CK_AuditLogs_BeforeSnapshot_Json",
                "[BeforeSnapshot] IS NULL OR ISJSON([BeforeSnapshot]) = 1");
            table.HasCheckConstraint(
                "CK_AuditLogs_AfterSnapshot_Json",
                "[AfterSnapshot] IS NULL OR ISJSON([AfterSnapshot]) = 1");
        });

        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.OccurredAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(audit => audit.EventType).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(audit => audit.EntityType).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(audit => audit.BeforeSnapshot).HasColumnType("nvarchar(max)");
        builder.Property(audit => audit.AfterSnapshot).HasColumnType("nvarchar(max)");
        builder.Property(audit => audit.Reason).HasColumnType("nvarchar(max)");
        builder.Property(audit => audit.Source).HasMaxLength(80).IsUnicode(false).IsRequired();
        builder.HasIndex(audit => audit.OccurredAtUtc).HasDatabaseName("IX_AuditLogs_OccurredAtUtc");
        builder.HasIndex(audit => new { audit.EntityType, audit.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");
        builder.HasIndex(audit => audit.CorrelationId).HasDatabaseName("IX_AuditLogs_CorrelationId");
    }
}
