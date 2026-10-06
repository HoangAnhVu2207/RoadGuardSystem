using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;

namespace RoadGuardSystem.Repositories.Messaging;

// Deliberately not IEntityTypeConfiguration: shared schema writer must explicitly adopt these
// mappings after the H4 additive checkpoint. Compilation alone never changes the current model.
internal static class H6NotificationMappingDraft
{
    internal static void Occurrence(EntityTypeBuilder<H6NotificationOccurrenceRow> b)
    {
        b.ToTable("H6NotificationOccurrences", table =>
        {
            table.HasTrigger("TR_H6NotificationOccurrences_Immutable"); table.HasTrigger("TR_H6NotificationOccurrences_Scope");
            table.HasCheckConstraint("CK_H6NotificationOccurrences_Json", "ISJSON([PayloadJson])=1");
            table.HasCheckConstraint("CK_H6NotificationOccurrences_Hashes", "LEN([OccurrenceKey])=64 AND LEN([ContentFingerprint])=64");
        });
        b.HasKey(row => row.Id); b.HasIndex(row => row.OccurrenceKey).IsUnique();
        b.Property(row => row.OccurrenceKey).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.Property(row => row.ContentFingerprint).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.Property(row => row.EventType).HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(row => row.SourceKind).HasMaxLength(80).IsUnicode(false).IsRequired(); b.Property(row => row.PayloadJson).IsRequired();
        b.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OutboxMessage>().WithMany().HasForeignKey(row => row.SourceEventId).OnDelete(DeleteBehavior.Restrict);
    }
    internal static void Delivery(EntityTypeBuilder<H6NotificationDeliveryRow> b)
    {
        b.ToTable("H6NotificationDeliveries", table =>
        {
            table.HasTrigger("TR_H6NotificationDeliveries_Identity"); table.HasTrigger("TR_H6NotificationDeliveries_Scope");
            table.HasCheckConstraint("CK_H6NotificationDeliveries_State", "[Status] IN ('PENDING','UNRESOLVED','DELIVERED') AND (([Status]='DELIVERED' AND [RecipientUserId] IS NOT NULL AND [NotificationId] IS NOT NULL AND [DeliveredAtUtc] IS NOT NULL AND [ReasonCode] IS NULL) OR ([Status]<>'DELIVERED' AND [NotificationId] IS NULL AND [DeliveredAtUtc] IS NULL))");
        });
        b.HasKey(row => row.Id); b.HasIndex(row => new { row.OccurrenceId, row.RecipientKey }).IsUnique();
        b.HasIndex(row => row.NotificationId).IsUnique().HasFilter("[NotificationId] IS NOT NULL");
        b.Property(row => row.RecipientKey).HasMaxLength(100).IsUnicode(false).IsRequired(); b.Property(row => row.Status).HasMaxLength(24).IsUnicode(false).IsRequired();
        b.Property(row => row.ReasonCode).HasMaxLength(80).IsUnicode(false); b.Property(row => row.RowVersion).IsRowVersion();
        b.HasOne<H6NotificationOccurrenceRow>().WithMany().HasForeignKey(row => row.OccurrenceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(row => row.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Notification>().WithMany().HasForeignKey(row => row.NotificationId).OnDelete(DeleteBehavior.Restrict);
    }
    internal static void Attempt(EntityTypeBuilder<H6NotificationDeliveryAttempt> b)
    {
        b.ToTable("H6NotificationDeliveryAttempts", table => table.HasTrigger("TR_H6NotificationDeliveryAttempts_Immutable"));
        b.HasKey(row => row.Id); b.Property(row => row.Status).HasMaxLength(24).IsUnicode(false).IsRequired();
        b.Property(row => row.ReasonCode).HasMaxLength(80).IsUnicode(false);
        b.HasOne<H6NotificationDeliveryRow>().WithMany().HasForeignKey(row => row.DeliveryId).OnDelete(DeleteBehavior.Restrict);
    }
    internal static void Scope(EntityTypeBuilder<H6NotificationScopeRow> b)
    {
        b.ToTable("H6NotificationScopes", table => table.HasTrigger("TR_H6NotificationScopes_Source"));
        b.HasKey(row => row.NotificationId); b.Property(row => row.Classification).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(row => row.SourceKind).HasMaxLength(80).IsUnicode(false).IsRequired(); b.Property(row => row.EventType).HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(row => row.ResolverVersion).HasMaxLength(100).IsUnicode(false).IsRequired(); b.Property(row => row.RowVersion).IsRowVersion();
        b.HasOne<Notification>().WithOne().HasForeignKey<H6NotificationScopeRow>(row => row.NotificationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<H6NotificationOccurrenceRow>().WithMany().HasForeignKey(row => row.OccurrenceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<H6NotificationAuditRow>().WithMany().HasForeignKey(row => row.CurrentAuditId).OnDelete(DeleteBehavior.Restrict);
    }
    internal static void Audit(EntityTypeBuilder<H6NotificationAuditRow> b)
    {
        b.ToTable("H6NotificationAudits", table => table.HasTrigger("TR_H6NotificationAudits_Immutable"));
        b.HasKey(row => row.Id); b.HasIndex(row => row.DedupKey).IsUnique();
        b.Property(row => row.DedupKey).HasMaxLength(64).IsUnicode(false).IsRequired(); b.Property(row => row.Classification).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(row => row.SourceKind).HasMaxLength(80).IsUnicode(false).IsRequired(); b.Property(row => row.ReasonCode).HasMaxLength(80).IsUnicode(false).IsRequired();
        b.Property(row => row.ResolverVersion).HasMaxLength(100).IsUnicode(false).IsRequired();
        b.HasOne<Notification>().WithMany().HasForeignKey(row => row.NotificationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OutboxMessage>().WithMany().HasForeignKey(row => row.OutboxMessageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Project>().WithMany().HasForeignKey(row => row.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<H6NotificationAuditRow>().WithMany().HasForeignKey(row => row.PreviousAuditId).OnDelete(DeleteBehavior.Restrict);
    }
    internal static void Receipt(EntityTypeBuilder<H6NotificationEventReceipt> b)
    {
        b.ToTable("H6NotificationEventReceipts", table => table.HasTrigger("TR_H6NotificationEventReceipts_Immutable"));
        b.HasKey(row => row.OutboxMessageId); b.Property(row => row.MessageType).HasMaxLength(200).IsUnicode(false).IsRequired();
        b.Property(row => row.PayloadHash).HasMaxLength(64).IsUnicode(false).IsRequired(); b.Property(row => row.Status).HasMaxLength(24).IsUnicode(false).IsRequired();
        b.HasOne<OutboxMessage>().WithOne().HasForeignKey<H6NotificationEventReceipt>(row => row.OutboxMessageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<H6NotificationOccurrenceRow>().WithMany().HasForeignKey(row => row.OccurrenceId).OnDelete(DeleteBehavior.Restrict);
    }
    internal static void Calendar(EntityTypeBuilder<H6NotificationCalendarRow> b)
    {
        b.ToTable("H6NotificationCalendar", table => table.HasTrigger("TR_H6NotificationCalendar_Identity"));
        b.HasKey(row => row.Id); b.HasIndex(row => new { row.ClockId, row.ScheduledAtUtc }).IsUnique();
        b.Property(row => row.Status).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.HasOne<DeadlineClock>().WithMany().HasForeignKey(row => row.ClockId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OutboxMessage>().WithMany().HasForeignKey(row => row.OutboxMessageId).OnDelete(DeleteBehavior.Restrict);
    }
}
