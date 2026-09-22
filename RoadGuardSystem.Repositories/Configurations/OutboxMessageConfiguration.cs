using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", table =>
        {
            table.HasCheckConstraint(
                "CK_OutboxMessages_PayloadJson_Json",
                "ISJSON([PayloadJson]) = 1");
            table.HasCheckConstraint(
                "CK_OutboxMessages_DeliveryStatus",
                "[DeliveryStatus] IN (1, 2, 3, 4)");
            table.HasCheckConstraint(
                "CK_OutboxMessages_DeliveryAttemptCount",
                "[DeliveryAttemptCount] >= 0");
        });

        builder.HasKey(message => message.Id);
        builder.Property(message => message.MessageType).HasMaxLength(200).IsUnicode(false).IsRequired();
        builder.Property(message => message.OccurredAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(message => message.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(message => message.DeliveryStatus)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();
        builder.Property(message => message.DeliveryAttemptCount)
            .HasColumnType("int")
            .IsRequired();
        builder.Property(message => message.NextAttemptAtUtc)
            .HasColumnType("datetimeoffset(7)")
            .IsRequired();
        builder.Property(message => message.LeaseOwner).HasMaxLength(120).IsUnicode(false);
        builder.Property(message => message.LeaseExpiresAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(message => message.LastErrorCode).HasMaxLength(80).IsUnicode(false);
        builder.Property(message => message.LastErrorMessage).HasColumnType("nvarchar(max)");
        builder.HasIndex(message => message.OccurredAtUtc).HasDatabaseName("IX_OutboxMessages_OccurredAtUtc");
        builder.HasIndex(message => message.CorrelationId).HasDatabaseName("IX_OutboxMessages_CorrelationId");
        builder.HasIndex(message => new { message.DeliveryStatus, message.NextAttemptAtUtc })
            .HasDatabaseName("IX_OutboxMessages_DeliveryStatus_NextAttempt");
    }
}
