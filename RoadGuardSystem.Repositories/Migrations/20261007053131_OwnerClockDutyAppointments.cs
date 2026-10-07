using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadGuardSystem.cRepositories.Migrations
{
    /// <inheritdoc />
    public partial class OwnerClockDutyAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<Guid>(
                name: "AppointedActorId",
                table: "DeadlineClocks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "AppointedRole",
                table: "DeadlineClocks",
                type: "tinyint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeadlineDutyAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    DecisionActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineDutyAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadlineDutyAppointments_DeadlineClocks_ClockId",
                        column: x => x.ClockId,
                        principalTable: "DeadlineClocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineDutyAppointments_Users_CurrentActorId",
                        column: x => x.CurrentActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeadlineDutyAppointments_Users_DecisionActorId",
                        column: x => x.DecisionActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineClocks_AppointedActorId",
                table: "DeadlineClocks",
                column: "AppointedActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineDutyAppointments_ClockId",
                table: "DeadlineDutyAppointments",
                column: "ClockId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineDutyAppointments_CurrentActorId",
                table: "DeadlineDutyAppointments",
                column: "CurrentActorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineDutyAppointments_DecisionActorId",
                table: "DeadlineDutyAppointments",
                column: "DecisionActorId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeadlineClocks_Users_AppointedActorId",
                table: "DeadlineClocks",
                column: "AppointedActorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_DeadlineDutyAppointments_Immutable] ON [DeadlineDutyAppointments] AFTER UPDATE,DELETE AS
                BEGIN SET NOCOUNT ON; THROW 51105, 'Deadline duty appointment history is immutable.', 1; END
                """, suppressTransaction: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [DeadlineDutyAppointments]) THROW 51106, 'Populated duty appointment downgrade is forbidden.', 1;");
            migrationBuilder.DropForeignKey(
                name: "FK_DeadlineClocks_Users_AppointedActorId",
                table: "DeadlineClocks");

            migrationBuilder.DropTable(
                name: "DeadlineDutyAppointments");

            migrationBuilder.DropIndex(
                name: "IX_DeadlineClocks_AppointedActorId",
                table: "DeadlineClocks");

            migrationBuilder.DropColumn(
                name: "AppointedActorId",
                table: "DeadlineClocks");

            migrationBuilder.DropColumn(
                name: "AppointedRole",
                table: "DeadlineClocks");
        }
    }
}
