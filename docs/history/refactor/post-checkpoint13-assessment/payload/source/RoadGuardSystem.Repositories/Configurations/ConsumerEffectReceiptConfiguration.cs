using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class ConsumerEffectReceiptConfiguration : IEntityTypeConfiguration<ConsumerEffectReceipt>
{
    public void Configure(EntityTypeBuilder<ConsumerEffectReceipt> builder)
    {
        builder.ToTable("ConsumerEffectReceipts");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.ConsumerName).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(receipt => receipt.ProcessedAtUtc).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.HasIndex(receipt => new { receipt.MessageId, receipt.ConsumerName })
            .IsUnique()
            .HasDatabaseName("UX_ConsumerEffectReceipts_MessageConsumer");
        builder.HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(receipt => receipt.MessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
