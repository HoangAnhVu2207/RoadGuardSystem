using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H6NotificationPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "H6NotificationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PreviousAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    ResolverVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DedupKey = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_H6NotificationAudits_PreviousAuditId",
                        column: x => x.PreviousAuditId,
                        principalTable: "H6NotificationAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationAudits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationCalendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SchedulerRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationCalendar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_H6NotificationCalendar_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationCalendar_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceKey = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationOccurrences", x => x.Id);
                    table.CheckConstraint("CK_H6NotificationOccurrences_Hashes", "LEN([OccurrenceKey])=64 AND LEN([ContentFingerprint])=64");
                    table.CheckConstraint("CK_H6NotificationOccurrences_Json", "ISJSON([PayloadJson])=1");
                    table.ForeignKey(
                        name: "FK_H6NotificationOccurrences_OutboxMessages_SourceEventId",
                        column: x => x.SourceEventId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationOccurrences_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    DeliveredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationDeliveries", x => x.Id);
                    table.CheckConstraint("CK_H6NotificationDeliveries_State", "[Status] IN ('PENDING','UNRESOLVED','DELIVERED') AND (([Status]='DELIVERED' AND [RecipientUserId] IS NOT NULL AND [NotificationId] IS NOT NULL AND [DeliveredAtUtc] IS NOT NULL AND [ReasonCode] IS NULL) OR ([Status]<>'DELIVERED' AND [NotificationId] IS NULL AND [DeliveredAtUtc] IS NULL))");
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveries_H6NotificationOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "H6NotificationOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveries_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationEventReceipts",
                columns: table => new
                {
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletionFence = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    PayloadHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationEventReceipts", x => x.OutboxMessageId);
                    table.ForeignKey(
                        name: "FK_H6NotificationEventReceipts_H6NotificationOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "H6NotificationOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationEventReceipts_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationScopes",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Classification = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    SourceKind = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    ResolverVersion = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CurrentAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationScopes", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_H6NotificationAudits_CurrentAuditId",
                        column: x => x.CurrentAuditId,
                        principalTable: "H6NotificationAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_H6NotificationOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "H6NotificationOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_H6NotificationScopes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "H6NotificationDeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", unicode: false, maxLength: 24, nullable: false),
                    ReasonCode = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_H6NotificationDeliveryAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_H6NotificationDeliveryAttempts_H6NotificationDeliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "H6NotificationDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_DedupKey",
                table: "H6NotificationAudits",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_NotificationId",
                table: "H6NotificationAudits",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_OutboxMessageId",
                table: "H6NotificationAudits",
                column: "OutboxMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_PreviousAuditId",
                table: "H6NotificationAudits",
                column: "PreviousAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationAudits_ProjectId",
                table: "H6NotificationAudits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationCalendar_ClockId_ScheduledAtUtc",
                table: "H6NotificationCalendar",
                columns: new[] { "ClockId", "ScheduledAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationCalendar_OutboxMessageId",
                table: "H6NotificationCalendar",
                column: "OutboxMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveries_NotificationId",
                table: "H6NotificationDeliveries",
                column: "NotificationId",
                unique: true,
                filter: "[NotificationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveries_OccurrenceId_RecipientKey",
                table: "H6NotificationDeliveries",
                columns: new[] { "OccurrenceId", "RecipientKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveries_RecipientUserId",
                table: "H6NotificationDeliveries",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationDeliveryAttempts_DeliveryId",
                table: "H6NotificationDeliveryAttempts",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationEventReceipts_OccurrenceId",
                table: "H6NotificationEventReceipts",
                column: "OccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationOccurrences_OccurrenceKey",
                table: "H6NotificationOccurrences",
                column: "OccurrenceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationOccurrences_ProjectId",
                table: "H6NotificationOccurrences",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationOccurrences_SourceEventId",
                table: "H6NotificationOccurrences",
                column: "SourceEventId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationScopes_CurrentAuditId",
                table: "H6NotificationScopes",
                column: "CurrentAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationScopes_OccurrenceId",
                table: "H6NotificationScopes",
                column: "OccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_H6NotificationScopes_ProjectId",
                table: "H6NotificationScopes",
                column: "ProjectId");
            InstallNotificationGuards(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RefusePopulatedNotificationDowngrade(migrationBuilder);
            migrationBuilder.DropTable(
                name: "H6NotificationCalendar");

            migrationBuilder.DropTable(
                name: "H6NotificationDeliveryAttempts");

            migrationBuilder.DropTable(
                name: "H6NotificationEventReceipts");

            migrationBuilder.DropTable(
                name: "H6NotificationScopes");

            migrationBuilder.DropTable(
                name: "H6NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "H6NotificationAudits");

            migrationBuilder.DropTable(
                name: "H6NotificationOccurrences");
        }
    }
}
