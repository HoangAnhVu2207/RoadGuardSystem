using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class OwnerReceivingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessReceivingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibleActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleRole = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClaimedDeviceAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessReceivingRequests", x => x.Id);
                    table.CheckConstraint("CK_BusinessReceivingRequests_Ack", "([AcknowledgedAt] IS NULL AND [AcknowledgmentId] IS NULL AND [AcknowledgedBy] IS NULL AND [ClockId] IS NULL AND [ClaimedDeviceAt] IS NULL) OR ([AcknowledgedAt] IS NOT NULL AND [AcknowledgmentId] IS NOT NULL AND [AcknowledgedBy] IS NOT NULL AND [ClockId] IS NOT NULL AND [AcknowledgedAt]>=[RequestedAt])");
                    table.ForeignKey(
                        name: "FK_BusinessReceivingRequests_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessReceivingRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessReceivingRequests_Users_ResponsibleActorId",
                        column: x => x.ResponsibleActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessDutyAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDutyAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessDutyAppointments_BusinessReceivingRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "BusinessReceivingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessDutyAppointments_Users_CurrentActorId",
                        column: x => x.CurrentActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessDutyAppointments_Users_DecisionActorId",
                        column: x => x.DecisionActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDutyAppointments_CurrentActorId",
                table: "BusinessDutyAppointments",
                column: "CurrentActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDutyAppointments_DecisionActorId",
                table: "BusinessDutyAppointments",
                column: "DecisionActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDutyAppointments_RequestId",
                table: "BusinessDutyAppointments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_ClockId",
                table: "BusinessReceivingRequests",
                column: "ClockId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_ProjectId_ScopeId_CompletedAt",
                table: "BusinessReceivingRequests",
                columns: new[] { "ProjectId", "ScopeId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_ResponsibleActorId",
                table: "BusinessReceivingRequests",
                column: "ResponsibleActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReceivingRequests_SourceKind_SourceId_Kind",
                table: "BusinessReceivingRequests",
                columns: new[] { "SourceKind", "SourceId", "Kind" },
                unique: true);
            // Existing pending requests retain their genuine source time; no ACK or clock is backfilled.
            migrationBuilder.Sql("""
                INSERT INTO BusinessReceivingRequests
                  (Id,ProjectId,Kind,SourceKind,SourceId,SourceVersion,ScopeId,ResponsibleActorId,ResponsibleRole,RequestedAt)
                SELECT NEWID(),r.ProjectId,7,'FieldReview',r.Id,LOWER(REPLACE(CONVERT(nvarchar(36),r.Id),'-','')),
                  r.TaskId,a.AssignedToUserId,4,r.OccurredAt
                FROM FieldInspectionReviews r JOIN FieldInspectionTasks t ON t.Id=r.TaskId AND t.ProjectId=r.ProjectId
                JOIN FieldInspectionSubmissions s ON s.Id=r.SubmissionId AND s.TaskId=t.Id
                JOIN FieldInspectionAssignments a ON a.FieldInspectionTaskId=t.Id AND a.Status=1 AND a.EndedAt IS NULL
                WHERE r.Decision='SUPPLEMENT' AND t.RepairItemId IS NULL AND t.Status NOT IN (6,7,8)
                  AND NOT EXISTS (SELECT 1 FROM FieldInspectionSubmissions newer WHERE newer.TaskId=t.Id AND newer.Revision>s.Revision);
                INSERT INTO BusinessReceivingRequests
                  (Id,ProjectId,Kind,SourceKind,SourceId,SourceVersion,ScopeId,ResponsibleActorId,ResponsibleRole,RequestedAt)
                SELECT NEWID(),r.ProjectId,7,'RepairReview',r.Id,LOWER(REPLACE(CONVERT(nvarchar(36),r.Id),'-','')),
                  b.TaskId,b.CrewId,4,r.At
                FROM RepairAttemptReviews r JOIN RepairItems i ON i.CurrentReviewId=r.Id AND i.SupersededByItemId IS NULL
                JOIN RepairFieldTaskBindings b ON b.Id=i.CurrentBindingId AND b.ProjectId=r.ProjectId
                WHERE r.Decision='SUPPLEMENT';
                INSERT INTO BusinessReceivingRequests
                  (Id,ProjectId,Kind,SourceKind,SourceId,SourceVersion,ScopeId,ResponsibleActorId,ResponsibleRole,RequestedAt)
                SELECT NEWID(),c.ProjectId,8,'ReviewBreach',b.Id,LOWER(REPLACE(CONVERT(nvarchar(36),b.Id),'-','')),
                  c.Id,NULL,1,b.ObservedAt
                FROM DeadlineBreaches b JOIN DeadlineClocks c ON c.Id=b.ClockId
                JOIN FieldInspectionSubmissions s ON s.Id=c.OriginEventId AND s.RootId=s.Id AND s.TaskId=c.TargetId
                  AND s.ProjectId=c.ProjectId AND s.ServerReceivedAt=c.OriginAt
                WHERE c.Kind=4 AND c.CompletedAt IS NULL;
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER [TR_FieldInspectionReviews_Scope] ON [dbo].[FieldInspectionReviews] AFTER INSERT AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN FieldInspectionTasks t ON t.Id=i.TaskId
                    WHERE t.Id IS NULL OR t.ProjectId<>i.ProjectId OR t.LifecycleVersion<>2
                    OR NOT EXISTS (SELECT 1 FROM FieldInspectionSubmissions s WHERE s.Id=i.SubmissionId AND s.TaskId=i.TaskId AND s.ProjectId=i.ProjectId)
                    OR i.Decision NOT IN ('CONFIRM','NO_DEFECT','SUPPLEMENT') OR LEN(LTRIM(RTRIM(i.Reason)))=0
                    OR (i.Decision='SUPPLEMENT' AND i.ReceiptActivation NOT IN ('AWAITING_OWNER_RECEIPT_PROTOCOL','BUSINESS_ACK_REQUIRED')))
                    THROW 51131, 'FIELD history scope, lineage or provenance is invalid.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_BusinessDutyAppointments_Immutable] ON [BusinessDutyAppointments] AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51101, 'Business duty appointment history is immutable.', 1; END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_BusinessReceivingRequests_SourceAck] ON [BusinessReceivingRequests] AFTER UPDATE,DELETE AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS (SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
                    THROW 51102, 'Business receiving source history cannot be deleted.', 1;
                  IF EXISTS (SELECT 1 FROM deleted d JOIN inserted i ON i.Id=d.Id WHERE
                    i.ProjectId<>d.ProjectId OR i.Kind<>d.Kind OR i.SourceKind<>d.SourceKind OR i.SourceId<>d.SourceId OR
                    i.SourceVersion<>d.SourceVersion OR i.ScopeId<>d.ScopeId OR i.ResponsibleRole<>d.ResponsibleRole OR i.RequestedAt<>d.RequestedAt OR
                    (d.AcknowledgedAt IS NOT NULL AND (i.AcknowledgedAt IS NULL OR i.AcknowledgedAt<>d.AcknowledgedAt OR
                     i.AcknowledgmentId IS NULL OR i.AcknowledgmentId<>d.AcknowledgmentId OR i.AcknowledgedBy IS NULL OR i.AcknowledgedBy<>d.AcknowledgedBy OR
                     i.ClockId IS NULL OR i.ClockId<>d.ClockId OR ISNULL(CONVERT(nvarchar(64),i.ClaimedDeviceAt,127),'')<>ISNULL(CONVERT(nvarchar(64),d.ClaimedDeviceAt,127),''))) OR
                    (d.CompletedAt IS NOT NULL AND (i.CompletedAt IS NULL OR i.CompletedAt<>d.CompletedAt)))
                    THROW 51103, 'Business receiving source and first ACK are immutable.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [BusinessReceivingRequests]) THROW 51104, 'Populated receiving request downgrade is forbidden.', 1;");
            migrationBuilder.DropTable(
                name: "BusinessDutyAppointments");

            migrationBuilder.DropTable(
                name: "BusinessReceivingRequests");
        }
    }
}
