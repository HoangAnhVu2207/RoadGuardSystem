using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class H4SafetySourceAdmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Never manufacture a uploader or collapse duplicate historical safety obligations.
            migrationBuilder.Sql("IF EXISTS(SELECT SafetyObligationId FROM RepairSafetyMonitoring GROUP BY SafetyObligationId HAVING COUNT(*)>1) THROW 51270, 'Duplicate safety monitoring requires an explicit preservation decision.', 1;");

            migrationBuilder.DropIndex(
                name: "IX_RepairSafetyMonitoring_SafetyObligationId",
                table: "RepairSafetyMonitoring");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeadlineClocks_Times",
                table: "DeadlineClocks");

            migrationBuilder.AddColumn<Guid>(
                name: "ActualUploaderId",
                table: "RepairSafetyCheckEvidence",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepairSafetyActionSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeasureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OriginId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairSafetyActionSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_RepairFieldTaskBindings_BindingId",
                        column: x => x.BindingId,
                        principalTable: "RepairFieldTaskBindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_RepairTemporarySafetyMeasures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "RepairTemporarySafetyMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairSafetyActionSources_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyMonitoring_SafetyObligationId",
                table: "RepairSafetyMonitoring",
                column: "SafetyObligationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyCheckEvidence_ActualUploaderId",
                table: "RepairSafetyCheckEvidence",
                column: "ActualUploaderId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DeadlineClocks_Times",
                table: "DeadlineClocks",
                sql: "([OriginalDueAt]>[OriginAt] OR ([Kind]=10 AND [OriginalDueAt]=[OriginAt])) AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_ActorId",
                table: "RepairSafetyActionSources",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_BindingId",
                table: "RepairSafetyActionSources",
                column: "BindingId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_ItemId",
                table: "RepairSafetyActionSources",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_MeasureId_Kind_OriginId",
                table: "RepairSafetyActionSources",
                columns: new[] { "MeasureId", "Kind", "OriginId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyActionSources_ProjectId",
                table: "RepairSafetyActionSources",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairSafetyCheckEvidence_Users_ActualUploaderId",
                table: "RepairSafetyCheckEvidence",
                column: "ActualUploaderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_RepairSafetyActionSources_Immutable] ON [RepairSafetyActionSources]
                AFTER INSERT, UPDATE, DELETE AS BEGIN
                 SET NOCOUNT ON;
                 IF EXISTS(SELECT 1 FROM deleted) THROW 51271, 'Safety action provenance is append only.', 1;
                 IF EXISTS(SELECT 1 FROM inserted s WHERE NOT EXISTS(
                   SELECT 1 FROM RepairTemporarySafetyMeasures m
                   JOIN RepairItems i ON i.Id=s.ItemId AND i.ProjectId=s.ProjectId
                   JOIN RepairFieldTaskBindings b ON b.Id=s.BindingId AND b.ItemId=i.Id AND b.ProjectId=s.ProjectId
                   JOIN RepairSafetyMonitoring r ON r.MeasureId=m.Id
                   WHERE m.Id=s.MeasureId AND m.ProjectId=s.ProjectId AND r.FormalObligationId=i.ObligationId
                   AND ((s.Kind='ASSIGNED' AND s.OriginId=m.Id AND EXISTS(SELECT 1 FROM Users u WHERE u.Id=s.ActorId AND u.RoleCode='PM'))
                    OR (s.Kind='INSTALLED' AND s.OriginId=m.InstallationEventId AND s.ActorId=m.InstalledBy AND s.At=m.InstalledAt)
                    OR (s.Kind='WARNING' AND EXISTS(SELECT 1 FROM RepairDangerWarnings w
                        WHERE w.Id=s.OriginId AND w.MonitoringId=r.Id AND w.ServerReceivedAt=s.At)))))
                   THROW 51272, 'Safety action must retain its actual scoped source.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM RepairSafetyActionSources) OR EXISTS(SELECT 1 FROM RepairSafetyCheckEvidence WHERE ActualUploaderId IS NOT NULL) OR EXISTS(SELECT 1 FROM DeadlineClocks WHERE OriginalDueAt=OriginAt) THROW 51273, 'Populated safety admission requires preservation-aware downgrade.', 1;");

            migrationBuilder.DropForeignKey(
                name: "FK_RepairSafetyCheckEvidence_Users_ActualUploaderId",
                table: "RepairSafetyCheckEvidence");

            migrationBuilder.DropTable(
                name: "RepairSafetyActionSources");

            migrationBuilder.DropIndex(
                name: "IX_RepairSafetyMonitoring_SafetyObligationId",
                table: "RepairSafetyMonitoring");

            migrationBuilder.DropIndex(
                name: "IX_RepairSafetyCheckEvidence_ActualUploaderId",
                table: "RepairSafetyCheckEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeadlineClocks_Times",
                table: "DeadlineClocks");

            migrationBuilder.DropColumn(
                name: "ActualUploaderId",
                table: "RepairSafetyCheckEvidence");

            migrationBuilder.CreateIndex(
                name: "IX_RepairSafetyMonitoring_SafetyObligationId",
                table: "RepairSafetyMonitoring",
                column: "SafetyObligationId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DeadlineClocks_Times",
                table: "DeadlineClocks",
                sql: "[OriginalDueAt]>[OriginAt] AND [CurrentDueAt]>=[OriginalDueAt] AND ([CompletedAt] IS NULL OR [CompletedAt]>=[OriginAt])");
        }
    }
}
