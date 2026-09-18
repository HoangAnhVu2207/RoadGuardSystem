using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Messaging;

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
        });

        builder.HasKey(message => message.Id);
        builder.Property(message => message.MessageType).HasMaxLength(200).IsUnicode(false).IsRequired();
        builder.Property(message => message.OccurredAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(message => message.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(message => message.OccurredAtUtc).HasDatabaseName("IX_OutboxMessages_OccurredAtUtc");
        builder.HasIndex(message => message.CorrelationId).HasDatabaseName("IX_OutboxMessages_CorrelationId");
    }
}
