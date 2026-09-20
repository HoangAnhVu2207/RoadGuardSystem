using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id)
            .HasColumnType("uniqueidentifier")
            .ValueGeneratedNever();
        builder.Property(notification => notification.RecipientUserId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(notification => notification.SourceEntityType)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(notification => notification.SourceEntityId)
            .HasColumnType("uniqueidentifier")
            .IsRequired();
        builder.Property(notification => notification.EventType)
            .HasMaxLength(80)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(notification => notification.Body)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
        builder.Property(notification => notification.ReadAt)
            .HasColumnType("datetimeoffset(7)");
        builder.HasIndex(notification => new
        {
            notification.RecipientUserId,
            notification.SourceEntityType,
            notification.SourceEntityId,
            notification.EventType
        })
            .IsUnique()
            .HasDatabaseName("UX_Notifications_RecipientSourceEvent");
        builder.HasIndex(notification => new { notification.RecipientUserId, notification.ReadAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_ReadAt");
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(notification => notification.RecipientUserId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(notification => notification.SourceEntityType).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(notification => notification.SourceEntityId).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(notification => notification.EventType).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(notification => notification.Title).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(notification => notification.Body).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
