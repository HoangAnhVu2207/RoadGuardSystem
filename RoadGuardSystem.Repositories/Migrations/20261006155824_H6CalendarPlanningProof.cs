using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H6CalendarPlanningProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlannedAtUtc",
                table: "H6NotificationCalendar",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
            // Existing rows have no preplanning proof. Preserve them as history; zero is
            // deliberately not manufactured as an observed scheduler timestamp.
            migrationBuilder.Sql("UPDATE [H6NotificationCalendar] SET [Status]='PENDING_POLICY' WHERE [Status]='PLANNED';");
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_H6NotificationCalendar_Identity] ON [H6NotificationCalendar]
                AFTER UPDATE, DELETE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id))
                   THROW 51192, 'Calendar occurrence history cannot be deleted.', 1;
                 IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE
                   i.ClockId<>d.ClockId OR i.ScheduledAtUtc<>d.ScheduledAtUtc OR i.PlannedAtUtc<>d.PlannedAtUtc
                   OR i.SchedulerRunId<>d.SchedulerRunId
                   OR (d.OutboxMessageId IS NOT NULL AND NOT EXISTS(SELECT i.OutboxMessageId INTERSECT SELECT d.OutboxMessageId))
                   OR (d.Status IN ('COMMITTED','PENDING_POLICY','NO_REVIEW') AND EXISTS(
                      SELECT i.Status,i.OutboxMessageId,i.ObservedAtUtc EXCEPT SELECT d.Status,d.OutboxMessageId,d.ObservedAtUtc)))
                   THROW 51193, 'Calendar period, planning proof and final observation are immutable.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM [H6NotificationCalendar]) THROW 51200, 'Populated calendar planning history requires a preserving forward migration.', 1;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_H6NotificationCalendar_Identity];");
            migrationBuilder.DropColumn(
                name: "PlannedAtUtc",
                table: "H6NotificationCalendar");
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
    }
}
