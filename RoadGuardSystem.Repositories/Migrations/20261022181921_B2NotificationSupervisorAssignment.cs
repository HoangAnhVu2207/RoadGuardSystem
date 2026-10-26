using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class B2NotificationSupervisorAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NotificationAssignmentId",
                table: "H6NotificationScopes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationAssignmentVersion",
                table: "H6NotificationScopes",
                type: "varchar(12)",
                unicode: false,
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationSupervisorHeads",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupervisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSupervisorHeads", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_NotificationSupervisorHeads_RepairItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "RepairItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSupervisorGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupervisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSupervisorGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSupervisorGenerations_NotificationSupervisorHeads_ItemId",
                        column: x => x.ItemId,
                        principalTable: "NotificationSupervisorHeads",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSupervisorAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSupervisorAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSupervisorAudits_NotificationSupervisorGenerations_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "NotificationSupervisorGenerations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSupervisorAudits_AssignmentId",
                table: "NotificationSupervisorAudits",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSupervisorGenerations_ItemId_Version",
                table: "NotificationSupervisorGenerations",
                columns: new[] { "ItemId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSupervisorHeads_AssignmentId",
                table: "NotificationSupervisorHeads",
                column: "AssignmentId",
                unique: true);

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_NotificationSupervisorGenerations_Immutable] ON [NotificationSupervisorGenerations]
                INSTEAD OF UPDATE, DELETE AS
                BEGIN
                    THROW 51000, 'Assignment generations are immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_NotificationSupervisorAudits_Immutable] ON [NotificationSupervisorAudits]
                INSTEAD OF UPDATE, DELETE AS
                BEGIN
                    THROW 51000, 'Assignment audits are immutable.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_H6NotificationScopes_AssignmentProof] ON [H6NotificationScopes]
                AFTER INSERT, UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i WHERE
                        (i.NotificationAssignmentId IS NULL AND i.NotificationAssignmentVersion IS NOT NULL)
                        OR (i.NotificationAssignmentId IS NOT NULL AND i.NotificationAssignmentVersion IS NULL))
                        THROW 51000, 'Incomplete assignment scope proof.', 1;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.NotificationId=i.NotificationId
                        WHERE ISNULL(CONVERT(varchar(36),i.NotificationAssignmentId),'') <> ISNULL(CONVERT(varchar(36),d.NotificationAssignmentId),'')
                        OR ISNULL(i.NotificationAssignmentVersion,'') <> ISNULL(d.NotificationAssignmentVersion,''))
                        THROW 51000, 'Assignment scope proof is immutable.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER [TR_H6NotificationScopes_AssignmentProof]");
            migrationBuilder.DropTable(name: "NotificationSupervisorAudits");
            migrationBuilder.DropTable(name: "NotificationSupervisorGenerations");
            migrationBuilder.DropTable(name: "NotificationSupervisorHeads");
            migrationBuilder.DropColumn(name: "NotificationAssignmentId", table: "H6NotificationScopes");
            migrationBuilder.DropColumn(name: "NotificationAssignmentVersion", table: "H6NotificationScopes");
        }
    }
}
