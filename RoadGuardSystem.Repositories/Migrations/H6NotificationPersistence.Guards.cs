using Microsoft.EntityFrameworkCore.Migrations;

namespace RoadGuardSystem.cRepositories.Migrations;

public partial class H6NotificationPersistence
{
    private static void InstallNotificationGuards(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationOccurrences_Immutable] ON [H6NotificationOccurrences]
            AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51180, 'Notification occurrence facts are immutable.', 1; END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationDeliveryAttempts_Immutable] ON [H6NotificationDeliveryAttempts]
            AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51181, 'Notification attempt facts are immutable.', 1; END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationAudits_Immutable] ON [H6NotificationAudits]
            AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51182, 'Notification audit history is immutable.', 1; END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationEventReceipts_Immutable] ON [H6NotificationEventReceipts]
            AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51183, 'Notification completion receipts are immutable.', 1; END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationOccurrences_Scope] ON [H6NotificationOccurrences]
            AFTER INSERT AS BEGIN
             SET NOCOUNT ON;
             IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
               SELECT 1 FROM [OutboxMessages] o WHERE o.Id=i.SourceEventId
               AND o.MessageType COLLATE Latin1_General_100_BIN2=i.EventType COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(o.MessageType)=DATALENGTH(i.EventType) AND o.OccurredAtUtc=i.OccurredAtUtc
               AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(o.PayloadJson,'$.projectId'))=i.ProjectId
               AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(o.PayloadJson,'$.sourceId'))=i.SourceId
               AND TRY_CONVERT(uniqueidentifier,JSON_VALUE(o.PayloadJson,'$.originEventId'))=i.OriginEventId
               AND JSON_VALUE(o.PayloadJson,'$.sourceKind') COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(CONVERT(varchar(80),JSON_VALUE(o.PayloadJson,'$.sourceKind')))=DATALENGTH(i.SourceKind)))
               THROW 51184, 'Notification occurrence must mirror its actual admitted transport source.', 1;
            END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationDeliveries_Identity] ON [H6NotificationDeliveries]
            AFTER UPDATE, DELETE AS BEGIN
             SET NOCOUNT ON;
             IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id))
               THROW 51185, 'Notification delivery history cannot be deleted.', 1;
             IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE
               i.OccurrenceId<>d.OccurrenceId OR i.RecipientKey COLLATE Latin1_General_100_BIN2<>d.RecipientKey COLLATE Latin1_General_100_BIN2
               OR DATALENGTH(i.RecipientKey)<>DATALENGTH(d.RecipientKey)
               OR (d.RecipientUserId IS NOT NULL AND NOT EXISTS(SELECT i.RecipientUserId INTERSECT SELECT d.RecipientUserId))
               OR (d.Status='DELIVERED' AND EXISTS(SELECT i.Status,i.NotificationId,i.DeliveredAtUtc,i.ReasonCode
                                                EXCEPT SELECT d.Status,d.NotificationId,d.DeliveredAtUtc,d.ReasonCode)))
               THROW 51186, 'A pinned recipient or committed delivery cannot be retargeted.', 1;
            END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationDeliveries_Scope] ON [H6NotificationDeliveries]
            AFTER INSERT, UPDATE AS BEGIN
             SET NOCOUNT ON;
             IF EXISTS(SELECT 1 FROM inserted i WHERE i.NotificationId IS NOT NULL AND NOT EXISTS(
               SELECT 1 FROM [Notifications] n JOIN [H6NotificationOccurrences] o ON o.Id=i.OccurrenceId
               WHERE n.Id=i.NotificationId AND n.RecipientUserId=i.RecipientUserId
               AND n.SourceEntityId=o.SourceId AND n.SourceEntityType COLLATE Latin1_General_100_BIN2=o.SourceKind COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(n.SourceEntityType)=DATALENGTH(o.SourceKind)
               AND n.EventType COLLATE Latin1_General_100_BIN2=o.OccurrenceKey COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(n.EventType)=DATALENGTH(o.OccurrenceKey) AND n.OccurredAtUtc=o.OccurredAtUtc))
               THROW 51187, 'Notification effect must match its occurrence and actual recipient.', 1;
            END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationScopes_Source] ON [H6NotificationScopes]
            AFTER INSERT, UPDATE, DELETE AS BEGIN
             SET NOCOUNT ON;
             IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.NotificationId=d.NotificationId))
               THROW 51188, 'Notification source scope cannot be deleted.', 1;
             IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.NotificationId=i.NotificationId WHERE
               i.SourceId<>d.SourceId OR i.SourceKind COLLATE Latin1_General_100_BIN2<>d.SourceKind COLLATE Latin1_General_100_BIN2
               OR DATALENGTH(i.SourceKind)<>DATALENGTH(d.SourceKind)
               OR i.EventType COLLATE Latin1_General_100_BIN2<>d.EventType COLLATE Latin1_General_100_BIN2
               OR DATALENGTH(i.EventType)<>DATALENGTH(d.EventType)
               OR (d.ProjectId IS NOT NULL AND NOT EXISTS(SELECT i.ProjectId INTERSECT SELECT d.ProjectId))
               OR (d.OccurrenceId IS NOT NULL AND NOT EXISTS(SELECT i.OccurrenceId INTERSECT SELECT d.OccurrenceId)))
               THROW 51189, 'Known notification source identities cannot be relabelled.', 1;
             IF EXISTS(SELECT 1 FROM inserted i WHERE NOT EXISTS(
               SELECT 1 FROM [Notifications] n JOIN [H6NotificationAudits] a ON a.Id=i.CurrentAuditId
               WHERE n.Id=i.NotificationId AND n.SourceEntityId=i.SourceId
               AND n.SourceEntityType COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(n.SourceEntityType)=DATALENGTH(i.SourceKind)
               AND a.NotificationId=i.NotificationId AND a.SourceId=i.SourceId
               AND a.SourceKind COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(a.SourceKind)=DATALENGTH(i.SourceKind)
               AND a.Classification=i.Classification AND a.ResolverVersion=i.ResolverVersion
               AND (a.ProjectId=i.ProjectId OR (a.ProjectId IS NULL AND i.ProjectId IS NULL))))
               THROW 51190, 'Notification scope must have actual matching immutable audit facts.', 1;
             IF EXISTS(SELECT 1 FROM inserted i WHERE i.OccurrenceId IS NOT NULL AND NOT EXISTS(
               SELECT 1 FROM [H6NotificationOccurrences] o JOIN [Notifications] n ON n.Id=i.NotificationId
               WHERE o.Id=i.OccurrenceId AND o.ProjectId=i.ProjectId AND o.SourceId=i.SourceId
               AND o.SourceKind COLLATE Latin1_General_100_BIN2=i.SourceKind COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(o.SourceKind)=DATALENGTH(i.SourceKind)
               AND o.EventType COLLATE Latin1_General_100_BIN2=i.EventType COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(o.EventType)=DATALENGTH(i.EventType)
               AND n.EventType COLLATE Latin1_General_100_BIN2=o.OccurrenceKey COLLATE Latin1_General_100_BIN2
               AND DATALENGTH(n.EventType)=DATALENGTH(o.OccurrenceKey)))
               THROW 51191, 'Project notification scope must match its pinned occurrence.', 1;
            END;
            """);
        migrationBuilder.Sql("""
            CREATE OR ALTER TRIGGER [TR_H6NotificationCalendar_Identity] ON [H6NotificationCalendar]
            AFTER UPDATE, DELETE AS BEGIN
             SET NOCOUNT ON;
             IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id))
               THROW 51192, 'Calendar occurrence history cannot be deleted.', 1;
             IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE
               i.ClockId<>d.ClockId OR i.ScheduledAtUtc<>d.ScheduledAtUtc
               OR (d.OutboxMessageId IS NOT NULL AND NOT EXISTS(SELECT i.OutboxMessageId INTERSECT SELECT d.OutboxMessageId))
               OR (d.Status='COMMITTED' AND EXISTS(SELECT i.Status,i.OutboxMessageId,i.ObservedAtUtc
                                                EXCEPT SELECT d.Status,d.OutboxMessageId,d.ObservedAtUtc)))
               THROW 51193, 'Calendar period and committed source are immutable.', 1;
            END;
            """);
    }
    private static void RefusePopulatedNotificationDowngrade(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS(SELECT 1 FROM [H6NotificationOccurrences]) OR EXISTS(SELECT 1 FROM [H6NotificationDeliveries]) OR EXISTS(SELECT 1 FROM [H6NotificationDeliveryAttempts]) OR EXISTS(SELECT 1 FROM [H6NotificationScopes]) OR EXISTS(SELECT 1 FROM [H6NotificationAudits]) OR EXISTS(SELECT 1 FROM [H6NotificationEventReceipts]) OR EXISTS(SELECT 1 FROM [H6NotificationCalendar])
            THROW 51199, 'Populated notification history requires a data-preserving migration.', 1;
            """);
    }
}
